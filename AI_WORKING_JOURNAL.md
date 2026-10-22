# AI Working Journal — Chubb APAC Claims Platform

> **Deliverable Requirement**: A running log (committed alongside the code) of what was asked of the AI, what was accepted, what was challenged, and what was overridden, with brief reasoning.

---

## Entry 1: Problem Decomposition & Architecture Alignment (`speckit.specify` & `speckit.grill-me`)

### What I Asked the AI
I asked the AI to help decompose the Chubb APAC Fullstack Take-Home Assessment following a Spec-Driven Development lifecycle (`speckit.specify` -> `speckit.grill-me` -> `speckit.implement`) using **Angular** on the frontend and **.NET 10** on the backend.

### What the AI Proposed
1. **Service Boundaries**: Proposed a Modular Monolith vs. Microservices trade-off, recommending a single deployable solution with strict internal module separation for a 2–3 hour sprint.
2. **Event-Driven Communication (REST vs. Kafka)**: Proposed synchronous REST for user commands/queries (where immediate validation and state confirmation are needed) and asynchronous Kafka events for liability exposure recalculation, audit logging, and real-time UI notifications.
3. **Database**: Proposed SQLite with EF Core auto-seeding across all 6 APAC markets (`SG`, `HK`, `AU`, `MY`, `TH`, `ID`).
4. **Dual-Persona Angular App**: Proposed a single Angular codebase with a top-bar Persona Switcher (`Claimant` vs. `Claims Officer` vs. `Claims Manager`) passing persona headers (`X-Demo-Role`, `X-Demo-User-Id`).

### What I Challenged & Overrode
- **Override 1 (Backend Architecture & Mediator Pattern)**: Instead of a generic service-layer monolith or adding a third-party commercial library like MediatR (which moved to a commercial license in recent versions), I directed the AI to structure the backend using **Clean Architecture** (`Domain`, `Application`, `Infrastructure`, `Api`/Workers) with a custom, lightweight **`IDispatcher`** interface and explicit Application-layer Handlers (`SubmitClaimHandler`, `GetExposureAnalyticsHandler`, etc.).
  - *Reasoning*: Keeps the Application layer 100% free of external package bloat, enforces Single Responsibility Principle (one file/class per business use case), and makes unit testing handlers straightforward.
- **Challenge 2 (Multi-Currency Liability Exposure Across 6 APAC Markets)**: The brief highlights that Chubb APAC operates across 6 markets (`SG`, `HK`, `AU`, `MY`, `TH`, `ID`) and asks *"What does the system need to know about outstanding liability exposure?"*. A naive implementation stores a single static claim amount in a single currency.
  - *Decision Accepted*: We designed a multi-currency model where claims are recorded in their market's native currency (`SGD`, `HKD`, `AUD`, `MYR`, `THB`, `IDR`) and normalized to **USD** (base reporting currency) via an `IFxRateService`. Furthermore, we modeled a **Dynamic Reserve Lifecycle**:
    - `EstimatedLoss` (Claimant initial report) -> `AssessedReserve` (Officer adjustment during review) -> `SettledAmount` (Final payout).
    - Active liability exposure equals `AssessedReserve ?? EstimatedLoss` while open, and automatically drops to `0` when `Settled` (moving to Realized Payout) or `Rejected` (moving to Released Reserve).
- **Challenge 3 (Dual-Write Risk between Database and Kafka)**: Publishing directly to Kafka inside a command handler risks dual-write inconsistency if the DB commit succeeds but Kafka fails (or vice versa).
  - *Decision Accepted*: Implemented the **Transactional Outbox Pattern** (`OutboxMessages` persisted in the same EF Core transaction as `Claim` state changes) with a background `OutboxRelayWorker` that publishes to Kafka (with an automatic in-memory `Channel` + SignalR fallback so `dotnet run` works out-of-the-box without requiring Docker to be up).

---

## Entry 2: Domain Aggregate & State Machine Invariants (`Chubb.Claims.Domain`)

### What I Asked the AI
Generate the `Claim` aggregate root and child entities (`ClaimInformationRequest`, `ClaimNote`, `ClaimAuditEntry`, `OutboxMessage`) with rich domain behavior.

### What the AI Proposed
Initial draft allowed `Settle` and `Reject` from any non-terminal state, and exposed public setters on child collections.

### What I Challenged & Overrode
- **Challenge (Strict State Transitions)**: In insurance operations, an unreviewed FNOL (`Submitted`) or a claim awaiting claimant documentation (`InfoRequested`) should not be settled directly without being in `UnderReview`.
  - *Override*: Enforced strict guard clauses in `Claim.Settle()` and `Claim.Reject()` requiring `Status == ClaimStatus.UnderReview`, throwing a `DomainException` (mapped to HTTP 400 ProblemDetails) if violated.
- **Challenge (Data Privacy Between Personas)**: Since both Claimants and Claims Staff use the same Angular application and query `GET /api/claims/{id}`, internal adjuster notes (`ClaimNote.IsInternalOnly = true`) must never leak to a Claimant.
  - *Accepted & Verified*: Added role-aware projection in `GetClaimByIdHandler` and `ClaimMapper.ToDetailDto(includeInternalNotes: query.IsStaffRole)`, and wrote an automated xUnit test (`Dispatcher_EndToEndWithSqliteOutbox_PersistsOutboxEventsAndMasksInternalNotesForClaimant`) to prove internal notes are stripped when `IsStaffRole == false`.

---

## Entry 3: EF Core Backing Fields & Transactional Outbox (`Chubb.Claims.Infrastructure`)

### What I Asked the AI
Implement `ClaimsDbContext` with SQLite and automatic extraction of `IDomainEvent` items into `OutboxMessages` inside `SaveChangesAsync`.

### What I Challenged & Fixed
- **Challenge (EF Core Backing Field Change Tracking)**: When domain methods like `claim.RequestAdditionalInfo()` or `claim.UpdateAssessment()` append new entities to private `readonly List<T>` backing fields (`_informationRequests`, `_notes`, `_auditTrail`) on an already-tracked `Claim` aggregate, EF Core can sometimes misclassify new child entities with client-generated `Guid` keys as `Modified` instead of `Added`, causing `DbUpdateConcurrencyException`.
  - *Fix Applied*: Explicitly inspected `ChangeTracker.Entries<Claim>()` inside `ClaimsDbContext.SaveChangesAsync` before base save to ensure newly appended child items not yet in the database are marked `EntityState.Added`.

---

## Entry 4: Single Angular Codebase Serving Dual Personas (`frontend/`)

### What I Asked the AI
Build the Angular 19 standalone application serving Claimants, Claims Officers, and Claims Managers from a single codebase with real-time SignalR updates.

### What I Challenged & Refined
- **Challenge (Demonstrating Both Personas Fluidly in a 15-Minute Demo)**: Logging out and logging back in repeatedly during a live panel walkthrough wastes time and breaks flow.
  - *Refinement*: Implemented a top-bar **Live Persona Switcher** pre-loaded with 7 realistic APAC personas (`Tan Wei Ming - SG Claimant`, `Chloe Chan - HK Claimant`, `Liam O'Connor - AU Claimant`, `Sarah Jenkins - SG/HK Officer`, `Rajesh Patel - AU/MY Officer`, `Narong Srisai - TH/ID Officer`, and `Victoria Lim - VP APAC Claims Manager`). Switching personas immediately updates the HTTP headers (`X-Demo-User-Id`, `X-Demo-Role`) and adapts the UI between the **Claimant Self-Service Portal**, **Claims Officer Triage & Assessment Workbench**, and **Executive APAC Liability Exposure Dashboard**.
