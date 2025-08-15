
export const GraphColors = {
  Node: "#B0B0B0",                // default node
  NodeHover: "#888888",            // hovered node
  NodeHoverNeighbour: "#888888",   // hovered neighbor
  NodeHoverNonNeighbour: "#E0E0E0",// faded non-neighbor

  Edge: "#B0B0B0",                 // default edge
  EdgeHoverNeighbour: "#888888",   // edge between hovered + neighbor
  EdgeHoverNonNeighbour: "#E0E0E0" // other edges
};

export function getRandomPosition(maxRange = 5) {
  return {
    x: (Math.random() * 2 - 1) * maxRange,
    y: (Math.random() * 2 - 1) * maxRange
  };
}

export function addOrUpdateNode(graph, n) {
  if (graph.hasNode(n.id)) {
    graph.mergeNodeAttributes(n.id, {
      label: n.label,
      size: n.size,
      color: "rgba(136,136,136,1)", // solid gray
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
      color: "rgba(136,136,136,1)", // solid gray
      image: n.image,
      opacity: 1,
      type: n.type,
      summary: n.summary,
      tags: n.tags,
      sourceLastModified: n.sourceLastModified,
      createdAt: n.createdAt,
      x: pos.x,
      y: pos.y
    });
  }
}

export function addEdge(graph, e) {
  const sourceId = graph.hasNode(e.source) ? e.source : null;
  const targetId = graph.hasNode(e.target) ? e.target : null;
  if (!sourceId || !targetId) return;

  if (!graph.hasEdge(e.id)) {
    graph.addEdgeWithKey(e.id, sourceId, targetId);
  }
}


export function highlightNeighbors(graph, sigmaInstance, hoveredNode) {
  const neighbors = new Set(graph.neighbors(hoveredNode));
  neighbors.add(hoveredNode);

  // Update nodes
  graph.forEachNode(n => {
    const isNeighbor = neighbors.has(n);

    graph.updateNodeAttributes(n, oldAttr => {
      // Store original size for reset if not already stored
      if (oldAttr._originalSize === undefined) {
        oldAttr._originalSize = oldAttr.size;
      }

      let newSize = oldAttr._originalSize;

      if (n === hoveredNode) {
        newSize = oldAttr._originalSize * 1.5; // enlarge selected node
      } else if (!isNeighbor) {
        newSize = 10; // shrink non-neighbors
      }

      return {
        ...oldAttr,
        type: isNeighbor ? "image" : "circle",
        size: newSize,
        color: isNeighbor
          ? (n === hoveredNode ? GraphColors.NodeHover : GraphColors.NodeHoverNeighbour)
          : GraphColors.NodeHoverNonNeighbour
      };
    });
  });

  // Update edges
  graph.forEachEdge((edge, attr, source, target) => {
    const isConnected = neighbors.has(source) && neighbors.has(target);
    graph.updateEdgeAttributes(edge, oldAttr => ({
      ...oldAttr,
      color: isConnected
        ? GraphColors.EdgeHoverNeighbour
        : GraphColors.EdgeHoverNonNeighbour,
      size: isConnected ? 4 : 1
    }));
  });

  sigmaInstance.refresh();
}

export function resetHighlight(graph, sigmaInstance) {
  // Reset all nodes to default image type, color, and original size
  graph.forEachNode(n => {
    graph.updateNodeAttributes(n, oldAttr => ({
      ...oldAttr,
      type: "image",
      size: oldAttr._originalSize || oldAttr.size,
      color: GraphColors.Node
    }));
  });

  // Reset all edges to default color and size
  graph.forEachEdge((edge, attr) => {
    graph.updateEdgeAttributes(edge, oldAttr => ({
      ...oldAttr,
      color: GraphColors.Edge,
      size: 1
    }));
  });

  sigmaInstance.refresh();
}
