# Architecture Document

An e-commerce order system built as .NET 8 microservices under a single `docker-compose.yml`.
This document covers the final architecture, the data-store ADRs, and the technology choices.

## 1. System diagram

```
                              ┌──────────────────────────────┐
   Browser / client  ───────► │   GatewayService (YARP)      │   :5204  (ONLY public port)
                              │   - routing                   │   also serves the web UI
                              │   - load balancing            │
                              └───┬───────────┬───────────┬───┘
                                  │           │           │
                 /api/products    │           │/api/*     │ /api/bff/*
                (round-robin)     │           │           │
              ┌───────────────────┘           │           └──────────────┐
              ▼                   ▼            ▼                          ▼
     ┌─────────────┐    ┌─────────────┐  ┌─────────────┐          ┌─────────────┐
     │  Catalog #1 │    │  Order      │  │ Inventory   │          │    BFF      │
     │  Catalog #2 │    │  Service    │  │ Service     │          │  (aggregates│
     │  (Mongo)    │    │  (SQL)      │  │ (SQL)       │          │  Order+Cat) │
     └──────┬──────┘    └──────┬──────┘  └──────┬──────┘          └─────────────┘
            │ cache-aside      │                │
            ▼                  │  events         │  events
        ┌───────┐             ▼   (RabbitMQ fanout exchange "orders.events")  ▼
        │ Redis │        ═══════════════════════════════════════════════════════
        └───────┘             ▲                                      ▲
                              │                                      │
                       ┌──────┴──────┐                        ┌──────┴────────┐
                       │ Order saga  │                        │ Notification  │
                       │ consumer    │                        │ Service (Mongo)│
                       └─────────────┘                        └───────────────┘

   Every service → Serilog → Seq (:8081)     |     Databases: SQL Server, MongoDB
```

Infrastructure containers: **SQL Server**, **MongoDB**, **Redis**, **RabbitMQ**, **Seq**.

## 2. The order saga (Phase 4, choreography)

Placing an order is asynchronous — the POST returns `Pending` immediately and the outcome is
decided by events on RabbitMQ:

```
OrderService      publishes  OrderPlaced
InventoryService  reserves stock (all-or-nothing, ACID)
                  publishes  InventoryReserved   OR   InventoryRejected
OrderService      InventoryReserved -> status Confirmed -> publishes OrderConfirmed
                  InventoryRejected -> status Cancelled -> publishes OrderCancelled   (compensation)
NotificationService  records a Confirmed / Rejected notification for the customer
```

- **Topology:** one durable **fanout** exchange `orders.events`; each service owns a durable queue
  bound to it. Each service reacts only to the event types it cares about.
- **Idempotency (at-least-once delivery):** Inventory remembers processed order ids; Order only acts
  when the order is still `Pending`; Notification skips a notification that already exists. A
  duplicated message is therefore a harmless no-op.
- **Correlation id:** generated when the order is placed and carried in the AMQP message properties,
  so it survives the trip **through the broker** — one id traces the whole saga in Seq.

## 3. Database ADRs (Phase 2)

Full ADRs in [adr-databases.md](adr-databases.md). Summary:

| Service | Family | Model | CAP / consistency | Why |
|---------|--------|-------|-------------------|-----|
| ProductCatalog | Document (**MongoDB**) | schema-flexible documents | AP / BASE (eventual) | products have different attributes per category; reads dominate; a slightly stale catalog is acceptable |
| Order | Relational (**SQL Server**) | tables + transactions | CP / **ACID** | money and order state must be atomic and consistent |
| Inventory | Relational (**SQL Server**) | tables + transactions | CP / **ACID** | stock reservation must be all-or-nothing to avoid overselling |
| Notification | Document (**MongoDB**) | append-only documents | AP / BASE | a notification log tolerates eventual consistency |
| Cache | Key-value (**Redis**) | strings with TTL | AP, cache-aside | speed up hot catalog reads; a second NoSQL family |

**Cache-aside + invalidation:** reads check Redis first (HIT) else load Mongo and populate the key
(MISS); writes (`PUT`/`DELETE`/`POST`) evict `products:all` and `product:{id}` so the next read is a
fresh MISS. Both catalog replicas share one Redis, so the cache is coherent across them.

## 4. Gateway vs BFF (Phase 3)

- **Gateway (YARP):** cross-cutting concerns — routing and load balancing. It does *not* understand
  the domain; it forwards `/api/*` to the right service. It is the only public entry point.
- **BFF:** client-shaping — it *aggregates* data (`/api/bff/orders/{id}/details` = order from
  OrderService **+** live product data from ProductCatalogService) into one response the web client
  can render directly. Domain-aware, client-specific.

**Load balancing:** ProductCatalogService runs as two replicas; the gateway round-robins across
them. Each response carries an `X-Instance-Id` header (the container id) so alternation is provable.

## 5. Technology choices & substitutions

Everything the assignment named could be substituted with justification. Kept vs. substituted:

| Concern | Chosen | Class default | Justification |
|---------|--------|---------------|---------------|
| Gateway | **YARP** | Ocelot | .NET-native, config-driven reverse proxy with built-in load-balancing policies; one less framework to learn than Ocelot and a first-class fit for the .NET stack. |
| Load balancer | **YARP round-robin + Docker DNS** | Nginx | avoids adding an Nginx container; round-robin is proven with the `X-Instance-Id` header. Nginx would add a second proxy layer for no extra benefit at this scale. |
| Log aggregator | **Seq** | ELK (Elasticsearch/Kibana) | a single lightweight container with first-class **structured** log storage and a query language that filters directly on properties like `CorrelationId` — ideal for tracing one saga. ELK is far heavier (JVM + multiple containers) for the same goal here. |
| Message broker | **RabbitMQ** | (kept) | class default; simple, reliable pub/sub with durable queues — exactly what a small choreography saga needs. We did **not** go off-script here, so no half-page broker comparison is required. |
| Cache | **Redis** | (kept) | class default; the canonical distributed cache and a NoSQL key-value store. |
| Catalog / Notification DB | **MongoDB** | (kept) | class default document DB. |
| Order / Inventory DB | **SQL Server** | (kept) | class default relational DB for ACID. |

**Why RabbitMQ and not Kafka:** this system needs task-style messaging (reserve this order) with
per-message acknowledgement and simple routing, not a high-throughput replayable event log. RabbitMQ
delivers that with less operational weight than Kafka, so we stayed with the class default.

## 6. Observability (Phase 5)

- **Serilog** in every service writes structured events to the console **and** to **Seq**
  (`http://seq:5341`), each tagged with a `Service` property.
- **/health** endpoint per service, wired into `docker-compose` healthchecks (all 12 containers
  report healthy).
- **Correlation id** flows end-to-end (HTTP → publish → broker → consume) so a single order's full
  journey is one filtered query in Seq. See [demo-evidence.md](demo-evidence.md).

## 7. CI/CD (bonus)

Pipeline: **GitHub Actions** (`.github/workflows/ci.yml`), chosen because the repository is already
on GitHub — the CI is native, needs no extra hosting, and the status badge lives right in the
README. On every push and pull request it:

1. **Builds and runs the xUnit tests**; a failing test fails the pipeline (so a bad test blocks a
   merge).
2. **Detects which service folders changed** (via `dorny/paths-filter`) and **only rebuilds those**
   services' Docker images — no need to rebuild all six on every push.
3. **Builds a Docker image per changed service, tagged with the commit SHA**.
4. **Smoke test:** brings the whole stack up with `docker compose up`, waits for the gateway
   `/health` to be healthy, then tears it down.
