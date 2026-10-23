# CHUBB APAC — Claims & Liability Exposure Platform

> **Fullstack Developer Take-Home Assessment**
> **Stack**: Angular 19 (Standalone + Signals + SignalR) | .NET 10 Clean Architecture (`IDispatcher` CQRS) | EF Core SQLite + Transactional Outbox | Apache Kafka / Redpanda Hybrid Event Stream

---

## 1. Run Locally for the First Time

Keep `chubb-api` and `chubb-frontend` as sibling folders. Install the .NET 10 SDK, Node.js/npm, Docker Desktop, and VS Code with C# Dev Kit. Docker runs Redpanda, a Kafka-compatible broker; the API and Angular app run on your Mac.

### 1. Start Kafka

Open a terminal in `chubb-api` (the folder containing `docker-compose.yml`):

```bash
docker compose up -d
docker compose ps
docker exec chubb-apac-kafka rpk topic create chubb.apac.claims.events
```

Wait until `docker compose ps` shows `healthy`. Create the topic only once; an “already exists” response on later runs is fine. The API connects to Kafka at `localhost:19092`, the broker's advertised address for programs running on your Mac.

### 2. Start the API with VS Code Run and Debug

Open `chubb-api.code-workspace` in VS Code. If an earlier API debug session is running, stop it first. In **Run and Debug**, select the C# launch configuration for `Chubb.Claims.Api` and press **F5**. The project's `src/Chubb.Claims.Api/Properties/launchSettings.json` supplies `Kafka__BootstrapServers=localhost:19092` to this debug session. You do not need to type a terminal environment variable when using F5.

Check `http://localhost:5150/swagger`. The API creates and seeds its SQLite database on first startup. If you use a different VS Code `launch.json` configuration that ignores `launchSettings.json`, set `Kafka__BootstrapServers` to `localhost:19092` in that configuration's `env` section and restart the debugger.

### 3. Start Angular

In a second terminal, from the sibling `chubb-frontend` folder:

```bash
npm ci
npm start
```

Open `http://localhost:4200`.

### 4. Confirm an event reaches Kafka

In another terminal, run:

```bash
docker exec -it chubb-apac-kafka rpk topic consume chubb.apac.claims.events
```

Leave the consumer open, then submit or update a claim in Angular. A new event should appear. The API first saves the claim and an outbox row in SQLite; `OutboxRelayWorker` publishes the row to Kafka. Previously processed outbox rows are not replayed when Kafka starts. If the consumer shows nothing, confirm that the API debug session was restarted after the Kafka launch setting changed and that you performed a new claim action. `GET /api/analytics/summary` also shows each recent event's `transportMode`; successful publishing is labelled `Kafka (...)`.

### Later runs and optional checks

On later runs, start Docker with `docker compose up -d`, press **F5** for the API, then run `npm start` in `chubb-frontend`. SQLite data persists between runs. To stop the broker while keeping its container and topic, run `docker compose stop` from `chubb-api`. `docker compose down` removes the container; because this Compose file has no data volume, create the topic again after the next `up`. Neither command deletes the API's SQLite database.

For a terminal API run instead of VS Code, run `dotnet run` from `chubb-api/src/Chubb.Claims.Api`. The same launch profile supplies the Kafka setting. Run backend tests from `chubb-api` with `dotnet test Chubb.Claims.sln`.

The API can run without Docker. If Kafka is unavailable, the current producer falls back to a local event stream and SignalR. In this POC the worker still marks the outbox row processed after that fallback, so those events will not later be sent to Kafka automatically.

---

## 2. Architectural Decisions & Walkthrough Defense Guide

### Q1: What are the core entities in this domain and how do they relate?
- **`Claim` (Aggregate Root)**: Encapsulates the end-to-end lifecycle of a Motor or Property claim in one of Chubb's 6 APAC markets (`SG`, `HK`, `AU`, `MY`, `TH`, `ID`). Enforces state transition invariants (`Submitted` -> `UnderReview` <-> `InfoRequested` -> `Settled` | `Rejected`).
- **`ClaimInformationRequest`**: Structured question/verification request raised by a Claims Officer when placing a claim into `InfoRequested` status, and resolved when the Claimant submits their response/document reference.
- **`ClaimNote`**: Internal adjuster assessment notes attached to a claim (`IsInternalOnly = true`). Automatically stripped by the Application Query Handler (`GetClaimByIdHandler`) when queried by a `Claimant` persona.
- **`ClaimAuditEntry`**: Immutable chronological audit record created on every state change or reserve adjustment.
- **`OutboxMessage`**: Event record persisted in the same SQLite database save as the claim change. The background worker publishes it after the save. Kafka delivery is not guaranteed when the current local fallback marks a message processed.

### Q2: Is the backend one service or more than one? What drives that decision?
- **Decision**: A **Clean Architecture Modular Monolith** (`Domain`, `Application`, `Infrastructure`, `Api` + Background `OutboxRelayWorker`) inside a single `.NET 10` solution.
- **Why**:
  1. Within a 2–3 hour sprint, deploying 3 separate microservices introduces operational boilerplate without adding domain value.
  2. By strictly decoupling the **Application Layer** via a custom zero-dependency **`IDispatcher`** (`SubmitClaimHandler`, `PickUpClaimHandler`, `UpdateClaimAssessmentHandler`, `GetExposureAnalyticsHandler`, etc.) and decoupling write-side commands from downstream projections via the **Transactional Outbox (`OutboxMessages`)**, the Analytics/Exposure module or Notification worker can be extracted into separate deployable microservices in < 1 hour when team scale or independent scaling demands it.

### Q3: What operations are synchronous (REST/HTTP) vs. event-driven (Kafka)?
| Operation | Protocol | Why? |
|---|---|---|
| **Submit Claim (FNOL), Pick Up Claim, Update Reserve, Request/Respond Info, Settle/Reject** | **REST/HTTP (`POST`/`PUT`)** | The user (Claimant or Claims Officer) is waiting for immediate domain validation (e.g., state machine rules, positive reserve checks) and confirmation that their action succeeded. |
| **Query Claims Queue, Claim Details, Exposure Dashboard** | **REST/HTTP (`GET`)** | Request/response read queries with filter parameters. |
| **Claim change events for future backend consumers** | **Kafka (`chubb.apac.claims.events`)** | The outbox worker publishes state changes after the claim is saved. This POC produces events but has no Kafka consumer yet. |
| **Live browser refresh** | **SignalR** | Connected Angular clients refresh after a claim event. The analytics summary currently reads SQLite directly. |

### Q4: How does the system calculate Outstanding Liability Exposure across 6 APAC markets?
Insurance claims across **SG (`SGD`), HK (`HKD`), AU (`AUD`), MY (`MYR`), TH (`THB`), and ID (`IDR`)** cannot be naively summed in local currency, nor is a claim's financial exposure static:
1. **Dynamic Reserve Lifecycle**:
   - At FNOL submission, exposure starts at the Claimant's **`EstimatedLoss`**.
   - During assessment, the Claims Officer sets/updates **`AssessedReserve`** (e.g., upward after hidden workshop damage is found, or downward if repairable).
   - While open (`Submitted`, `UnderReview`, `InfoRequested`), $\text{ActiveReserveExposure} = \text{AssessedReserve} ?? \text{EstimatedLoss}$.
   - When **`Settled`**, $\text{ActiveReserveExposure}$ drops to $0$ and moves to **Realized Settled Payouts** ($\text{SettledAmount}$).
   - When **`Rejected`**, $\text{ActiveReserveExposure}$ drops to $0$ and moves to **Released Reserves**.
2. **Multi-Currency Normalization**:
   - Every claim retains its native currency (`SGD`, `HKD`, `AUD`, `MYR`, `THB`, `IDR`) for local settlement accuracy, while `IFxRateService` normalizes exposure into **USD** so the Claims Manager Dashboard presents both per-market native reserves and consolidated APAC USD exposure.

### Q5: How does a single Angular codebase serve two distinct user types?
- **Role-Aware Context & Persona Switcher**: The top navigation bar includes a **Live Persona Switcher** allowing the interview panel to toggle instantaneously between:
  - **Claimants** (`Lim Hean Soon - SG`, `Chloe Chan - HK`, `Liam O'Connor - AU`): Sees `/portal` mode — their personal claims, visual 4-stage progress stepper, FNOL submission modal, and a highlighted **"Action Required: Respond to Information Request"** card when an officer requests documents.
  - **Claims Officers** (`Admin Officer - SG/HK`, `Rajesh Patel - AU/MY`, `Narong Srisai - TH/ID`): Sees the **Triage & Assessment Workbench** — multi-market filters, 1-click "Pick Up & Assign to Me", reserve adjustment, confidential adjuster notes, info requests, and Settlement/Rejection authority.
  - **Claims Manager** (`Victoria Lim - VP APAC Claims`): Sees the **Executive Liability Exposure & Team Workload Dashboard** — consolidated USD active exposure, 6-market native + USD table, per-officer workload & cycle time, and the live Outbox/Kafka event ticker.

---

## 3. What Would We Do With More Time? (10-Minute Panel Segment)
1. **OIDC / Entra ID Authentication & Policy-Based Authorization**: Replace the demo `X-Demo-Role` header simulation with JWT Bearer tokens (Auth0 / Azure Entra ID), enforcing `[Authorize(Policy = "ClaimsOfficerOnly")]` and row-level security on Claimant queries, plus maker-checker approval limits (e.g., settlements > \$50,000 USD require Manager co-sign).
2. **Dedicated Read-Model Projection Table for Analytics**: Currently `GetExposureAnalyticsHandler` aggregates over the normalized SQLite tables; at millions of claims, `EventProjectionWorker` would consume the Kafka topic `chubb.apac.claims.events` and maintain a pre-aggregated materialized table (`MarketExposureSnapshots`) in PostgreSQL/Redis for sub-5ms dashboard loads.
3. **Document Blob Storage & Presigned URLs**: Replace attachment filename references with direct S3/Azure Blob Storage presigned uploads with virus scanning and OCR extraction for workshop invoices.
4. **Live FX Rate Feed & Historical FX Locking**: Lock the FX rate at the date of loss/reserve booking and subscribe to an external treasury FX feed.
