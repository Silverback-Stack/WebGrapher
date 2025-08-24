import Sigma from "sigma"
import { createNodeImageProgram } from "@sigma/node-image"
import FA2Layout from "graphology-layout-forceatlas2/worker"

// --- Colors ---
export const GraphColors = {
  Node: "#E0E0E0",
  NodeSelected: "#888888",
  Edge: "#E0E0E0",
  EdgeSelected: "#888888"
}

// --- Sigma setup ---
export function setupSigma(sigmaGraph, container) {
  const sigmaInstance = new Sigma(sigmaGraph, container, {
    defaultNodeType: "image",
    renderLabels: true,
    labelRenderedSizeThreshold: 30
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
      gravity: 0.1,
      scalingRatio: 5,
      strongGravityMode: false,
      adjustSizes: true
    }
  })
}

// --- Node/Stage highlighting ---
export function setupHighlighting(sigmaGraph, sigmaInstance, fa2, highlightedNode, handleSidebar) {
  sigmaInstance.on("clickNode", ({ node }) => {

    // Trigger sidebar
    if (typeof handleSidebar === "function") {
      const nodeAttributes = sigmaGraph.getNodeAttributes(node)

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

      handleSidebar(nodeData)
    }

    if (highlightedNode.value === node) return

    if (highlightedNode.value) resetHighlight(sigmaGraph, sigmaInstance)

    highlightedNode.value = node
    highlightNeighbors(sigmaGraph, sigmaInstance, node)

    if (!fa2.isRunning()) {
      fa2.start()
      setTimeout(() => fa2.stop(), 250)
    }
  })

  sigmaInstance.on("clickStage", () => {
    if (highlightedNode.value) {
      resetHighlight(sigmaGraph, sigmaInstance)
      highlightedNode.value = null
    }
  })
}

// --- Node sizing ---
export function setupNodeSizing(sigmaGraph, sigmaInstance) {
  function updateNodeSize(nodeId) {
    const incoming = sigmaGraph.inEdges(nodeId).length
    const outgoing = sigmaGraph.outEdges(nodeId).length
    const baseSize = calculateNodeSize(incoming, outgoing)

    sigmaGraph.updateNodeAttributes(nodeId, oldAttr => ({
      ...oldAttr,
      _baseSize: baseSize,
      size: oldAttr._highlighted ? baseSize * 2 : baseSize
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
        const newType = screenSize < 10 ? "circle" : "image"
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
  if (sigmaGraph.hasNode(n.id)) {
    sigmaGraph.mergeNodeAttributes(n.id, {
      _originalType: n.type, //used for reducer
      label: n.label,
      size: n.size,
      color: GraphColors.Node, // solid gray
      image: n.image,
      type: n.type,
      summary: n.summary,
      tags: n.tags,
      sourceLastModified: n.sourceLastModified,
      createdAt: n.createdAt
    })
  } else {
    const pos = getRandomPosition(100);

    sigmaGraph.addNode(n.id, {
      _originalType: n.type, //used for reducer
      label: n.label,
      size: n.size,
      color: GraphColors.Node, // solid gray
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

export function highlightNeighbors(sigmaGraph, sigmaInstance, hoveredNode) {
  const neighbors = new Set(sigmaGraph.neighbors(hoveredNode));
  neighbors.add(hoveredNode);

  // Update nodes
  sigmaGraph.forEachNode(n => {
    const isNeighbor = neighbors.has(n);

    sigmaGraph.updateNodeAttributes(n, oldAttr => {
      // Ensure _originalSize exists
      const baseSize = oldAttr._originalSize ?? oldAttr.size;
      let newSize = baseSize;

      if (n === hoveredNode) {
        newSize = baseSize * 1.5; // enlarge selected node
      } else if (!isNeighbor) {
        newSize = 10; // shrink non-neighbors
      }

      return {
        ...oldAttr,
        _originalSize: baseSize, // keep base size for future updates
        size: newSize,
        type: isNeighbor ? "image" : "circle",
        color: isNeighbor ? GraphColors.NodeSelected : GraphColors.Node
      };
    });
  });

  // Update edges
  sigmaGraph.forEachEdge((edge, attr, source, target) => {
    const isConnected = neighbors.has(source) && neighbors.has(target);
    sigmaGraph.updateEdgeAttributes(edge, oldAttr => ({
      ...oldAttr,
      color: isConnected ? GraphColors.EdgeSelected : GraphColors.Edge,
      size: isConnected ? 2 : 1
    }));
  });

  sigmaInstance.refresh();
}

export function resetHighlight(sigmaGraph, sigmaInstance) {
  // Reset all nodes to default image type, color, and original size
  sigmaGraph.forEachNode(n => {
    sigmaGraph.updateNodeAttributes(n, oldAttr => {
      const baseSize = oldAttr._originalSize ?? oldAttr.size; // fallback if _originalSize missing
      return {
        ...oldAttr,
        type: "image",
        size: baseSize,
        color: GraphColors.Node
      };
    });
  });

  // Reset all edges to default color and size
  sigmaGraph.forEachEdge((edge, attr) => {
    sigmaGraph.updateEdgeAttributes(edge, oldAttr => ({
      ...oldAttr,
      color: GraphColors.Edge,
      size: 1
    }));
  });

  sigmaInstance.refresh();
}

export function focusNode(sigmaGraph, sigmaInstance, node) {
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
  )

  // Convert node coordinates to normalized camera coordinates
  const normX = (node.x - bbox.minX) / (bbox.maxX - bbox.minX)
  const normY = (node.y - bbox.minY) / (bbox.maxY - bbox.minY)

  camera.animate(
    { x: normX, y: normY, ratio: camera.ratio },
    { duration: 250 }
  )
}

function getRandomPosition(maxRange = 100) {
  return {
    x: (Math.random() * 2 - 1) * maxRange,
    y: (Math.random() * 2 - 1) * maxRange
  }
}

function calculateNodeSize(incoming, outgoing) {
  const total = incoming + outgoing
  const minSize = 10
  const maxSize = 100

  return (
    minSize +
    (Math.log10(total + 1) * (maxSize - minSize)) / Math.log10(1000)
  )
}
