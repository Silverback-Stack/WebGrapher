// ENV values are set in root .env.* 
const apiUrl = import.meta.env.VITE_API_URL;
const hubUrl = import.meta.env.VITE_HUB_URL;

export default {
  AUTH_LOGIN: `${apiUrl}/Auth/login`,

  GRAPH_LIST: `${apiUrl}/Graph/list`,
  GRAPH_GET: (graphId) => `${apiUrl}/Graph/${graphId}`,
  GRAPH_CREATE: `${apiUrl}/Graph/create`,
  GRAPH_UPDATE: (graphId) => `${apiUrl}/Graph/${graphId}/update`,
  GRAPH_DELETE: (graphId) => `${apiUrl}/Graph/${graphId}/delete`,
  GRAPH_POPULATE: (graphId) => `${apiUrl}/Graph/${graphId}/populate`,
  GRAPH_NODESUBGRAPH: (graphId) => `${apiUrl}/Graph/${graphId}/node-subgraph`,
  GRAPH_CRAWL: (graphId) => `${apiUrl}/Graph/${graphId}/crawl`,

  PROXY_IMAGE: (imageUrl) => `${apiUrl}/Proxy/image?url=${imageUrl}`,

  SIGNALR_HUB: hubUrl
};
