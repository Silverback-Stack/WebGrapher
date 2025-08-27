const debug = {
  graphUrl: "http://localhost:5000/api",
  hubUrl: "http://localhost:5001/graphstreamerhub"
};

const release = {
  graphUrl: "https://my-production-api.com/api",
  hubUrl: "https://my-production-api.com/graphstreamerhub"
};

// Choose config based on environment
const env = import.meta.env.MODE === "production" ? release : debug;

export default {
  GRAPH_LIST: `${env.graphUrl}/Graph/list`,
  GRAPH_GET: (graphId) => `${env.graphUrl}/Graph/${graphId}`,
  GRAPH_CREATE: `${env.graphUrl}/Graph/create`,
  GRAPH_UPDATE: (graphId) => `${env.graphUrl}/Graph/${graphId}/update`,
  GRAPH_DELETE: (graphId) => `${env.graphUrl}/Graph/${graphId}/delete`,
  GRAPH_POPULATE: (graphId) => `${env.graphUrl}/Graph/${graphId}/populate`,
  GRAPH_NODESUBGRAPH: (graphId) => `${env.graphUrl}/Graph/${graphId}/node-subgraph`,
  GRAPH_CRAWL: (graphId) => `${env.graphUrl}/Graph/${graphId}/crawl`,
  SIGNALR_HUB: env.hubUrl
};
