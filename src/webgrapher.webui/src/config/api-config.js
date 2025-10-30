// ENV values are set in root .env.* 
const graphUrl = import.meta.env.VITE_GRAPH_URL;
const hubUrl = import.meta.env.VITE_HUB_URL;

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
