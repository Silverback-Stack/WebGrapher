<script setup>
  import { ref, computed, onMounted, onUnmounted, watch } from "vue"
  import { useRoute, useRouter } from "vue-router"
  import * as signalR from "@microsoft/signalr"
  import Graph from "graphology"
  import {
    addOrUpdateNode,
    addEdge,
    setupReducer
  } from "./graph-utils.js"
  import {
    setupSigma,
    setupFA2,
    setupHighlighting,
    setupNodeSizing
  } from "./graph-setup.js"
  import { setupSignalR } from "./signalr-setup.js"
  import GraphConnector from './components/GraphConnector.vue'
  import GraphCreator from './components/GraphCreator.vue'

  // --- Refs / State ---
  const container = ref(null)
  const graph = new Graph()
  let sigmaInstance = null
  let fa2 = null
  let highlightedNode = ref(null)
  let signalrController = null
  const signalrStatus = ref("disconnected")
  const modalView = ref(null)
  const graphId = ref(null)

  const route = useRoute()
  const router = useRouter()

  watch(
    () => route.params.id,
    async (newId) => {
      if (newId) {
        graphId.value = newId

        if (!signalrController) {
          signalrController = await setupSignalR(newId, {
            graph,
            fa2,
            onStatus: (status) => (signalrStatus.value = status),
          })
        }

        modalView.value = null // hide modal if connected
      } else {
        modalView.value = "connector" // show connector when no id
      }
    },
    { immediate: true }
  )



  onMounted(async () => {
    sigmaInstance = setupSigma(graph, container.value)
    fa2 = setupFA2(graph)

    setupReducer(graph, sigmaInstance, highlightedNode)
    setupHighlighting(graph, sigmaInstance, fa2, highlightedNode)
    setupNodeSizing(graph, sigmaInstance)
  })

  onUnmounted(() => {
    signalrController?.dispose()
  })








  // example graph list
  const availableGraphs = ref([
    { id: '00000000-0000-0000-0000-000000000001', name: 'IMDB Movies', description: 'Map of IMDB movies and related people.', url: 'https://www.imdb.com' },
    { id: '00000000-0000-0000-0000-000000000002', name: 'Medical Conditions', description: 'Medical conditions from Mayo', url: 'https://www.mayoclinic.com' }
  ])

  // Navbar / modal triggers
  function openConnector() {
    modalView.value = "connector"
  }

  function openCreator() {
    modalView.value = "creator"
  }

  async function connectToGraph(selectedGraph) {
    console.log("Connecting to graph", selectedGraph)
    modalView.value = null

    // update the URL
    await router.push({ name: "Graph", params: { id: selectedGraph.id } })

    // dispose old connection if exists
    signalrController?.dispose()

    // update local state
    graphId.value = selectedGraph.id

    // connect
    signalrController = await setupSignalR(selectedGraph.id, {
      graph,
      fa2,
      onStatus: (status, info) => {
        console.log("SignalR status:", status, info)
        signalrStatus.value = status
      }
    })
  }


</script>

<template>
  <header>
    <b-navbar type="is-primary">
      <template #brand>
        <b-navbar-item>
          <span class="has-text-weight-bold">WebGrapher</span>
        </b-navbar-item>
      </template>

      <template #end>
        <!-- Connect -->
        <b-navbar-item>
          <b-button type="is-light" outlined @click="openConnector">
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

        <!-- New Graph -->
        <b-navbar-item v-if="signalrStatus !== 'connected'">
          <b-button type="is-light" outlined @click="openCreator">
            <span class="icon"><i class="mdi mdi-plus-thick"></i></span>
            <span>New</span>
          </b-button>
        </b-navbar-item>

        <!-- Graph Settings (only when connected) -->
        <b-navbar-item v-if="signalrStatus === 'connected'">
          <b-button type="is-light" outlined>
            <span class="icon">
              <i class="mdi mdi-cog"></i>
            </span>
            <span>Settings</span>
          </b-button>
        </b-navbar-item>

        <!-- Graph Activity (only when connected) -->
        <b-navbar-item v-if="signalrStatus === 'connected'">
          <b-button type="is-light" outlined>
            <span class="icon">
              <i class="mdi mdi-list-box"></i>
            </span>
            <span>Activity</span>
          </b-button>
        </b-navbar-item>

      </template>
    </b-navbar>
  </header>

  <main>
    <div id="graph-container" ref="container"></div>

    <!-- Single modal, swaps contents based on modalView -->
    <b-modal v-if="modalView !== null"
             :model-value="true"
             @update:model-value="modalView = null"
             custom-class="responsive-modal"
             trap-focus
             :has-modal-card="false">
        <GraphConnector v-if="modalView === 'connector'"
                        :graphs="availableGraphs"
                        @select="connectToGraph"
                        @createGraph="openCreator" />

        <GraphCreator v-if="modalView === 'creator'" />

        <!-- <GraphPreview v-if="modalView === 'preview'" /> -->
    </b-modal>
  </main>
</template>

<style scoped>
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
