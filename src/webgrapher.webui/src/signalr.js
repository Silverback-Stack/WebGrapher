import { ref } from "vue"
import * as signalR from "@microsoft/signalr"
import { addOrUpdateNode, addEdge } from "./sigma.js"
import appConfig from "./config/app-config.js"

// Create Controller
export async function initSignalRController(graphId, { sigmaGraph, runFA2, hubUrl, activityLogs, router }) {
  const status = ref("disconnected")
  const controller = await setupSignalR(graphId, { sigmaGraph, runFA2, hubUrl, onStatus: (s) => (status.value = s), activityLogs, router })
  controller.graphId = graphId
  controller.status = status
  return controller
}

export function disposeSignalR(controller) {
  controller?.dispose()
}

async function setupSignalR(graphId, { sigmaGraph, runFA2, hubUrl, flushInterval = 2000, onStatus, activityLogs, router }) {
  // Build connection
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => {
        // Add JWT token from localStorage
        return localStorage.getItem('jwt') || '';
      }
    })
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

    if (error && error.statusCode === 401) {
      // Jwt Token expired or invalid, redirect to login
      const currentUrl = window.location.href;
      router.push({ name: 'Login', query: { redirect: currentUrl } });
    }

    if (onStatus) onStatus("closed", error)
  })

  // Start connection with retry loop
  async function startConnection() {
    try {
      await connection.start()
      await connection.invoke("JoinGraphGroupAsync", graphId)
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

  // Receive node data
  connection.on("ReceiveGraphPayload", payload => {
    if (!payload) return

    payload.nodes.forEach(n => nodeBuffer.push(n))
    payload.edges.forEach(e => edgeBuffer.push(e))
  })

  // Receive activity messages
  connection.on("ReceiveGraphLog", payload => {
    // push log entry to array
    activityLogs.value.unshift(payload)
    // cull log entries
    if (activityLogs.value.length > appConfig.maxLogEntries) {
      activityLogs.value.pop() // drop oldest
    }
  })

  // Periodic flush
  flushTimer = setInterval(() => {
    if (nodeBuffer.length > 0 || edgeBuffer.length > 0) {
      nodeBuffer.forEach(n => addOrUpdateNode(sigmaGraph, n))
      edgeBuffer.forEach(e => addEdge(sigmaGraph, e))
      nodeBuffer = []
      edgeBuffer = []

      runFA2(appConfig.fa2DurationSlow_MS)
    }
  }, flushInterval)

  // Cleanup function
  function dispose() {
    clearInterval(flushTimer)
    connection.stop().catch(err => console.error("Error stopping SignalR:", err))
  }

  return { connection, dispose }
}
