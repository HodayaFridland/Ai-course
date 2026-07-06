# Demo Evidence

All evidence below was captured live from the running system (`docker compose up`), through the
API Gateway only (`http://localhost:5204`). Log lines are pulled from the services and from the
central **Seq** aggregator.

---

## (a) Saga — Happy Path

Placing an order returns **immediately** as `Pending`; the choreography saga then flips it to
`Confirmed` in the background (over RabbitMQ).

```
POST /api/orders  { userId:1, items:[{ productId:"book", quantity:1 }] }
-> 201  { "id":3004, "status":"Pending", ... }        # returns instantly

GET /api/orders/3004  ->  { "status":"Confirmed" }     # a moment later, via the saga
```

Full trace of this one order, **queried from Seq**, filtered by its correlation id
(`e3acc81d74a5425fabcab8a5a29d83af`) — the story crosses three services in order:

```
21:28:34.080 [OrderService       ] --> published OrderPlaced
21:28:34.080 [OrderService       ] Order 3004 placed (Pending); OrderPlaced published
21:28:34.092 [InventoryService   ] --> published InventoryReserved
21:28:34.092 [InventoryService   ] Order 3004 stock RESERVED
21:28:34.104 [OrderService       ] --> published OrderConfirmed
21:28:34.104 [OrderService       ] Order 3004 CONFIRMED
21:28:34.109 [NotificationService] NOTIFY user 1 about order 3004: [Confirmed] ...
```

---

## (b) Saga — Compensation Path (out-of-stock)

Ordering `mouse` (stock = 0) → the order is **Cancelled** and stock is untouched.

```
POST /api/orders  { items:[{ productId:"mouse", quantity:1 }] }
-> "Pending"  ->  (saga) ->  "Cancelled"

GET /api/inventory/mouse -> { availableStock:0, reservedStock:0 }   # unchanged
```

Compensation trace (correlation id `aa5dfcb1f25a49389612572c1d6b820a`):

```
[OrderService     ] --> published OrderPlaced
[OrderService     ] Order 3005 placed (Pending); OrderPlaced published
[InventoryService ] --> published InventoryRejected
[InventoryService ] Order 3005 stock REJECTED: Insufficient stock for product 'mouse'.
[OrderService     ] --> published OrderCancelled
[OrderService     ] Order 3005 CANCELLED (compensation): Insufficient stock for product 'mouse'.
[NotificationService] NOTIFY user 1 about order 3005: [Rejected] Your order #3005 was rejected ...
```

---

## (c) Redis Cache — Hit / Miss (cache-aside)

Two catalog replicas share **one** Redis, so a value cached by one replica is a hit for the other.

```
catalogservice-2 | Cache MISS for product:book  — reading from MongoDB and caching
catalogservice-1 | Cache HIT  for products:all
catalogservice-2 | Cache HIT  for products:all
```

**Invalidation strategy:** on `PUT`/`DELETE`/`POST` of a product, the affected keys
(`products:all`, `product:{id}`) are evicted from Redis, so the next read is a fresh MISS.

---

## (d) Correlation ID across the broker

The correlation id is generated when the order is placed and travels **inside the RabbitMQ message
properties** (not just HTTP headers), so it survives the trip through the broker. See section (a):
the same id `e3acc81d...` appears in OrderService, InventoryService and NotificationService logs,
retrieved from the single Seq aggregator.

---

## (e) Load Balancing (2 catalog replicas)

Calling `/api/products` repeatedly through the gateway alternates between the two catalog
containers (the `X-Instance-Id` response header = container id):

```
call 1 -> 1ef03ac55677
call 2 -> 89ebb1e41335
call 3 -> 1ef03ac55677
call 4 -> 89ebb1e41335
call 5 -> 1ef03ac55677
call 6 -> 89ebb1e41335
```

---

## (f) Health & container status

All 12 containers report healthy (`docker compose ps`), and every app service exposes `/health`
wired into a compose healthcheck:

```
sqlserver, mongo, redis, rabbitmq, seq,
catalogservice-1, catalogservice-2, inventoryservice,
notificationservice, orderservice, gatewayservice, bffservice   -> Up (healthy)
```

## Where to see it live
- Web UI (via gateway): http://localhost:5204
- Seq (all logs, searchable by CorrelationId): http://localhost:8081
- RabbitMQ management: http://localhost:15672  (guest / guest)
