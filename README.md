# Chubb APAC Claims

A local claims application with a .NET API and an Angular frontend. Claimants can submit and track motor or property claims. Claims officers can pick up claims, adjust reserves, request information, and settle or reject claims.

The repositories should be next to each other:

```text
Chubb/
├── chubb-api/
└── chubb-frontend/
```

## Requirements

- .NET 10 SDK
- Node.js, npm, and the Angular CLI (`ng` command)
- Docker Desktop for local Kafka (Redpanda)
- VS Code with C# Dev Kit if you want to start the API with Run and Debug

## First-time setup

### 1. Restore the API packages

Open a terminal in `chubb-api`:

```bash
dotnet restore Chubb.Claims.sln
```

### 2. Start Redpanda and create the topic

From `chubb-api`:

```bash
docker compose up -d
docker compose ps
docker exec chubb-apac-kafka rpk topic create chubb.apac.claims.events
```

Wait until `docker compose ps` reports `healthy` before creating the topic. You only need to create the topic once per container. If it already exists, continue to the next step. The API runs on your computer and connects to the broker through `localhost:19092`.

### 3. Start the API

Open `chubb-api.code-workspace` in VS Code. In **Run and Debug**, select the C# configuration for `Chubb.Claims.Api` and press **F5**. If the API is already running, stop that debug session before starting another one.

The `http` profile in `src/Chubb.Claims.Api/Properties/launchSettings.json` sets `Kafka__BootstrapServers=localhost:19092`. VS Code uses that setting when it starts the API with the project's launch profile. The API listens at `http://localhost:5150`.

You can also run it from a terminal instead of VS Code:

```bash
cd src/Chubb.Claims.Api
dotnet run
```

Check [Swagger UI](http://localhost:5150/swagger) after the API starts. On first startup, the API creates a SQLite database and adds sample claims.

### 4. Start the frontend

Open another terminal in the sibling `chubb-frontend` folder:

```bash
npm ci
ng serve
```

Open [the app](http://localhost:4200). The top bar switches between the claimant and claims officer views.

If `ng` is not found, install the Angular 19 CLI with `npm install -g @angular/cli@19`, then run `ng serve` again.

### 5. Check Kafka publishing

Open a third terminal and watch the topic:

```bash
docker exec -it chubb-apac-kafka rpk topic consume chubb.apac.claims.events
```

Submit or update a claim in the app. A new event should appear in the consumer. `GET http://localhost:5150/api/analytics/summary` also includes recent events and their `transportMode`; successful Kafka sends are labelled `Kafka (...)`.

If no event appears, confirm that the topic exists with `docker exec chubb-apac-kafka rpk topic list`, restart the API debug session, and perform a **new** claim action. Outbox messages already marked as processed are not sent again.

## Later runs

1. From `chubb-api`, run `docker compose up -d`.
2. Start the API with **F5** or `dotnet run` from `src/Chubb.Claims.Api`.
3. From `chubb-frontend`, run `ng serve`.

Use `docker compose stop` to stop Redpanda while keeping its container and topic. `docker compose down` removes the container; this Compose file has no data volume, so you must create the topic again after the next `up`. Neither command removes the API's SQLite database.

The API can also run without Docker. If Kafka is unavailable, event publishing falls back locally and SignalR updates still work. The current worker marks an outbox message processed after that fallback, so it will not later replay that message to Kafka.

## Data and configuration

- `src/Chubb.Claims.Api/appsettings.json` contains the SQLite connection string. `chubb_apac_claims.db` is created in the API process's working directory. Existing claims remain after a restart; sample data is added only when the database is empty.
- SQLite may create `.db-wal` and `.db-shm` files beside the database. Leave them in place while the API or a database viewer is running.
- `src/Chubb.Claims.Api/Properties/launchSettings.json` contains the VS Code debug profile and its Kafka address. If you use a separate VS Code `launch.json` that ignores this profile, set `Kafka__BootstrapServers` to `localhost:19092` in that configuration.
- The frontend expects the API at `http://localhost:5150` and connects to `/hubs/claims` for live updates.

## Project layout

| Path | Purpose |
| --- | --- |
| `src/Chubb.Claims.Api` | HTTP controllers, SignalR hub, and outbox worker |
| `src/Chubb.Claims.Application` | Claim commands, queries, request and response contracts |
| `src/Chubb.Claims.Domain` | Claim entities, statuses, and domain events |
| `src/Chubb.Claims.Infrastructure` | SQLite persistence, FX rates, and Kafka producer |
| `tests/Chubb.Claims.UnitTests` | Backend tests |
| `docker-compose.yml` | Local Redpanda broker |

Run backend tests from `chubb-api` with:

```bash
dotnet test Chubb.Claims.sln
```

The role switcher uses demo user headers rather than production authentication. FX conversion uses fixed sample rates. This application publishes Kafka events, but it does not contain a Kafka consumer; the analytics summary reads claims from SQLite.
