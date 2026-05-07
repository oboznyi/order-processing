# Order Processing Service (.NET 8)

Small .NET 8 microservice that accepts orders via HTTP, stores them in PostgreSQL, and processes them asynchronously through RabbitMQ.

## Scope

Implemented capabilities:

- `POST /api/orders` accepts an order and returns immediately (`202 Accepted`).
- Asynchronous order processing via MassTransit consumer (`ProcessOrderConsumer`).
- Inventory reservation with atomic SQL update.
- Discount calculation during processing (`DiscountService`).
- Persistent order state transitions (`Pending` -> `Processing` -> `Processed` / `Failed`).
- Idempotency on API side (`Idempotency-Key`) and consumer deduplication (`processed_messages`).
- Outbox publisher + cleanup background workers.
- Health checks and Prometheus metrics.

## Architecture Summary

- **API layer**: controllers + request validation + mapping.
- **Application layer**: commands, handlers, consumer, domain services.
- **Infrastructure layer**: EF Core (PostgreSQL), repositories, RabbitMQ transport, background services.
- **Messaging reliability**:
  - outbox table (`outbox_messages`) for reliable event publishing;
  - processed messages table (`processed_messages`) for at-least-once consumer deduplication.

## Run with Docker (recommended)

```bash
docker compose up --build
```

Detached mode:

```bash
docker compose up --build -d
```

Endpoints:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- RabbitMQ UI: `http://localhost:15672` (`guest` / `guest`)
- Prometheus UI: `http://localhost:9090`
- Metrics endpoint: `http://localhost:8080/metrics`

Quick metric check:

```bash
curl http://localhost:8080/metrics
```

## Run locally (without Docker)

Requirements:

- .NET 8 SDK
- PostgreSQL
- RabbitMQ

Run:

```bash
dotnet restore
dotnet run --project src/OrderProcessing.API/OrderProcessing.API.csproj
```

Configuration defaults are in `src/OrderProcessing.API/appsettings.json`.

## Tests

Run all tests:

```bash
dotnet test
```

Projects:

- `tests/OrderProcessing.API.UnitTests`
- `tests/OrderProcessing.API.IntegrationTests`

Integration tests use:

- real PostgreSQL container (`Testcontainers`)
- MassTransit InMemory harness for consumer-style integration tests

## API Example

Create order:

```http
POST /api/orders
Content-Type: application/json
Idempotency-Key: order-1
X-Correlation-Id: demo-correlation-id

{
  "customerId": "customer-123",
  "items": [
    { "productId": "1", "quantity": 2, "unitPrice": 25.00 },
    { "productId": "2", "quantity": 1, "unitPrice": 10.00 }
  ],
  "totalAmount": 60.00
}
```

Get order:

```http
GET /api/orders/{orderId}
```

Returns `404` when order does not exist.

## How to Test Normal Flow

The steps below validate the happy-path behavior after `docker compose up --build -d`.

### 1) Create an order

```bash
curl -X POST "http://localhost:8080/api/orders" ^
  -H "Content-Type: application/json" ^
  -H "Idempotency-Key: happy-flow-1" ^
  -H "X-Correlation-Id: demo-flow-1" ^
  -d "{\"customerId\":\"customer-1\",\"items\":[{\"productId\":\"1\",\"quantity\":2,\"unitPrice\":25.0},{\"productId\":\"2\",\"quantity\":1,\"unitPrice\":10.0}],\"totalAmount\":60.0}"
```

Expected: `202 Accepted` with JSON body containing `orderId` and `status`.

### 2) Poll order status by `orderId`

```bash
curl "http://localhost:8080/api/orders/{orderId}"
```

Expected state transition:

- `Pending` (or `Processing`) right after create
- `Processed` after asynchronous consumer completes

### 3) Validate idempotency (same key, same payload)

Repeat step 1 with the same `Idempotency-Key`.

Expected: response contains the same `orderId` as the first request.

### 4) Check metrics endpoint

```bash
curl "http://localhost:8080/metrics"
```

Expected counters:

- `orders_processed_total` should be greater than `0`
- `orders_failed_total` should remain `0` for happy path

### 5) Check Prometheus UI

Open `http://localhost:9090` and execute queries:

- `orders_processed_total`
- `orders_failed_total`

## Design Decisions

- **RabbitMQ + MassTransit**: selected for clear async processing semantics, good .NET integration, and built-in retry/consumer patterns.
- **Dead-letter queue (DLQ)**: after retry attempts are exhausted, failed messages are moved to a dedicated DLQ (`{QueueName}.dlq`) via a dead-letter exchange (`{QueueName}.dlx`) for safe inspection and replay.
- **PostgreSQL + EF Core**: simple relational persistence with migrations and transactional consistency.
- **Single project with folder-based architecture**: application layers are separated by folders (`Domain`, `Application`, `Infrastructure`, `Controllers`) instead of multiple `.csproj` files to keep this test task simple to run, navigate, and review.
- **Outbox pattern**: prevents losing events between DB commit and publish.
- **Idempotency key**: protects API from duplicate client retries.
- **Processed messages table**: protects consumer from duplicate broker deliveries.
- **Prometheus metrics**: provides visibility into processed/failed order counters.

## Trade-offs

- **Overhead vs reliability**: outbox + idempotency + processed-messages add complexity but improve robustness.
- **InMemory transport in integration tests**: faster and deterministic for consumer behavior, but not a full broker-level e2e simulation.
- **Auto migrations in Development**: convenient for local setup, but production should run migrations in controlled deployment steps.
- **Minimal security scope**: authentication/authorization is intentionally out of scope for this task.
- **Folders vs separate projects**: using folders reduces setup overhead and keeps the example compact, while multiple projects would provide stricter compile-time boundaries in larger production systems.

## Assumptions

- Inventory is pre-seeded with products `"1"` and `"2"`.
- This is a single-service bounded context (no distributed saga orchestration between multiple services).
- The goal is a complete demo implementation, not production-hardening.

## Operational Notes

- RabbitMQ settings: `RabbitMq` section.
- Failed messages are routed to `${RabbitMq:QueueName}.dlq` (for example, `order-processing-queue.dlq`) after retry policy is exhausted.
- PostgreSQL connection: `ConnectionStrings:Postgres`.
- Outbox cleanup settings: `OutboxCleanup` section.
- Prometheus scrape config: `prometheus.yml` at repository root.

## Docker Troubleshooting

If API logs briefly show `Broker unreachable: guest@rabbitmq:5672/`, it is usually startup timing while RabbitMQ is still booting. Compose includes RabbitMQ health checks and restart policy for API to recover automatically.

Useful commands:

```bash
docker compose logs -f rabbitmq
docker compose logs -f api
docker compose ps
```
