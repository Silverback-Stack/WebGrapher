<script setup>
  import { ref, onMounted, onUnmounted, watch } from "vue"
  import { useRoute, useRouter } from "vue-router"
  import axios from "axios"
  import * as signalR from "@microsoft/signalr"
  import { setupSignalR } from "./signalr-setup.js"
  import Graph from "graphology"

  import apiConfig from "./config/api-config.js"
  import {
    addOrUpdateNode,
    addEdge,
    setupReducer,
    resetHighlight,
    highlightNeighbors
  } from "./sigma-graph-utils.js"
  import {
    setupSigma,
    setupFA2,
    setupHighlighting,
    setupNodeSizing
  } from "./sigma-graph-setup.js"
  import GraphConnect from './components/graph-connect.vue'
  import GraphForm from './components/graph-form.vue'
  import GraphSidebar from './components/graph-sidebar.vue'

  // --- Refs / State ---
  const container = ref(null)
  const sigmaGraph = new Graph()
  let sigmaInstance = null
  let fa2 = null
  let highlightedNode = ref(null)

  let signalrController = null
  const signalrStatus = ref("disconnected")

  const modalView = ref(null)
  const graphId = ref(null)
  const connectingGraphId = ref(null)

  const availableGraphs = ref([])
  const page = ref(1)
  const pageSize = ref(10)
  const totalCount = ref(0)

  const route = useRoute()
  const router = useRouter()

  const sidebarOpen = ref(false)
  const sidebarData = ref(null)

  // --- Lifecycle ---
  onMounted(async () => {
    sigmaInstance = setupSigma(sigmaGraph, container.value)
    fa2 = setupFA2(sigmaGraph)

    setupReducer(sigmaGraph, sigmaInstance, highlightedNode)
    setupHighlighting(sigmaGraph, sigmaInstance, fa2, highlightedNode, handleSidebar)
    setupNodeSizing(sigmaGraph, sigmaInstance)

    loadGraphs()
  })

  onUnmounted(() => {
    signalrController?.dispose()
  })

  // --- Watch route for graphId changes ---
  watch(() => route.params.id, async (id) => {
    if (!id) {
      graphId.value = null
      modalView.value = "connect"
      return
    }

    if (signalrController?.graphId === id) return
    await connectGraphById(id)
  }, { immediate: true })


  // --- Functions ---
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





  function handleSidebar(nodeData) {
    if (!nodeData) {
      sidebarData.value = null
      sidebarOpen.value = false
      return
    }
    sidebarData.value = nodeData
    sidebarOpen.value = true
  }

  function handleCrawlNode(nodeId) {
    if (!sigmaGraph.hasNode(nodeId) || !sigmaInstance) return

    console.log("Crawl triggered for:", nodeId)
  }

  function handleFocusNode(nodeId) {
    console.log("focus triggered for:", nodeId)
    if (!sigmaGraph.hasNode(nodeId) || !sigmaInstance) return

    const nodeAttr = sigmaGraph.getNodeAttributes(nodeId);

    if (highlightedNode.value) resetHighlight(sigmaGraph, sigmaInstance)

    // Highlight this node
    highlightedNode.value = nodeId;
    highlightNeighbors(sigmaGraph, sigmaInstance, nodeId);

    if (!fa2.isRunning()) {
      fa2.start()
      setTimeout(() => fa2.stop(), 250)
    }

    //pan camera to node
    const camera = sigmaInstance.getCamera();
    // Get graph bounding box
    const bbox = sigmaGraph.nodes().reduce(
      (acc, n) => {
        const a = sigmaGraph.getNodeAttributes(n);
        return {
          minX: Math.min(acc.minX, a.x),
          maxX: Math.max(acc.maxX, a.x),
          minY: Math.min(acc.minY, a.y),
          maxY: Math.max(acc.maxY, a.y)
        };
      },
      { minX: Infinity, maxX: -Infinity, minY: Infinity, maxY: -Infinity }
    );

    // Convert node coordinates to normalized camera coordinates
    const normX = (nodeAttr.x - bbox.minX) / (bbox.maxX - bbox.minX);
    const normY = (nodeAttr.y - bbox.minY) / (bbox.maxY - bbox.minY);

    console.log("Node coords:", nodeAttr.x, nodeAttr.y);
    console.log("Camera BEFORE:", camera.x, camera.y, camera.ratio);
    console.log("Normalized target:", normX, normY);

    camera.animate(
      { x: normX, y: normY, ratio: camera.ratio },
      { duration: 250 }
    );
  }

  function onConfirmAction(response) {
    modalView.value = null;
  }

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

  async function populateGraph(graphId, sigmaGraph, fa2, { maxDepth, maxNodes }) {
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

      if (fa2 && !fa2.isRunning()) {
        fa2.start()
        setTimeout(() => fa2.stop(), 2000)
      }
    } catch (err) {
      console.error("Failed to populate graph:", err)
    }
  }

  async function connectToGraph(graph) {
    if (connectingGraphId.value === graph.id) return;
    connectingGraphId.value = graph.id;

    modalView.value = null;
    graphId.value = graph.id;

    await router.push({
      name: "Graph",
      params: { id: graph.id }
    });

    // --- reset state ---
    sigmaGraph.clear()
    fa2.stop()
    fa2 = setupFA2(sigmaGraph)
    highlightedNode.value = null

    // --- populate with initial data ---
    await populateGraph(graph.id, sigmaGraph, fa2, { maxDepth: 1, maxNodes: 250 })

    // --- signalR ---
    signalrController?.dispose();
    signalrController = await setupSignalR(graph.id, {
      sigmaGraph,
      fa2,
      onStatus: (status) => (signalrStatus.value = status),
      hubUrl: apiConfig.SIGNALR_HUB
    })
    signalrController.graphId = graph.id
    connectingGraphId.value = null
  }

  async function connectGraphById(id) {
    if (!id) {
      graphId.value = null
      modalView.value = "connect"
      return
    }

    if (signalrController?.graphId === id) return;

    try {
      // Get graph metadata from API
      const response = await axios.get(apiConfig.GRAPH_GET(id))
      const graph = response.data
      if (!graph) throw new Error("Graph not found")

      await connectToGraph(graph);

    } catch (err) {
      console.error("Failed to connect graph:", err)
      signalrController?.dispose()
      sigmaGraph.clear()
      fa2.stop()
      fa2 = setupFA2(sigmaGraph)

      highlightedNode.value = null
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

        <!-- Graph Activity (only show if connected) -->
        <b-navbar-item v-if="signalrStatus === 'connected'">
          <b-button type="is-light" outlined>
            <span class="icon">
              <i class="mdi mdi-list-box"></i>
            </span>
            <span>Activity</span>
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

    <!-- Sidebar -->
    <GraphSidebar v-model="sidebarOpen"
                  :node="sidebarData"
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
