<template>

  <GraphNavbar :signalrStatus="signalrStatus"
               :graphTitle="graphTitle"
               :isAuthenticated="isAuthenticated"
               @reset-graph="resetGraph"
               @open-crawl="openCrawlGraph"
               @open-connect="openConnect"
               @open-create="openCreateGraph"
               @open-activity="openActivitySidebar"
               @open-update="openUpdateGraph" />


  <main class="graph-layout">
    <!-- Container for Sigma Graph -->
    <div id="graph-container"
         ref="container"
         class="graph-container"></div>

    <!-- Activity Sidebar -->
    <ActivitySidebar v-model="activitySidebarOpen"
                     :activityLogs="activityLogs"
                     @clear-activity="clearActivity" />

    <!-- Node Sidebar -->
    <NodeSidebar v-model="nodeSidebarOpen"
                 :node="nodeSidebarData"
                 @crawl-node="handleCrawlNode"
                 @focus-node="handleFocusNode"
                 @display-node="handleDisplayNode"/>

    <!-- Modal windows -->
    <b-modal v-if="modalView !== null"
             :model-value="true"
             @update:model-value="modalView = null"
             custom-class="responsive-modal"
             trap-focus
             :has-modal-card="false">

      <GraphConnect v-if="modalView === 'connect'"
                    :graphs="availableGraphs"
                    :connectedGraphId="graphId"
                    :page="page"
                    :pageSize="pageSize"
                    :totalCount="totalCount"
                    @connectGraph="connectGraph"
                    @disconnectGraph="disconnectGraph"
                    @createGraph="openCreateGraph"
                    @changePage="loadGraphs"
                    @deleteGraph="deleteGraph" />

      <GraphForm v-if="['create', 'update', 'crawl', 'preview'].includes(modalView)"
                 :graphId="graphId"
                 :correlationId="correlationId"
                 :mode="modalView"
                 :crawlUrl="crawlUrl"
                 :activityLogs="activityLogs"
                 @confirmAction="onConfirmAction"
                 @previewBack="onPreviewBack" />
    </b-modal>
  </main>
</template>

<style scoped>
  .graph-layout {
    height: calc(100vh - 60px); /* full viewport minus navbar */
    overflow: hidden;
  }

  .modal.is-active {
    z-index: 3000 !important; /* overrides default */
  }

  /* override the inline max-width */
  .responsive-modal .modal-content {
    max-width: 40% !important;
  }

  @media (max-width: 768px) {
    .responsive-modal .modal-content {
      max-width: 60% !important;
    }
  }

  @media (max-width: 480px) {
    .responsive-modal .modal-content {
      max-width: 90% !important;
    }
  }
</style>

<script setup>
  import { ref, onMounted, onUnmounted, watch, computed } from "vue"
  import { useRoute, useRouter } from "vue-router"
  import axios from "axios"
  import apiClient from './apiClient.js'
  import Graph from "graphology"
  import * as signalR from "@microsoft/signalr"
  import apiConfig from "./config/api-config.js"
  import appConfig from "./config/app-config.js"
  import {
    initSignalRController,
    disposeSignalR
  } from "./signalr.js"
  import {
    setupSigma,
    setupFA2,
    setupHighlighting,
    setupReducer,
    setupNodeSizing,
    addOrUpdateNode,
    addEdge,
    highlightNode,
    nodeClickHandler,
    panTo
  } from "./sigma.js"
  import GraphNavbar from './components/graph-navbar.vue'
  import GraphConnect from './components/graph-connect.vue'
  import GraphForm from './components/graph-form.vue'
  import NodeSidebar from './components/node-sidebar.vue'
  import ActivitySidebar from './components/activity-sidebar.vue'


  // --- Refs / State ---
  // Authentication Token
  const isAuthenticated = computed(() => !!localStorage.getItem('jwt'))

  // Sigma / Graph
  const container = ref(null)
  const sigmaGraph = new Graph()
  let sigmaInstance = null
  let fa2 = null
  let fa2Timer = null
  let highlightedNode = ref(null)

  // SignalR
  let signalrController = null
  let signalrStatus = ref("disconnected")

  // Modal / Page
  const modalView = ref(null)
  const graphId = ref(null)
  const correlationId = ref(null)
  const graphTitle = ref(null)
  const connectingGraphId = ref(null)
  const crawlUrl = ref(null)

  // Pagination / Graph list
  const availableGraphs = ref([])
  const page = ref(1)
  const pageSize = ref(appConfig.defaultPageSize)
  const totalCount = ref(0)

  // Routing
  const route = useRoute()
  const router = useRouter()

  // Activity Sidebar
  const activitySidebarOpen = ref(false)
  const activityLogs = ref([])

  // Node Sidebar
  const nodeSidebarOpen = ref(false)
  const nodeSidebarData = ref(null)

  // Subgraph Requests Throttle
  const subgraphRequestCache = new Map()
  let subgraphRequest = { nodeId: null, controller: null }


  // --- Lifecycle ---
  onMounted(async () => {
    sigmaInstance = setupSigma(sigmaGraph, container.value)
    fa2 = setupFA2(sigmaGraph)

    setupReducer(sigmaGraph, sigmaInstance, highlightedNode)
    setupHighlighting(sigmaGraph, sigmaInstance, runFA2, highlightedNode, openNodeSidebar, nodeSubGraph, graphId)
    setupNodeSizing(sigmaGraph, sigmaInstance)

    loadGraphs()
  })

  onUnmounted(() => {
    disposeSignalR(signalrController)
  })

  // --- Watchers ---
  // Watch route for graphId changes
  watch(() => route.params.id, async (id) => {
    if (!id) {
      graphId.value = null
      modalView.value = "connect"
      return
    }

    if (signalrController?.graphId === id) return
    await connectGraphById(id)
  }, { immediate: true })


  // --- Centralized FA2 ---
  function runFA2(duration = appConfig.fa2DurationSlow_MS) {
    // If already running, just extend the timer
    if (fa2.isRunning()) {
      clearTimeout(fa2Timer)
    } else {
      fa2.start()
    }

    // Always extend/renew the stop timer
    fa2Timer = setTimeout(() => {
      fa2.stop()
    }, duration)

    runFA2.stop = () => {
      if (fa2.isRunning()) fa2.stop()
      clearTimeout(fa2Timer)
    }
  }


  // --- UI Event Handlers ---
  function openConnect() {
    modalView.value = "connect"
    loadGraphs()
  }

  function openCreateGraph() {
    modalView.value = "create"
  }

  function openUpdateGraph() {
    modalView.value = "update"
  }

  function openCrawlGraph() {
    modalView.value = "crawl"
  }

  function resetGraph() {
    panTo(sigmaGraph, sigmaInstance, null, {
      ratio: 1,
      duration: appConfig.panDuration_MS
    })
  }

  function clearActivity() {
    activityLogs.value = []
  }

  function openActivitySidebar() {
    activitySidebarOpen.value = true
  }

  function openNodeSidebar(nodeData) {
    if (!nodeData) {
      nodeSidebarData.value = null
      nodeSidebarOpen.value = false
      return
    }
    nodeSidebarData.value = nodeData
    nodeSidebarOpen.value = true
  }

  function handleCrawlNode(nodeId) {
    if (!sigmaGraph.hasNode(nodeId) || !sigmaInstance) return

    crawlUrl.value = nodeId;
    modalView.value = "crawl";
  }

  async function handleFocusNode(nodeId) {
    highlightNode(
      sigmaGraph,
      sigmaInstance,
      runFA2,
      highlightedNode,
      nodeId,
      panTo,
      appConfig.fa2DurationSlow_MS
    )
  }

  async function handleDisplayNode(nodeId) {
    await nodeClickHandler({
        nodeId,
        sigmaGraph,
        sigmaInstance,
        runFA2,
        highlightedNode,
        openNodeSidebar,
        nodeSubGraph,
        graphId,
        panTo,
        fa2Duration: appConfig.fa2DurationFast_MS
      });
  }

  function onConfirmAction(response) {
    switch (response.type) {
      case "create":
        modalView.value = "crawl";
        break;
      case "update":
        modalView.value = null
        break;
      case "crawl":
        if (response.data.preview === true) {
          correlationId.value = response.data.correlationId
          modalView.value = "preview"

        } else {
          //crawl - hide modal
          modalView.value = null
          activitySidebarOpen.value = true;
        }
        break;
    }
  }

  function onPreviewBack() {
    modalView.value = "crawl";
  }


  // --- Graph / Data Functions ---
  async function loadGraphs(newPage = page.value, newPageSize = pageSize.value) {
    page.value = newPage
    pageSize.value = newPageSize

    try {
      const response = await apiClient.get(apiConfig.GRAPH_LIST, {
        params: { page: newPage, pageSize: newPageSize }
      })

      availableGraphs.value = response.data.items
      totalCount.value = response.data.totalCount
      page.value = response.data.page
      pageSize.value = response.data.pageSize

    } catch (err) {
      availableGraphs.value = []
      totalCount.value = 0
    }
  }

  async function populateGraph(graphId, sigmaGraph, runFA2, { maxDepth, maxNodes }) {
    try {
      const response = await apiClient.get(apiConfig.GRAPH_POPULATE(graphId), {
        params: { maxDepth, maxNodes }
      })

      const payload = response.data
      if (!payload || !payload.nodes) {
        console.warn("No graph data returned")
        return
      }

      payload.nodes.forEach(n => addOrUpdateNode(sigmaGraph, n))
      payload.edges.forEach(e => addEdge(sigmaGraph, e))

      runFA2(appConfig.fa2DurationSlow_MS)

    } catch (err) {
      console.error("Failed to populate graph:", err)
    }
  }


  async function nodeSubGraph(graphId, nodeId, sigmaGraph, runFA2) {
    const now = Date.now()

    // Skip if recently requested
    const last = subgraphRequestCache.get(nodeId) || 0
    if (now - last < appConfig.subGraphThrottle_MS) return

    // Abort previous request
    subgraphRequest.controller?.abort()

    // Create new controller for this request
    const controller = new AbortController()
    subgraphRequest = { nodeId, controller }
    subgraphRequestCache.set(nodeId, now)

    try {
      const { data: payload } = await apiClient.post(
        apiConfig.GRAPH_NODESUBGRAPH(graphId.value),
        {
          nodeUrl: nodeId,
          maxDepth: appConfig.subGraphMaxDepth
        },
        { signal: controller.signal }
      )

      if (!payload?.nodes) return

      // Merge into graph
      payload.nodes.forEach(n => {
        const exists = sigmaGraph.hasNode(n.id)

        addOrUpdateNode(sigmaGraph, n)

        if (!exists) {
          // seed position near selected node
          const { x: cx, y: cy } = sigmaGraph.getNodeAttributes(nodeId)
          const angle = Math.random() * 2 * Math.PI
          const radius = 20
          sigmaGraph.setNodeAttribute(n.id, "x", cx + Math.cos(angle) * radius)
          sigmaGraph.setNodeAttribute(n.id, "y", cy + Math.sin(angle) * radius)
        }
      })

      payload.edges.forEach(e => addEdge(sigmaGraph, e))

      runFA2(appConfig.fa2DurationSlow_MS)

    } catch (err) {
      if (axios.isCancel(err)) console.debug(`Request for node ${nodeId} cancelled`)
      else console.error(`Failed to load subgraph for node ${nodeId}:`, err)
    } finally {
      //clear request once complete
      if (subgraphRequest.nodeId === nodeId) subgraphRequest = { nodeId: null, controller: null }
      clearSubgraphRequests(subgraphRequestCache, appConfig.subGraphThrottle_MS)
    }
  }

  // Clears expired subgraph requests
  function clearSubgraphRequests(requestMap, thresholdMs) {
    const now = Date.now()
    for (const [nodeId, timestamp] of requestMap.entries()) {
      if (now - timestamp > thresholdMs) {
        requestMap.delete(nodeId)
      }
    }
  }

  function resetGraphState() {
    if (fa2Timer) {
      clearTimeout(fa2Timer)
      fa2Timer = null
    }

    if (fa2) fa2.stop()

    sigmaGraph.clear()
    fa2 = setupFA2(sigmaGraph)
    highlightedNode.value = null
    clearActivity()
  }


  // --- Graph Connection / SignalR Functions ---
  async function connectGraph(graph) {
    if (connectingGraphId.value === graph.id) return

    //update valid graphId
    connectingGraphId.value = graph.id
    modalView.value = null
    graphId.value = graph.id
    graphTitle.value = graph.name
    await router.push({
      name: "Graph",
      params: { id: graph.id }
    })

    resetGraphState()

    // --- populate with initial data ---
    await populateGraph(graph.id, sigmaGraph, runFA2, {
      maxDepth: appConfig.populateGraphMaxDepth,
      maxNodes: appConfig.populateGraphMaxNodes
    })

    // --- Connect SignalR ---
    disposeSignalR(signalrController)
    signalrController = await initSignalRController(graph.id, {
      sigmaGraph, runFA2, hubUrl: apiConfig.SIGNALR_HUB, activityLogs, router
    })

    // Watch signalR status changes
    signalrStatus.value = signalrController.status.value
    watch(signalrController.status, val => {
      signalrStatus.value = val
    })
  }

  async function disconnectGraph(graph) {
    connectingGraphId.value = null
    graphId.value = null
    graphTitle.value = null
    disposeSignalR(signalrController)
    signalrController = null

    await router.push({ path: "/" })
  }

  //Connect to Graph by Id (from route)
  async function connectGraphById(id) {
    if (!id) {
      graphId.value = null
      modalView.value = "connect"
      return
    }

    if (signalrController?.graphId === id) return;

    // Close Node Sidebar
    nodeSidebarOpen.value = false
    nodeSidebarData.value = null

    try {
      // Get graph metadata from API
      const response = await apiClient.get(apiConfig.GRAPH_GET(id))
      const graph = response.data
      if (!graph) throw new Error("Graph not found")

      await connectGraph(graph);

    } catch (err) {
      console.error("Failed to connect graph:", err)

      disposeSignalR(signalrController)
      resetGraphState()

      //reset invalid graphId
      graphId.value = null
      modalView.value = "connect"
      await router.replace({ path: "/" })
    }
  }

  async function deleteGraph(graph) {
    if (!graph?.id) return

    try {
      // Call DELETE API
      await apiClient.delete(apiConfig.GRAPH_DELETE(graph.id))

      // If the deleted graph is the one we're connected to, disconnect
      if (graphId.value === graph.id) {
        await disconnectGraph(graph)
        graphId.value = null
        modalView.value = "connect"
      }

      // Refresh list
      await loadGraphs(page.value, pageSize.value)

    } catch (err) {
      console.error(`Failed to delete graph ${graph.id}:`, err)
    }
  }

</script>
