# E-Commerce Order System — From Monolith to Microservices

[![CI](https://github.com/HodayaFridland/Ai-course/actions/workflows/ci.yml/badge.svg)](https://github.com/HodayaFridland/Ai-course/actions/workflows/ci.yml)

A .NET 8 e-commerce order system evolved from a monolith into distributed, production-style
microservices: containers, polyglot persistence, API gateway, BFF, load balancing, asynchronous
messaging, a choreography saga, distributed caching, and centralized observability.

## Run it on any machine — from zero

You do **not** need .NET, SQL Server, MongoDB, Redis or RabbitMQ installed. Everything runs in
Docker containers. The only thing you install is Docker.

**1. Install Docker Desktop** (once).
- Windows / macOS: download from https://www.docker.com/products/docker-desktop and install.
- Linux: install Docker Engine + the Compose plugin.
- Start Docker Desktop and wait until it says **"Engine running"**. Verify in a terminal:
  ```bash
  docker --version
  docker compose version
  ```

**2. Get the code** (either one):
```bash
# option A — clone with git
git clone https://github.com/HodayaFridland/Ai-course.git
cd Ai-course

# option B — download the ZIP from GitHub ("Code" ▸ "Download ZIP"), unzip, and open a
# terminal inside the unzipped folder.
```

**3. Start everything with one command** (from the folder that contains `docker-compose.yml`):
```bash
docker compose up --build
```
The first run downloads the base images (SQL Server, Mongo, Redis, RabbitMQ, Seq ≈ a few GB) and
builds the services — give it a few minutes. When you see the services logging
`Now listening on: http://0.0.0.0:8080`, it's ready.

**4. Open the web UI** in a browser: **http://localhost:5204**

**5. Stop it** when you're done (in another terminal, same folder):
```bash
docker compose down          # stop and remove the containers
# docker compose down -v     # ...also wipe the databases (start fresh next time)
```

> Tips: make sure Docker Desktop has enough disk space (SQL Server alone is ~2.3 GB), and that
> ports 5204 / 8081 / 15672 are free. If a build fails on a network timeout, just run
> `docker compose up --build` again — it resumes from where it stopped.

## One-command startup

```bash
docker compose up --build
```

That's it — the single root `docker-compose.yml` starts every database, the message broker, the
log aggregator, and all services.

> First run downloads the base images (SQL Server, Mongo, Redis, RabbitMQ, Seq) — give it a few
> minutes. After that it starts in seconds.

## Open these

| What | URL | Notes |
|------|-----|-------|
| **Web UI** (dashboard) | http://localhost:5204 | the client talks **only** to the gateway |
| **Seq** — all logs | http://localhost:8081 | search by `CorrelationId` |
| **RabbitMQ** — broker | http://localhost:15672 | guest / guest |

The individual services are **not** exposed to the host — the gateway (`:5204`) is the only public
entry point.

## The services

| Service | Role | Data store | Why |
|---------|------|-----------|-----|
| **ProductCatalogService** (×2 replicas) | browse products | **MongoDB** (document) | flexible per-category attributes |
| **OrderService** | place & track orders | **SQL Server** (relational) | money needs ACID |
| **InventoryService** | reserve stock | **SQL Server** (relational) | stock correctness needs ACID |
| **NotificationService** | notify the customer | **MongoDB** (document) | append-only log, BASE is fine |
| **GatewayService** | single entry point + web UI | — | YARP reverse proxy |
| **BffService** | aggregate order + product for the client | — | one call instead of two |

Supporting infrastructure: **Redis** (distributed cache), **RabbitMQ** (message broker),
**Seq** (structured-log aggregator).

## What each phase delivers

- **Phase 1 — Monolith baseline.** See [docs/phase1-monolith.md](docs/phase1-monolith.md).
- **Phase 2 — Microservices + polyglot persistence.** Database-per-service; ADRs in
  [docs/adr-databases.md](docs/adr-databases.md).
- **Phase 3 — Gateway, BFF, load balancing.** All traffic enters through YARP; the catalog runs as
  two replicas behind a round-robin load balancer (proven via the `X-Instance-Id` header).
- **Phase 4 — Async messaging, saga, caching.** The order flow is an asynchronous **choreography
  saga** over RabbitMQ (`OrderPlaced → InventoryReserved/Rejected → OrderConfirmed/Cancelled →
  Notification`), with Redis cache-aside on catalog reads and idempotent consumers.
- **Phase 5 — Observability.** Serilog in every service aggregated to **Seq**; `/health` endpoints
  wired into compose healthchecks; a single **correlation id** traces one order across all services
  and through the broker.

## Technology substitutions (all documented)

| Chosen | Instead of (class default) | Why — see [docs/architecture.md](docs/architecture.md) |
|--------|---------------------------|--------|
| **YARP** | Ocelot | .NET-native reverse proxy, config-driven, built-in load balancing |
| **Docker built-in LB + YARP round-robin** | Nginx | no extra component; round-robin proven with a header |
| **Seq** | ELK | one lightweight container, first-class structured-log + correlation search |

RabbitMQ, Redis and MongoDB are the class-default technologies and were kept.

## Demo evidence

Live-captured logs for the saga happy path, the compensation path, cache hit/miss, and a
fully-traced correlation id are in [docs/demo-evidence.md](docs/demo-evidence.md).

## Try it yourself (through the gateway)

```bash
# browse products
curl http://localhost:5204/api/products

# place an order (returns Pending immediately, becomes Confirmed via the saga)
curl -X POST http://localhost:5204/api/orders -H "Content-Type: application/json" \
  -d '{"userId":1,"shippingAddress":"Tel Aviv","items":[{"productId":"tshirt","quantity":1}]}'

# order an out-of-stock product to see the compensation (Cancelled)
curl -X POST http://localhost:5204/api/orders -H "Content-Type: application/json" \
  -d '{"userId":1,"shippingAddress":"Tel Aviv","items":[{"productId":"mouse","quantity":1}]}'

# BFF: order details aggregated from Order + Catalog
curl http://localhost:5204/api/bff/orders/1/details
```
