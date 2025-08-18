<script setup>
  import { onMounted, onUnmounted, ref, computed } from "vue"
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

  // --- Refs / State ---
  const container = ref(null)
  const graph = new Graph()
  let sigmaInstance = null
  let fa2 = null
  let highlightedNode = ref(null)
  let signalrController = null
  const signalrStatus = ref("disconnected")

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
</script>

<template>
  <header>
  </header>

  <main>
    <div v-if="graphId">
      Graph: {{ graphId }}
      <span style="margin-left: 1rem; font-weight: bold;">
        Status: {{ signalrStatus }}
      </span>
    </div>
    <div id="graph-container" ref="container" style="height: 100vh;"></div>
  </main>
</template>

<style scoped>
  body {
    margin: 0;
  }

  #graph-container {
    width: 100%;
    height: 100vh;
  }

</style>
