import Sigma from "sigma"
import { createNodeImageProgram } from "@sigma/node-image"
import FA2Layout from "graphology-layout-forceatlas2/worker"
import { highlightNeighbors, resetHighlight } from "./sigma-graph-utils.js"

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


//Edge selection



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

  function calculateNodeSize(incoming, outgoing) {
    const total = incoming + outgoing
    const minSize = 10
    const maxSize = 100

    return (
      minSize +
      (Math.log10(total + 1) * (maxSize - minSize)) / Math.log10(1000)
    )
  }

  sigmaGraph.on("edgeAdded", ({ target }) => updateNodeSize(target))
  sigmaGraph.on("edgeDropped", ({ target }) => updateNodeSize(target))
}

