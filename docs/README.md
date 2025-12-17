# WebGrapher

## Overview

**WebGrapher** is a distributed, scalable, event-driven microservices platform for crawling web pages, extracting and modelling relational data as a graph, and streaming results in real time for interactive visualization.

The goal of this repository is to demonstrate architectural clarity in the design and implementation of **event-driven microservices**.

WebGrapher demonstrates how to apply:

- Clean Architecture  
- SOLID principles  
- Event-driven design  
- Adapter-based infrastructure abstraction  

to a non-trivial, real-world domain without over-reliance on frameworks or platform-specific code.

---

## Visual Overview

The following videos provide a visual and experiential overview of the WebGrapher platform. They are intended to complement the architectural documentation by showing how the system behaves in practice.

---

### 🎥 Product Walkthrough

A guided walkthrough of the WebGrapher user interface, highlighting real-time behaviour, graph interaction, and streaming updates as data flows through the system.

[![Product Walkthrough](/docs/webgrapher_product_walkthrough_screenshot.png)](https://youtu.be/VRlcI-ePMeQ)

▶ Watch on YouTube:  
[Product Walkthrough Video](https://youtu.be/VRlcI-ePMeQ)

---

### 🎥 Live System Demonstration

A live demonstration of the platform crawling a website and incrementally building a real-time, interactive graph of relationships.

[![Live System Demonstration](/docs/webgrapher_live_demo-screenshot.png)](https://youtu.be/xJisRk_AHo8)

▶ Watch on YouTube:  
[Live Demo Video](https://youtu.be/xJisRk_AHo8)

---

### 🎥 High-Level Architecture Overview

A conceptual walkthrough of the WebGrapher architecture, focusing on service responsibilities, event-driven communication, and system boundaries.

[![High-Level Architecture Overview](/docs/webgrapher_highlevel_architecture_screenshot.png)](https://youtu.be/cwHya5NpOBQ)

▶ Watch on YouTube:  
[High-Level Architecture Video](https://youtu.be/cwHya5NpOBQ)

---

## Architecture at a Glance

The system is composed of autonomous components that communicate **only through events**. There are no direct synchronous calls between services — all coordination happens via an event bus and pub/sub mechanisms.

The event-driven pattern enables:

- Loose coupling  
- Asynchronous, scalable operation  
- Failure isolation and resilience  

At its core, WebGrapher models the web as a graph:

- **Nodes** represent web pages  
- **Edges** represent hyperlinks between pages  

Each component contributes to an event pipeline that starts with crawling and ends with graph streaming.

---

## Clean Architecture & SOLID

The codebase adheres to Clean Architecture principles:

- Core logic is infrastructure-agnostic  
- Dependencies point inward  
- Business rules are isolated from delivery mechanisms  

By following SOLID principles — especially **Dependency Inversion** and **Interface Segregation** — the platform ensures:

- High testability  
- Clear separation of concerns  
- Flexible composition across environments  

Infrastructure concerns are introduced through interfaces and adapters, composed at the edge of the system.

---

## Supported Execution Models

WebGrapher is designed to support multiple execution and deployment models without changes to core service logic. Hosting, scaling, and infrastructure concerns are treated as external composition details.

### 🧰 In-Memory / Local

For development, testing, and architectural exploration, the platform can run entirely locally using in-memory implementations of:

- Event bus  
- Caching  
- Queues  

This mode exercises the full event-driven pipeline while remaining lightweight and dependency-free.

### 🖥 CLI-Hosted

Microservices can be orchestrated from a command-line host, allowing the entire platform to run as a single local process while preserving service boundaries and event-driven communication.

This execution model is intentionally decoupled from any specific web framework or container runtime.

### ⚙️ Worker Services (Local or Cloud)

Each microservice can also be hosted as an independent **Worker Service**, enabling true service isolation.

Worker Services can:

- Run locally for development
- Be containerised using Docker
- Be deployed to **Azure Container Apps**

In this model, each service can scale independently based on load, queue depth, or resource usage, while remaining fully asynchronous and loosely coupled.

---

## Client Application

The client is implemented as a Single Page Application (SPA) and is intentionally decoupled from backend hosting concerns.

The SPA can:

- Run locally during development
- Be deployed as an **Azure Static Web Application** for cloud hosting

It communicates with backend services via APIs and receives real-time updates through streaming hubs, allowing interactive visualisation of graph data as it is discovered.

---

## Adapter-Based Infrastructure Support

WebGrapher decouples core logic from implementation detail using adapters. Supported adapters include:

### Messaging & Event Bus
- In-Memory bus (local development)  
- Azure Service Bus  

### Caching
- In-Memory  
- Azure Redis  

### Graph Storage
- In-Memory  
- Azure Cosmos DB (Graph)  

### Streaming
- SignalR  
- Azure SignalR Service  

### Authentication
- Configuration-based local auth  
- Azure AD  
- Auth0  

Adapters can be swapped without touching core services, enabling seamless transition from local execution to cloud-based, horizontally scalable deployments.

---

## Philosophy & Audience

This repository is intended as an **architectural reference implementation** that:

- Illustrates how to build truly event-driven microservices  
- Shows how to enforce Clean Architecture boundaries in a realistic system  
- Demonstrates patterns such as adapter composition, factory setup, and dependency inversion  

It is not just a crawler application — it is a **case study of robust architecture in action**.

---

## Documentation

- 📘 **[Getting Started](/docs/getting-started.md)**  
  Local development setup, HTTPS configuration, and running the system
