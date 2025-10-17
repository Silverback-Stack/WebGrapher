const configs = {
  development: {
    graphUrl: "http://localhost:5000/api",
    hubUrl: "http://localhost:5100/graphstreamerhub" //points to microservice which negotiates with azure instance
  },
  production: {
    graphUrl: "https://my-production-api.com/api",
    hubUrl: "https://my-production-api.com/graphstreamerhub"
  }
};

// Choose config based on environment
const env = import.meta.env.MODE || "development";

const { graphUrl, hubUrl } = configs[env];

export default {
  GRAPH_LIST: `${graphUrl}/Graph/list`,
  GRAPH_GET: (graphId) => `${graphUrl}/Graph/${graphId}`,
  GRAPH_CREATE: `${graphUrl}/Graph/create`,
  GRAPH_UPDATE: (graphId) => `${graphUrl}/Graph/${graphId}/update`,
  GRAPH_DELETE: (graphId) => `${graphUrl}/Graph/${graphId}/delete`,
  GRAPH_POPULATE: (graphId) => `${graphUrl}/Graph/${graphId}/populate`,
  GRAPH_NODESUBGRAPH: (graphId) => `${graphUrl}/Graph/${graphId}/node-subgraph`,
  GRAPH_CRAWL: (graphId) => `${graphUrl}/Graph/${graphId}/crawl`,
  SIGNALR_HUB: hubUrl
};
