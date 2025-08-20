import Sigma from "sigma"
import { createNodeImageProgram } from "@sigma/node-image"
import FA2Layout from "graphology-layout-forceatlas2/worker"
import { highlightNeighbors, resetHighlight } from "./graph-utils.js"

// --- Sigma setup ---
export function setupSigma(graph, container) {
  const sigmaInstance = new Sigma(graph, container, {
    defaultNodeType: "image",
    renderLabels: true,
    labelRenderedSizeThreshold: 30
  })
  sigmaInstance.registerNodeProgram("image", createNodeImageProgram())
  return sigmaInstance
}

// --- ForceAtlas2 ---
export function setupFA2(graph) {
  return new FA2Layout(graph, {
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
export function setupHighlighting(graph, sigmaInstance, fa2, highlightedNode) {
  sigmaInstance.on("clickNode", ({ node }) => {
    if (highlightedNode.value === node) return

    if (highlightedNode.value) resetHighlight(graph, sigmaInstance)

    highlightedNode.value = node
    highlightNeighbors(graph, sigmaInstance, node)

    if (!fa2.isRunning()) {
      fa2.start()
      setTimeout(() => fa2.stop(), 250)
    }
  })

  sigmaInstance.on("clickStage", () => {
    if (highlightedNode.value) {
      resetHighlight(graph, sigmaInstance)
      highlightedNode.value = null
    }
  })
}

// --- Node sizing ---
export function setupNodeSizing(graph, sigmaInstance) {
  function updateNodeSize(nodeId) {
    const incoming = graph.inEdges(nodeId).length
    const outgoing = graph.outEdges(nodeId).length
    const baseSize = calculateNodeSize(incoming, outgoing)

    graph.updateNodeAttributes(nodeId, oldAttr => ({
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

  graph.on("edgeAdded", ({ target }) => updateNodeSize(target))
  graph.on("edgeDropped", ({ target }) => updateNodeSize(target))
}

