<script setup>
  import { ref, onMounted, onUnmounted, watch } from "vue"
  import { useRoute, useRouter } from "vue-router"
  import axios from "axios"
  import Graph from "graphology"
  import * as signalR from "@microsoft/signalr"
  import apiConfig from "./config/api-config.js"
  import {
    initSignalRController,
    disposeSignalR } from "./signalr.js"
  import {
    setupSigma,
    setupFA2,
    setupHighlighting,
    setupReducer,
    setupNodeSizing,
    addOrUpdateNode,
    addEdge,
    resetHighlight,
    highlightNeighbors,
    focusNode } from "./sigma.js"
  import GraphConnect from './components/graph-connect.vue'
  import GraphForm from './components/graph-form.vue'
  import NodeSidebar from './components/node-sidebar.vue'

  // --- Refs / State ---
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
  const connectingGraphId = ref(null)
  const crawlUrl = ref(null)

  // Pagination / Graph list
  const availableGraphs = ref([])
  const page = ref(1)
  const pageSize = ref(10)
  const totalCount = ref(0)

  // Routing
  const route = useRoute()
  const router = useRouter()

  // Node Sidebar
  const nodeSidebarOpen = ref(false)
  const nodeSidebarData = ref(null)

  // --- Lifecycle ---
  onMounted(async () => {
    sigmaInstance = setupSigma(sigmaGraph, container.value)
    fa2 = setupFA2(sigmaGraph)

    setupReducer(sigmaGraph, sigmaInstance, highlightedNode)
    setupHighlighting(sigmaGraph, sigmaInstance, runFA2, highlightedNode, handleNodeSidebar, nodeSubGraph, graphId)
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
  function runFA2(duration = 1000) {
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

  function handleNodeSidebar(nodeData) {
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
    if (!sigmaGraph.hasNode(nodeId) || !sigmaInstance) return

    const nodeAttr = sigmaGraph.getNodeAttributes(nodeId);

    if (highlightedNode.value) resetHighlight(sigmaGraph, sigmaInstance)

    // Highlight node
    highlightedNode.value = nodeId;
    highlightNeighbors(sigmaGraph, sigmaInstance, nodeId);

    runFA2(500)

    //focus on selected node
    focusNode(sigmaGraph, sigmaInstance, nodeAttr)
  }

  function onConfirmAction(response) {
    modalView.value = null;
  }


  // --- Graph / Data Functions ---
  async function loadGraphs(newPage = page.value, newPageSize = pageSize.value) {
    page.value = newPage
    pageSize.value = newPageSize

    try {
      const response = await axios.get(apiConfig.GRAPH_LIST, {
        params: { page: newPage, pageSize: newPageSize  }
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
      const response = await axios.get(apiConfig.GRAPH_POPULATE(graphId), {
        params: { maxDepth, maxNodes }
      })

      const payload = response.data
      if (!payload || !payload.nodes) {
        console.warn("No graph data returned")
        return
      }

      payload.nodes.forEach(n => addOrUpdateNode(sigmaGraph, n))
      payload.edges.forEach(e => addEdge(sigmaGraph, e))

      runFA2(1500)

    } catch (err) {
      console.error("Failed to populate graph:", err)
    }
  }




  const requestCache = new Map() // nodeId => last request timestamp
  const THROTTLE_MS = 30_000 //30 seconds
  let currentRequest = { nodeId: null, controller: null }

  async function nodeSubGraph(graphId, nodeId, sigmaGraph, runFA2) {
    const now = Date.now()

    // Skip if recently requested
    const last = requestCache.get(nodeId) || 0
    if (now - last < THROTTLE_MS) return

    // Abort previous request
    currentRequest.controller?.abort()

    // Create new controller for this request
    const controller = new AbortController()
    currentRequest = { nodeId, controller }
    requestCache.set(nodeId, now)

    try {
      const { data: payload } = await axios.post(
        apiConfig.GRAPH_NODESUBGRAPH(graphId.value),
        { nodeUrl: nodeId },
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

      runFA2(1000)

    } catch (err) {
      if (axios.isCancel(err)) console.debug(`Request for node ${nodeId} cancelled`)
      else console.error(`Failed to load subgraph for node ${nodeId}:`, err)
    } finally {
      //clear request once complete
      if (currentRequest.nodeId === nodeId) currentRequest = { nodeId: null, controller: null }
      pruneOldRequests(requestCache, THROTTLE_MS)
    }
  }

  function pruneOldRequests(requestMap, thresholdMs) {
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
  }


  // --- Graph Connection / SignalR Functions ---
  async function connectToGraph(graph) {
    if (connectingGraphId.value === graph.id) return;

    //update valid graphId
    connectingGraphId.value = graph.id;
    modalView.value = null;
    graphId.value = graph.id;
    await router.push({
      name: "Graph",
      params: { id: graph.id }
    });

    resetGraphState()

    // --- populate with initial data ---
    await populateGraph(graph.id, sigmaGraph, runFA2, { maxDepth: 6, maxNodes: 5000 })

    // --- Connect SignalR ---
    disposeSignalR(signalrController)
    signalrController = await initSignalRController(graph.id, {
      sigmaGraph, runFA2, hubUrl: apiConfig.SIGNALR_HUB
    })

    // Watch signalR status changes
    signalrStatus.value = signalrController.status.value
    watch(signalrController.status, val => {
      signalrStatus.value = val
    })
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
      const response = await axios.get(apiConfig.GRAPH_GET(id))
      const graph = response.data
      if (!graph) throw new Error("Graph not found")

      await connectToGraph(graph);

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

</script>

<template>
  <header>
    <b-navbar type="is-primary">
      <template #brand>
        <b-navbar-item>
          <span class="has-text-weight-bold is-size-4">WebGrapher</span>
        </b-navbar-item>
      </template>

      <template #end>
        <!-- New Graph -->
        <b-navbar-item v-if="signalrStatus !== 'connected'">
          <b-button type="is-light" outlined @click="openCreateGraph">
            <span class="icon"><i class="mdi mdi-plus-thick"></i></span>
            <span>New</span>
          </b-button>
        </b-navbar-item>

        <!-- Crawl Page (only show if connected) -->
        <b-navbar-item v-if="signalrStatus === 'connected'">
          <b-button type="is-light" outlined @click="openCrawlGraph">
            <span class="icon">
              <i class="mdi mdi-spider"></i>
            </span>
            <span>Crawl</span>
          </b-button>
        </b-navbar-item>

        <!-- Connect -->
        <b-navbar-item>
          <b-button type="is-light" outlined @click="openConnect">
            <span class="icon">
              <i class="mdi mdi-circle"
                 :class="{
                  'has-text-success': signalrStatus === 'connected',
                  'has-text-danger': signalrStatus !== 'connected',
                }"></i>
            </span>
            <span>{{ signalrStatus === 'connected' ? "Connected" : "Connect" }}</span>
          </b-button>
        </b-navbar-item>

        <!-- Graph Activity (only show if connected) -->
        <b-navbar-item v-if="signalrStatus === 'connected'">
          <b-button type="is-light" outlined>
            <span class="icon">
              <i class="mdi mdi-list-box"></i>
            </span>
            <span>Activity</span>
          </b-button>
        </b-navbar-item>

        <!-- Settings (only show if connected) -->
        <b-navbar-item v-if="signalrStatus === 'connected'">
          <b-button type="is-light" outlined @click="openUpdateGraph">
            <span class="icon"><i class="mdi mdi-cog"></i></span>
          </b-button>
        </b-navbar-item>

      </template>
    </b-navbar>
  </header>

  <main class="graph-layout">
    <!-- Container for Sigma Graph -->
    <div id="graph-container"
         ref="container"
         class="graph-container"></div>

    <!-- Node Sidebar -->
    <NodeSidebar v-model="nodeSidebarOpen"
                  :node="nodeSidebarData"
                  @crawl-node="handleCrawlNode"
                  @focus-node="handleFocusNode"/>

    <!-- Modal windows -->
    <b-modal v-if="modalView !== null"
             :model-value="true"
             @update:model-value="modalView = null"
             custom-class="responsive-modal"
             trap-focus
             :has-modal-card="false">

      <GraphConnect v-if="modalView === 'connect'"
                    :graphs="availableGraphs"
                    :page="page"
                    :pageSize="pageSize"
                    :totalCount="totalCount"
                    @selectGraph="connectToGraph"
                    @createGraph="openCreateGraph"
                    @changePage="loadGraphs" />

      <GraphForm v-if="['create', 'update', 'crawl'].includes(modalView)"
                 :graphId="graphId"
                 :mode="modalView"
                 :crawlUrl="crawlUrl"
                 @confirmAction="onConfirmAction" />

      <!-- <GraphPreview v-if="modalView === 'preview'" /> -->
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
