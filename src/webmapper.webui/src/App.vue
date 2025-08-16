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
        slowDown: 20,
        gravity: 0.1,
        scalingRatio: 5,
        strongGravityMode: false,
        adjustSizes: true
      }
    })









    sigmaInstance.on("clickNode", ({ node }) => {
      if (highlightedNode === node) return; // already highlighted

      if (highlightedNode) resetHighlight(graph, sigmaInstance); // reset previous

      highlightedNode = node;
      highlightNeighbors(graph, sigmaInstance, node);

      // adjust layout burst animation
      if (!fa2.isRunning()) {
        fa2.start()
        setTimeout(() => fa2.stop(), 250);
      }
    });

    sigmaInstance.on("clickStage", () => {
      if (highlightedNode) {
        console.log("Background click, resetting highlight");
        resetHighlight(graph, sigmaInstance);
        highlightedNode = null;
      }
    });





    graph.on('edgeAdded', ({ edge, source, target }) => {
      updateNodeSize(target);
    });

    graph.on('edgeDropped', ({ edge, source, target }) => {
      updateNodeSize(target);
    });

    function updateNodeSize(nodeId) {
      const incomingLinks = graph.inEdges(nodeId).length;
      const outgoingLinks = graph.outEdges(nodeId).length;
      const baseSize = calculateNodeSize(incomingLinks, outgoingLinks);

      graph.updateNodeAttributes(nodeId, oldAttr => ({
        ...oldAttr,
        _baseSize: baseSize,               // store base size
        size: oldAttr._highlighted
          ? baseSize * 2                 // keep highlight enlargement if active
          : baseSize
      }));

      sigmaInstance.refresh();
    }

    function calculateNodeSize(incomingLinks, outgoingLinks) {
      const totalLinks = incomingLinks + outgoingLinks;
      const minSize = 10;
      const maxSize = 100;

      // Logarithmic scaling: prevents huge counts from exploding
      return minSize +
        (Math.log10(totalLinks + 1) * (maxSize - minSize) / Math.log10(1000));
    }






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


    let nodeBuffer = [];
    let edgeBuffer = [];

    connection.on('ReceiveNode', (node) => {
      node.nodes.forEach(n => nodeBuffer.push(n));
      node.edges.forEach(e => edgeBuffer.push(e));
    });

    // Flush every 3 seconds
    setInterval(() => {
      if (nodeBuffer.length > 0 || edgeBuffer.length > 0) {
        console.log(`Flushing ${nodeBuffer.length} nodes and ${edgeBuffer.length} edges`);

        nodeBuffer.forEach(n => addOrUpdateNode(graph, n));
        edgeBuffer.forEach(e => addEdge(graph, e));

        nodeBuffer = [];
        edgeBuffer = [];

        // Run FA2 once after flush
        if (!fa2.isRunning()) {
          fa2.start();
          setTimeout(() => fa2.stop(), 2000);
        }
      }
    }, 3000);







  })
</script>

<template>
  <header>
  </header>

  <main>
    <div v-if="graphId">Graph: {{ graphId }}</div>
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
