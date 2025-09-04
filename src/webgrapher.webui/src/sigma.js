import Sigma from "sigma"
import { createNodeImageProgram } from "@sigma/node-image"
import FA2Layout from "graphology-layout-forceatlas2/worker"
import appConfig from "./config/app-config.js"


// --- Sigma setup ---
export function setupSigma(sigmaGraph, container) {
  const sigmaInstance = new Sigma(sigmaGraph, container, {
    defaultNodeType: "image",
    renderLabels: true,
    labelRenderedSizeThreshold: 30,
    defaultEdgeType: "arrow"
  })
  sigmaInstance.registerNodeProgram("image", createNodeImageProgram())
  return sigmaInstance
}

// --- ForceAtlas2 ---
export function setupFA2(sigmaGraph) {
  return new FA2Layout(sigmaGraph, {
    iterations: 100,
    settings: {
      slowDown: 20,
      gravity: 0.01,
      scalingRatio: 5,
      strongGravityMode: false,
      adjustSizes: true
    }
  })
}

// --- Node/Stage highlighting ---
export function setupHighlighting(sigmaGraph, sigmaInstance, runFA2, highlightedNode, openNodeSidebar, nodeSubGraph, graphId) {
  sigmaInstance.on("clickNode", async ({ node }) => {

    // Trigger Node Sidebar
    if (typeof openNodeSidebar === "function") {
      const nodeAttributes = sigmaGraph.getNodeAttributes(node)

      //highlight neighbourhood
      // Outgoing edges
      const outgoingIds = sigmaGraph.outNeighbors(node)
      const outgoingEdges = outgoingIds.map(id => {
        const targetNode = sigmaGraph.getNodeAttributes(id);
        return {
          id: id,
          title: targetNode?.label || id // fallback to URL if no label
        };
      });
      // Incoming edges
      const incomingIds = sigmaGraph.inNeighbors(node);
      const incomingEdges = incomingIds.map(id => {
        const sourceNode = sigmaGraph.getNodeAttributes(id);
        return {
          id: id,
          title: sourceNode?.label || id
        };
      });

      const nodeData = {
        ...nodeAttributes,
        id: node,
        outgoingEdges,
        incomingEdges
      }

      openNodeSidebar(nodeData)
    }

    if (highlightedNode.value === node) return

    if (highlightedNode.value) resetHighlightNodeNeighborhood(sigmaGraph, sigmaInstance)

    highlightedNode.value = node

    highlightNodeNeighborhood(sigmaGraph, sigmaInstance, node)

    runFA2(appConfig.fa2DurationFast_MS)

    // --- fetch subgraph for clicked node ---
    if (typeof nodeSubGraph === "function" && graphId.value) {
      try {
        await nodeSubGraph(graphId, node, sigmaGraph, runFA2)
      } catch (err) {
        console.error(`Failed to fetch subgraph for node ${node}:`, err)
      }
    }
  })

  sigmaInstance.on("clickStage", () => {
    if (highlightedNode.value) {
      resetHighlightNodeNeighborhood(sigmaGraph, sigmaInstance)
      highlightedNode.value = null
    }
  })
}

// --- Node sizing ---
// Dynamically adjusts node size based on incoming/outgoing edges
export function setupNodeSizing(sigmaGraph, sigmaInstance) {
  function updateNodeSize(nodeId) {
    const incoming = sigmaGraph.inEdges(nodeId).length
    const outgoing = sigmaGraph.outEdges(nodeId).length
    const baseSize = calculateNodeSize(incoming, outgoing)

    sigmaGraph.updateNodeAttributes(nodeId, oldAttr => ({
      ...oldAttr,
      _baseSize: baseSize,                  // update dynamic base size
      size: oldAttr._highlighted            // only enlarge if currently highlighted
        ? baseSize * appConfig.nodeSizeSelectedRatio //eg: 150%
        : baseSize
    }))
    sigmaInstance.refresh()
  }

  sigmaGraph.on("edgeAdded", ({ target }) => updateNodeSize(target))
  sigmaGraph.on("edgeDropped", ({ target }) => updateNodeSize(target))
}

// --- Reducer (image/circle switch by zoom) ---
export function setupReducer(sigmaGraph, sigmaInstance, highlightedNodeRef) {
  const camera = sigmaInstance.getCamera()
  camera.on("updated", () => {
    const zoom = camera.ratio
    if (highlightedNodeRef.value) return

    sigmaGraph.forEachNode((node, attr) => {
      if (attr._originalType === "image") {
        const screenSize = attr.size / zoom
        const newType = screenSize < appConfig.minNodeSize ? "circle" : "image"                
        if (attr.type !== newType) {
          sigmaGraph.updateNodeAttributes(node, oldAttr => ({ ...oldAttr, type: newType }))
        }
      }
    })
    sigmaInstance.refresh()
  })
}



// --- Graph utils ---
export function addOrUpdateNode(sigmaGraph, n) {
  const pos = getRandomPosition(100);
  if (sigmaGraph.hasNode(n.id)) {
    sigmaGraph.updateNodeAttributes(n.id, oldAttr => ({
      _originalType: n.type, //used for reducer
      _originalSize: oldAttr._originalSize ?? oldAttr.size, // preserve highlight base
      _baseSize: oldAttr._baseSize ?? n.size,               // preserve dynamic base
      size: oldAttr.size,
      label: n.label,        
      color: oldAttr.color,  // preserve current color (highlighted or not)
      image: n.image,        
      type: n.type,
      summary: n.summary,
      tags: n.tags,
      sourceLastModified: n.sourceLastModified,
      createdAt: n.createdAt,
      x: oldAttr.x,          // preserve current x
      y: oldAttr.y           // preserve current y
    }))
  } else {
    sigmaGraph.addNode(n.id, {
      _originalType: n.type,   // for reducer
      _originalSize: n.size,   // for highlight reset
      _baseSize: n.size,       // for dynamic base size
      label: n.label,
      size: n.size,
      color: appConfig.nodeColor,
      image: n.image,
      opacity: 1,
      type: n.type,
      summary: n.summary,
      tags: n.tags,
      sourceLastModified: n.sourceLastModified,
      createdAt: n.createdAt,
      x: pos.x,
      y: pos.y
    })
  }
}

export function addEdge(sigmaGraph, e) {
  const sourceId = sigmaGraph.hasNode(e.source) ? e.source : null;
  const targetId = sigmaGraph.hasNode(e.target) ? e.target : null;
  if (!sourceId || !targetId) return;

  if (!sigmaGraph.hasEdge(e.id)) {
    sigmaGraph.addEdgeWithKey(e.id, sourceId, targetId);
  }
}


// Highlights the given node and its neighbors by:
// - Enlarging and emphasizing the selected node
// - Highlighting its direct neighbors
// - Visually de-emphasizing unrelated nodes (shrinking them and removing images)
// - Updating edges so that only those within the neighborhood are emphasized
export function highlightNodeNeighborhood(sigmaGraph, sigmaInstance, selectedNode) {
  const neighbors = new Set(sigmaGraph.neighbors(selectedNode));
  neighbors.add(selectedNode);

  // Update nodes
  sigmaGraph.forEachNode(n => {
    const isNeighbor = neighbors.has(n);

    sigmaGraph.updateNodeAttributes(n, oldAttr => {
      // Use dynamic base size for scaling
      const baseSize = oldAttr._baseSize ?? oldAttr.size;

      let newSize;
      if (n === selectedNode) {
        newSize = baseSize * appConfig.nodeSizeSelectedRatio // enlarge selected node
      } else if (isNeighbor) {
        newSize = baseSize;                    // keep neighbor size
      } else {
        newSize = appConfig.minNodeSize        // shrink non-neighbors
      }

      return {
        ...oldAttr,
        size: newSize,
        type: isNeighbor ? "image" : "circle",
        color: isNeighbor ? appConfig.nodeSelectedColor : appConfig.nodeColor
      }
    })
  })

  // Update edges
  sigmaGraph.forEachEdge((edge, attr, source, target) => {
    const isConnected = neighbors.has(source) && neighbors.has(target);
    sigmaGraph.updateEdgeAttributes(edge, oldAttr => ({
      ...oldAttr,
      color: isConnected ? appConfig.nodeEdgeSelectedSize : appConfig.nodeEdgeColor,
      size: isConnected ? appConfig.nodeEdgeSelectedSize : appConfig.nodeEdgeSize
    }))
  })

  sigmaInstance.refresh();
}


// Restores all nodes and edges to their default
// visual state, removing any highlighting applied
// by highlightNodeNeighborhood.
export function resetHighlightNodeNeighborhood(sigmaGraph, sigmaInstance) {
  // Reset all nodes to default image type, color, and original size
  sigmaGraph.forEachNode(n => {
    sigmaGraph.updateNodeAttributes(n, oldAttr => {
      return {
        ...oldAttr,
        type: "image",
        size: oldAttr._baseSize,  // restore to dynamic base size
        color: appConfig.nodeColor
      };
    });
  });

  // Reset all edges to default color and size
  sigmaGraph.forEachEdge((edge, attr) => {
    sigmaGraph.updateEdgeAttributes(edge, oldAttr => ({
      ...oldAttr,
      color: appConfig.nodeEdgeColor,
      size: 1
    }));
  });

  sigmaInstance.refresh();
}

// Pan/zoom Sigma camera to a node or the center of the graph.
export function panTo(sigmaGraph, sigmaInstance, node = null, options = {}) {
  const {
    duration = appConfig.panDuration_MS,
    ratio } = options;

  const camera = sigmaInstance.getCamera();

  // Compute graph bounding box
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

  // Determine target coordinates
  let targetX, targetY;
  if (node) {
    targetX = (node.x - bbox.minX) / (bbox.maxX - bbox.minX);
    targetY = (node.y - bbox.minY) / (bbox.maxY - bbox.minY);
  } else {
    // Center of the graph
    const centerX = (bbox.minX + bbox.maxX) / 2;
    const centerY = (bbox.minY + bbox.maxY) / 2;
    targetX = (centerX - bbox.minX) / (bbox.maxX - bbox.minX);
    targetY = (centerY - bbox.minY) / (bbox.maxY - bbox.minY);
  }

  camera.animate(
    { x: targetX, y: targetY, ratio: ratio ?? camera.ratio },
    { duration }
  );
}

function getRandomPosition(maxRange = 100) {
  return {
    x: (Math.random() * 2 - 1) * maxRange,
    y: (Math.random() * 2 - 1) * maxRange
  }
}

function calculateNodeSize(incoming, outgoing) {
  const minNodeSize = appConfig.minNodeSize
  const maxNodeSize = appConfig.maxNodeSize
  const total = incoming + outgoing

  return (
    minNodeSize +
    (Math.log10(total + 1) * (maxNodeSize - minNodeSize)) / Math.log10(1000)
  )
}
