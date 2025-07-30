Add information here

todo: add licence 

Micro services architecture

[Crawler] → [Scraper] → [Parser] → [Normaliser] → [Graphing] → [Streaming] → [Browser Client] → Feeds crawl requests back to Crawler


- The Graphing service stores node and edge data, possibly publishing domain events like NodeAdded, EdgeCreated.

- The Streaming service subscribes to those events (via internal bus, pub/sub, or a lightweight queue like Redis Streams).
- 
- It pushes deltas to connected clients over SignalR, formatted for Sigma.js (JSON node/edge structure).
- 
- Clients can optionally request context snapshots or paginate historical data.


Streaming service:
SignalR Core: Great choice. Native to ASP.NET Core, supports scale-out via Redis backplane or Azure SignalR Service.


Message Bus: Lightweight message forwarding:
- In-memory
- Redis Pub/Sub
- Azure Service Bus / RabbitMQ for durability


- Client Format: JSON Graph schema { nodes: [], edges: [] } tuned for Sigma.js or Cytoscape.

Graphing Visualisations:
https://js.cytoscape.org/
https://www.sigmajs.org/