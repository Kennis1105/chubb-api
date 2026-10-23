# CHUBB APAC — Claims & Liability Exposure Platform

> **Fullstack Developer Take-Home Assessment**
> **Stack**: Angular 19 (Standalone + Signals + SignalR) | .NET 10 Clean Architecture (`IDispatcher` CQRS) | EF Core SQLite + Transactional Outbox | Apache Kafka / Redpanda Hybrid Event Stream

---

## 1. Quick Start (Run Locally in < 60 Seconds)

### Option A: Zero-Docker Instant Start (Recommended for Fast Review)
The backend uses SQLite (auto-created and seeded on startup with realistic claims across all 6 APAC markets) and a **Hybrid Kafka/In-Memory Event Producer** that automatically falls back to an in-memory channel + SignalR when a Kafka broker is not configured.

**1. Start the .NET 10 Backend API (Port `5150`)**:
```bash
cd backend
dotnet run --project src/Chubb.Claims.Api/Chubb.Claims.Api.csproj --urls "http://localhost:5150"
```
- **Swagger UI**: Open **`http://localhost:5150/swagger`**
- **OpenAPI v1 JSON**: **`http://localhost:5150/openapi/v1.json`**

**2. Start the Angular Frontend (Port `4200`)**:
```bash
cd frontend
npm start
```
Open **`http://localhost:4200`** in your browser.

**3. Run Unit & Integration Tests**:
```bash
cd backend
dotnet test
```

### Option B: With Real Kafka Broker (Redpanda via Docker Compose)
If you want to see events published to a live Kafka broker (`localhost:19092`, topic `chubb.apac.claims.events`):
```bash
docker compose up -d
cd backend
Kafka__BootstrapServers="localhost:19092" dotnet run --project src/Chubb.Claims.Api/Chubb.Claims.Api.csproj --urls "http://localhost:5150"
```

---

## 2. Architectural Decisions & Walkthrough Defense Guide

### Q1: What are the core entities in this domain and how do they relate?
- **`Claim` (Aggregate Root)**: Encapsulates the end-to-end lifecycle of a Motor or Property claim in one of Chubb's 6 APAC markets (`SG`, `HK`, `AU`, `MY`, `TH`, `ID`). Enforces state transition invariants (`Submitted` -> `UnderReview` <-> `InfoRequested` -> `Settled` | `Rejected`).
- **`ClaimInformationRequest`**: Structured question/verification request raised by a Claims Officer when placing a claim into `InfoRequested` status, and resolved when the Claimant submits their response/document reference.
- **`ClaimNote`**: Internal adjuster assessment notes attached to a claim (`IsInternalOnly = true`). Automatically stripped by the Application Query Handler (`GetClaimByIdHandler`) when queried by a `Claimant` persona.
- **`ClaimAuditEntry`**: Immutable chronological audit record created on every state change or reserve adjustment.
- **`OutboxMessage`**: Transactional Outbox entity persisted in the exact same SQLite database transaction as `Claim` mutations to guarantee at-least-once event delivery to Kafka without dual-write bugs.

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
| **Liability Exposure Recalculation, Audit Streaming, Cross-Tab Live Alerts** | **Kafka (`chubb.apac.claims.events`) + SignalR Push** | Downstream side effects of state transitions (`ClaimSubmittedDomainEvent`, `ClaimReserveUpdatedDomainEvent`, `ClaimSettledDomainEvent`, `ClaimRejectedDomainEvent`). Decoupled via the **Transactional Outbox Pattern** (`OutboxRelayWorker`). |

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
