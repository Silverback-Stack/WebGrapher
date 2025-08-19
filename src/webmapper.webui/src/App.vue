<script setup>
  import { ref, computed, onMounted, onUnmounted } from "vue"
  import { useRoute } from "vue-router"
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

  // --- Refs / State ---
  const container = ref(null)
  const graph = new Graph()
  let sigmaInstance = null
  let fa2 = null
  let highlightedNode = ref(null)
  let signalrController = null
  const signalrStatus = ref("disconnected")
  const showGraphConnector = ref(false)

  const route = useRoute()
  const graphId = computed(() => route.params.id || '00000000-0000-0000-0000-000000000001') // /Graph/:id

  onMounted(async () => {
    sigmaInstance = setupSigma(graph, container.value)
    fa2 = setupFA2(graph)

    setupReducer(graph, sigmaInstance, highlightedNode)
    setupHighlighting(graph, sigmaInstance, fa2, highlightedNode)
    setupNodeSizing(graph, sigmaInstance)

    signalrController = await setupSignalR(graphId, {
      graph,
      fa2,
      onStatus: (status, info) => {
        console.log("SignalR status:", status, info)
        signalrStatus.value = status
      }
    })

  })

  onUnmounted(() => {
    signalrController?.dispose()
  })








  // example graph list
  const availableGraphs = ref([
    { id: '1', name: 'IMDB Movies', description: 'Map of IMDB movies and related people.', url: 'https://www.imdb.com' },
    { id: '2', name: 'Medical Conditions', description: 'Medical conditions from Mayo', url: 'https://www.mayoclinic.com' }
  ])

  function toggleGraphConnector() {
    console.log("toggle clicked")
    showGraphConnector.value = true
  }

  function connectToGraph(graph) {
    console.log("Connecting to graph", graph)
    showGraphConnector.value = false
  }


</script>

<template>
  <header>
    <b-navbar type="is-primary">
      <!-- Brand -->
      <template #brand>
        <b-navbar-item>
          <span class="has-text-weight-bold">
            WebMapper
          </span>
        </b-navbar-item>
      </template>

      <template #end>
        <!-- Left: New Graph (only when disconnected)-->
        <b-navbar-item v-if="signalrStatus !== 'connected'">
          <b-button type="is-light" outlined @click="toggleGraphCreator">
            <span class="icon">
              <i class="mdi mdi-plus-thick"></i>
            </span>
            <span>New</span>
          </b-button>
        </b-navbar-item>

        <!-- Connect to Graph -->
        <b-navbar-item>
          <b-button type="is-light" outlined @click="toggleGraphConnector">
            <span class="icon">
              <i class="mdi mdi-circle"
                 :class="{
                   'has-text-success': signalrStatus === 'connected',
                   'has-text-danger': signalrStatus !== 'connected'
                 }">
              </i>
            </span>
            <span>{{ signalrStatus === 'connected' ? 'Connected' : 'Connect' }}</span>
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

    <b-modal :model-value="showGraphConnector" @update:model-value="showGraphConnector = $event" custom-class="responsive-modal" trap-focus :width="'50%'">
      <GraphConnector :graphs="availableGraphs" @select="connectToGraph" />
    </b-modal>
  </main>
</template>

<style scoped>
</style>
