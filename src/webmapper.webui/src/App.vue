<script setup>
  import { ref, onMounted, computed } from 'vue'
  import { useRoute } from 'vue-router'
  import * as signalR from '@microsoft/signalr'
  import Sigma from 'sigma'
  import Graph from 'graphology'
  import forceAtlas2 from 'graphology-layout-forceatlas2'
  import FA2Layout from 'graphology-layout-forceatlas2/worker'
  import { createNodeImageProgram } from '@sigma/node-image'
  import { getRandomPosition, addOrUpdateNode, addEdge, highlightNeighbors, resetHighlight } from './graphUtils.js'

  const container = ref(null)
  const graph = new Graph()
  let sigmaInstance = null
  let fa2 = null

  const route = useRoute()
  const graphId = computed(() => route.params.id || '1') // /Graph/:id
  let highlightedNode = null

  // Setup SignalR connection
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5001/graphstreamerhub')
    .withServerTimeout(120000)
    .withAutomaticReconnect()
    .build()

  function explodeLayout() {
    if (fa2.isRunning()) fa2.stop();

    // Run a fresh layout burst with stronger spacing
    const positions = forceAtlas2(graph, {
      iterations: 100,
      settings: {
        gravity: 1,
        scalingRatio: 100,
        strongGravityMode: true,
        adjustSizes: true
      }
    });

    graph.updateEachNodeAttributes((node, attr) => ({
      ...attr,
      x: positions[node].x,
      y: positions[node].y
    }));

    sigmaInstance.refresh();
  }


  onMounted(async () => {

    sigmaInstance = new Sigma(graph, container.value, {
      defaultNodeType: "image",
      renderLabels: true,
      labelRenderedSizeThreshold: 30
    });

    sigmaInstance.registerNodeProgram("image", createNodeImageProgram());

    // ForceAtlas2 worker
    fa2 = new FA2Layout(graph, {
      iterations: 100,
      settings: {
        slowDown: 5,
        gravity: 0.5,
        scalingRatio: 10,
        strongGravityMode: true,
        adjustSizes: true
      }
    })

    //// Hover highlight events
    //sigmaInstance.on('enterNode', ({ node }) => {
    //  highlightNeighbors(graph, sigmaInstance, node);
    //});

    //sigmaInstance.on('leaveNode', () => {
    //  resetHighlight(graph, sigmaInstance);
    //});

    // Node click

    sigmaInstance.on("clickNode", ({ node }) => {
      if (highlightedNode === node) return; // already highlighted

      if (highlightedNode) resetHighlight(graph, sigmaInstance); // reset previous

      highlightedNode = node;
      highlightNeighbors(graph, sigmaInstance, node);
    });

    sigmaInstance.on("clickStage", () => {
      if (highlightedNode) {
        console.log("Background click, resetting highlight");
        resetHighlight(graph, sigmaInstance);
        highlightedNode = null;
      }
    });


    try {
      await connection.start()
      await connection.invoke('JoinGraphGroup', graphId.value)
      console.log('SignalR connected')
    }
    catch (err) {
      console.error('SignalR connection error:', err)
    }

    // Receive messages from server
    connection.on('ReceiveMessage', (message) => {
      console.log('Received message:', message);
    })

    connection.on('ReceiveNode', (node) => {
      node.nodes.forEach(n => addOrUpdateNode(graph, n));
      node.edges.forEach(e => addEdge(graph, e));

      if (!fa2.isRunning()) {
        fa2.start();
        setTimeout(() => fa2.stop(), 2000);
      }
    });

  })
</script>

<template>
  <header>
  </header>

  <main>
    <div v-if="graphId">Graph: {{ graphId }}</div>
    <button @click="explodeLayout" style="position: absolute; top: 10px; left: 10px; z-index: 10;">
      Explode Layout
    </button>
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
