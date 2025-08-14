<script setup>
  import { ref, onMounted, computed } from 'vue'
  import { useRoute } from 'vue-router'
  import * as signalR from '@microsoft/signalr'
  import Sigma from 'sigma'
  import Graph from 'graphology'
  import forceAtlas2 from 'graphology-layout-forceatlas2'
  import FA2Layout from 'graphology-layout-forceatlas2/worker'
  import { createNodeImageProgram } from '@sigma/node-image'

  const container = ref(null)
  const graph = new Graph()
  let sigmaInstance = null
  let fa2 = null

  const route = useRoute()
  const graphId = computed(() => route.params.id || '1') // /Graph/:id

  onMounted(async () => {

    sigmaInstance = new Sigma(graph, container.value)
    sigmaInstance.registerNodeProgram('image', createNodeImageProgram())

    // Prepare ForceAtlas2 worker layout (keeps running in background)
    fa2 = new FA2Layout(graph, {
      iterations: 100,
      settings: {
        slowDown: 5,
        gravity: 1,
        scalingRatio: 10,
        strongGravityMode: true,
        adjustSizes: true
      }
    })

    try {
      await connection.start()
      console.log('SignalR connected')

      // Join the graph group on the server
      await connection.invoke('JoinGraphGroup', graphId.value)

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

    // Add or merge nodes
    node.nodes.forEach(n => {
      if (graph.hasNode(n.id)) {
        graph.mergeNodeAttributes(n.id, {
          label: n.label,
          size: n.size,
          color: n.state === 'Populated' ? '#4CAF50' : '#888',
          image: n.image,
          type: n.type,
          summary: n.summary,
          tags: n.tags,
          sourceLastModified: n.sourceLastModified,
          createdAt: n.createdAt
        });
      } else {
        const pos = getRandomPosition(5);
        graph.addNode(n.id, {
          label: n.label,
          size: n.size,
          color: n.state === 'Populated' ? '#4CAF50' : '#888',
          image: n.image,
          type: n.type,
          summary: n.summary,
          tags: n.tags,
          sourceLastModified: n.sourceLastModified,
          createdAt: n.createdAt,
          x: pos.x,
          y: pos.y
        });
      }
    });

    // Add edges (flattened)
    node.edges.forEach(e => {
      // Ensure both source and target exist in the graph
      const sourceId = graph.hasNode(e.source) ? e.source : null;
      const targetId = graph.hasNode(e.target) ? e.target : null;

      if (!sourceId || !targetId) {
        console.warn(`Skipped edge ${e.id}: source or target missing`, e);
        return;
      }

      if (!graph.hasEdge(e.id)) {
        graph.addEdgeWithKey(e.id, sourceId, targetId);
        console.log(`Added edge ${e.id} from ${sourceId} to ${targetId}`);
      }
    });

    // Smoothly animate changes:
    // Run ForceAtlas2 for a short burst in background
    if (!fa2.isRunning()) {
      fa2.start()
      setTimeout(() => {
        fa2.stop()
      }, 2000) // run for 2 seconds after each update
    }

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
