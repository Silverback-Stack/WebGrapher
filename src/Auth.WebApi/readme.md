# Auth.WebApi – Architecture Notes

`Auth.WebApi` is a **shared Web API authentication module** used by multiple services (e.g. Graphing, Streaming).
It provides a common ASP.NET authentication and authorization pipeline.

## Why Identity Providers Live Here

Identity provider implementations (Local, AzureAD, Auth0):

* Depend on **ASP.NET / JWT middleware**
* Configure authentication via `IServiceCollection`
* Work with `ClaimsPrincipal` and HTTP 401 responses
* Are inherently **Web API–specific**

Because of this, they belong in the **Host / Delivery layer**, not Infrastructure.
Moving them to Infrastructure would introduce unnecessary ASP.NET dependencies and reduce clarity.

## Role of `IIdentityProvider`

`IIdentityProvider` exists to **isolate provider variation**, not to force a layer split.

It allows identity providers to be swapped via configuration while keeping all Web API–specific logic in one place.

## Why a Shared Auth.WebApi

Sharing `Auth.WebApi` across services provides:

* Consistent JWT validation and claims mapping
* A single authentication model across APIs
* Reusable authorization behavior
* One login → access multiple services

Each service simply calls:

```csharp
services.AddWebApiAuthentication(authConfig);
```

## Clean Architecture Alignment

* Core remains framework-agnostic
* Identity logic is isolated behind interfaces
* Framework-specific code stays in the outer layer

This is a **pragmatic and clean** application of Clean Architecture principles without forcing unnecessary abstraction through strict project separation.