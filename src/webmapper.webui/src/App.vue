<script setup>
  import { ref, onMounted } from 'vue'
  import * as signalR from '@microsoft/signalr'
  import Sigma from 'sigma'
  import Graph from 'graphology'
  import forceAtlas2 from 'graphology-layout-forceatlas2'
  import FA2Layout from 'graphology-layout-forceatlas2/worker'

  const container = ref(null)
  const graph = new Graph()
  let sigmaInstance = null
  let fa2 = null
  const graphId = '1'

  onMounted(async () => {

    sigmaInstance = new Sigma(graph, container.value)

    // Prepare ForceAtlas2 worker layout (keeps running in background)
    fa2 = new FA2Layout(graph, {
      settings: {
        slowDown: 10,
        gravity: 5,
        scalingRatio: 1,
        adjustSizes: true
      }
    })

    try {
      await connection.start()
      console.log('SignalR connected')

      // Join the graph group on the server
      await connection.invoke('JoinGraphGroup', graphId)

    }
    catch (err) {
      console.error('SignalR connection error:', err)
    }
  })


  // Setup SignalR connection
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5001/graphstreamerhub')
    .withServerTimeout(120000)
    .withAutomaticReconnect()
    .build()


  function getRandomPosition(maxRange = 5) {
    return {
      x: (Math.random() * 2 - 1) * maxRange, // random between -maxRange and +maxRange
      y: (Math.random() * 2 - 1) * maxRange
    }
  }

  // Receive messages from server
  connection.on('ReceiveMessage', (message) => {
    console.log('Received message:', message);
  })

  // Receive streamed nodes from server
  connection.on('ReceiveNode', (node) => {
    // Update/Add nodes
    console.log('Received node:', node);

    node.nodes.forEach(node => {
      if (graph.hasNode(node.id)) {
        graph.mergeNodeAttributes(node.id, {
          label: node.label,
          size: node.size,
          color: node.state === 'Populated' ? '#4CAF50' : '#888',
          keywords: node.keywords,
          tags: node.tags,
          sourceLastModified: node.sourceLastModified,
          createdAt: node.createdAt
        })
      }
      else {
        const pos = getRandomPosition(5)
        graph.addNode(node.id, {
          label: node.label,
          size: node.size,
          color: node.state === 'Populated' ? '#4CAF50' : '#888',
          keywords: node.keywords,
          tags: node.tags,
          sourceLastModified: node.sourceLastModified,
          createdAt: node.createdAt,
          x: pos.x,
          y: pos.y
        })
      }
    })

    // Add any new edges
    node.edges.forEach(edge => {
      if (!graph.hasEdge(edge.id)) {
        graph.addEdgeWithKey(edge.id, edge.source, edge.target);
        console.log(`Added edge ${edge.id} from ${edge.source} to ${edge.target}`);
      }
    });

    // Smoothly animate changes:
    // Run ForceAtlas2 for a short burst in background
    if (!fa2.isRunning()) {
      fa2.start()
      setTimeout(() => {
        fa2.stop()
      }, 3000) // run for 2 seconds after each update
    }


  })

</script>

<template>
  <header>
  </header>

  <main>
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
