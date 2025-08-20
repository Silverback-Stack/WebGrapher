import * as signalR from "@microsoft/signalr"
import { addOrUpdateNode, addEdge } from "./graph-utils.js"

export async function setupSignalR(graphId, { graph, fa2, flushInterval = 2000, onStatus }) {
  // Build connection
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5001/graphstreamerhub") // 👈 adjust your hub URL
    .withServerTimeout(120000)
    .withAutomaticReconnect([0, 2000, 5000, 10000]) // exponential backoff (ms)
    .configureLogging(signalR.LogLevel.Information)
    .build()

  // Connection status events
  connection.onreconnecting(error => {
    console.warn("SignalR reconnecting...", error)
    if (onStatus) onStatus("reconnecting", error)
  })

  connection.onreconnected(connectionId => {
    console.log("SignalR reconnected", connectionId)
    if (onStatus) onStatus("reconnected", connectionId)
  })

  connection.onclose(error => {
    console.error("SignalR connection closed", error)
    if (onStatus) onStatus("closed", error)
  })

  // Start connection with retry loop
  async function startConnection() {
    try {
      await connection.start()
      await connection.invoke("JoinGraphGroup", graphId)
      console.log("SignalR connected")
      if (onStatus) onStatus("connected")
    } catch (err) {
      console.error("SignalR connection error:", err)
      if (onStatus) onStatus("error", err)
      // retry after delay
      setTimeout(startConnection, 5000)
    }
  }

  await startConnection()

  // Buffers for nodes and edges
  let nodeBuffer = []
  let edgeBuffer = []
  let flushTimer = null

  connection.on("ReceiveNode", payload => {
    payload.nodes.forEach(n => nodeBuffer.push(n))
    payload.edges.forEach(e => edgeBuffer.push(e))
  })

  connection.on("ReceiveMessage", message => {
    console.log("Received message:", message)
  })

  // Periodic flush
  flushTimer = setInterval(() => {
    if (nodeBuffer.length > 0 || edgeBuffer.length > 0) {
      console.log(`Flushing ${nodeBuffer.length} nodes and ${edgeBuffer.length} edges`)
      nodeBuffer.forEach(n => addOrUpdateNode(graph, n))
      edgeBuffer.forEach(e => addEdge(graph, e))
      nodeBuffer = []
      edgeBuffer = []

      if (fa2 && !fa2.isRunning()) {
        fa2.start()
        setTimeout(() => fa2.stop(), 1950)
      }
    }
  }, flushInterval)

  // Cleanup function
  function dispose() {
    clearInterval(flushTimer)
    connection.stop().catch(err => console.error("Error stopping SignalR:", err))
  }

  return { connection, dispose }
}
