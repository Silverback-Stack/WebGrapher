
export const GraphColors = {
  Node: "#E0E0E0",
  NodeSelected: "#888888",
  Edge: "#E0E0E0",
  EdgeSelected: "#888888"
}

function getRandomPosition(maxRange = 100) {
  return {
    x: (Math.random() * 2 - 1) * maxRange,
    y: (Math.random() * 2 - 1) * maxRange
  }
}

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


// Remove or restore images from nodes as user zooms in or out to boost performance
export function setupReducer(sigmaGraph, sigmaInstance, highlightedNodeRef) {
  const camera = sigmaInstance.getCamera();

  camera.on('updated', () => {
    const zoom = camera.ratio;

    // Skip reducer if any node is highlighted
    if (highlightedNodeRef.value) return;

    sigmaGraph.forEachNode((node, attr) => {
      // Only apply to image nodes
      if (attr._originalType === 'image') {
        const screenSize = attr.size / zoom;
        const newType = screenSize < 10 ? 'circle' : 'image';

        if (attr.type !== newType) {
          //console.log(`Node ${node} type changed from ${attr.type} → ${newType}`);
          sigmaGraph.updateNodeAttributes(node, oldAttr => ({ ...oldAttr, type: newType }));
        }
      }
    });

    sigmaInstance.refresh();
  });
}
