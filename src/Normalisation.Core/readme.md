## Why These Libraries Live in Core

The Normalisation pipeline uses libraries such as **HtmlAgilityPack**, **LanguageDetection**, and **StopWord filtering**.  
Although these are third-party libraries, they are intentionally kept in **Core**.

**Reasoning:**
- They perform **pure, in-memory transformations** (no I/O, no external resources)
- They express **business/application logic**: how HTML content is interpreted and normalised
- They are part of *what the service does*, not *how it integrates*

In Clean Architecture, **Core is allowed to depend on libraries** when those libraries:
- model domain or application behavior
- are deterministic and side-effect free
- are not infrastructure concerns (databases, networks, files, queues)

Moving this logic to Infrastructure would invert the architecture by placing core behavior in an outer layer.

This is a **pragmatic application of Clean Architecture**: abstractions are introduced only when there is real volatility or a need to swap implementations.
