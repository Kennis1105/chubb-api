# AI Working Journal — Chubb APAC Claims Platform

> **Deliverable Requirement**: A running log (committed alongside the code) of what was asked of the AI, what was accepted, what was challenged, and what was overridden, with brief reasoning.

---

## Entry 1: Problem Decomposition & Architecture Alignment (`speckit.specify` & `speckit.grill-me`)

### What I Asked the AI
I asked the AI to help decompose the Chubb APAC Fullstack Take-Home Assessment following a Spec-Driven Development lifecycle (`speckit.specify` -> `speckit.grill-me` -> `speckit.implement`) using **Angular** on the frontend and **.NET 10** on the backend.

### What the AI Proposed
1. **Service Boundaries**: Proposed a Modular Monolith vs. Microservices trade-off, recommending a single deployable solution with strict internal module separation for a 2–3 hour sprint.
2. **Event-Driven Communication (REST vs. Kafka)**: Proposed synchronous REST for user commands and queries, with Kafka publishing claim events and SignalR notifying the UI. Analytics reads SQLite directly.
3. **Database**: Proposed SQLite with six supported APAC market currencies (`SG`, `HK`, `AU`, `MY`, `TH`, `ID`). Four sample claims are seeded across SG, HK, and AU.
4. **Dual-Persona Angular App**: Proposed a single Angular codebase with a top-bar role switcher for `Claimant` and `Claims Officer`, passing demo headers (`X-Demo-Role`, `X-Demo-User-Id`).

### What I Challenged & Overrode
- **Override 1 (Backend Architecture & Dispatcher)**: I chose separate `Domain`, `Application`, `Infrastructure`, and `Api` projects, with a lightweight `IDispatcher` and explicit handlers (`SubmitClaimHandler`, `GetExposureAnalyticsHandler`, etc.). The outbox worker runs in the API process.
  - *Reasoning*: Each use case is easy to locate and test without a separate worker deployment for this POC.
- **Challenge 2 (Multi-Currency Liability Exposure Across 6 APAC Markets)**: The brief highlights that Chubb APAC operates across 6 markets (`SG`, `HK`, `AU`, `MY`, `TH`, `ID`) and asks *"What does the system need to know about outstanding liability exposure?"*. A naive implementation stores a single static claim amount in a single currency.
  - *Decision Accepted*: We designed a multi-currency model where claims are recorded in their market's native currency (`SGD`, `HKD`, `AUD`, `MYR`, `THB`, `IDR`) and normalized to **USD** (base reporting currency) via an `IFxRateService`. Furthermore, we modeled a **Dynamic Reserve Lifecycle**:
    - `EstimatedLoss` (Claimant initial report) -> `AssessedReserve` (Officer adjustment during review) -> `SettledAmount` (Final payout).
    - Active liability exposure equals `AssessedReserve ?? EstimatedLoss` while open, and automatically drops to `0` when `Settled` (moving to Realized Payout) or `Rejected` (moving to Released Reserve).
- **Challenge 3 (Dual-Write Risk between Database and Kafka)**: Publishing directly to Kafka inside a command handler risks dual-write inconsistency if the DB commit succeeds but Kafka fails (or vice versa).
  - *Decision Accepted*: Implemented the **Transactional Outbox Pattern** (`OutboxMessages` persisted with `Claim` state changes) with a background `OutboxRelayWorker` that publishes to Kafka when available and sends SignalR notifications. Without Kafka, the worker still notifies the UI but marks the message processed, so it will not replay to Kafka later.

---

## Entry 2: Domain Aggregate & State Machine Invariants (`Chubb.Claims.Domain`)

### What I Asked the AI
Generate the `Claim` entity and related information requests, notes, audit entries, and outbox messages.

### What the AI Proposed
The first draft needed clearer rules for when officers can settle or reject a claim.

### What I Challenged & Overrode
- **Challenge (Strict State Transitions)**: In insurance operations, an unreviewed FNOL (`Submitted`) or a claim awaiting claimant documentation (`InfoRequested`) should not be settled directly without being in `UnderReview`.
  - *Override*: Added guard clauses in `SettleClaimHandler` and `RejectClaimHandler` requiring `Status == ClaimStatus.UnderReview`. The `Claim` entity itself has public properties rather than transition methods.
- **Challenge (Data Privacy Between Personas)**: Since both Claimants and Claims Staff use the same Angular application and query `GET /api/claims/{id}`, internal adjuster notes (`ClaimNote.IsInternalOnly = true`) must never leak to a Claimant.
  - *Accepted & Verified*: Added role-aware projection in `GetClaimByIdHandler` and `ClaimMapper.ToDetailResponse(includeInternalNotes: query.IsStaffRole)`, with a test that checks internal notes are hidden when `IsStaffRole == false`. The role header is demo-only and is not production authorization.

---

## Entry 3: EF Core Backing Fields & Transactional Outbox (`Chubb.Claims.Infrastructure`)

### What I Asked the AI
Implement `ClaimsDbContext` with SQLite and automatic extraction of `IDomainEvent` items into `OutboxMessages` inside `SaveChangesAsync`.

### What I Challenged & Fixed
- **Challenge (EF Core Change Tracking)**: New information requests, notes, and audit entries are appended to public lists on an already-tracked claim. EF Core may treat a new child with a client-generated `Guid` as `Modified` instead of `Added`.
  - *Fix Applied*: Inspected tracked claims in `ClaimsDbContext.SaveChangesAsync` and marked new child entities as `EntityState.Added` before saving.

---

## Entry 4: Single Angular Codebase Serving Dual Personas

### What I Asked the AI
Build the Angular 19 application for a claimant and a claims officer, with SignalR updates.

### What I Challenged & Refined
- **Challenge (Demonstrating Both Personas Fluidly in a 15-Minute Demo)**: Logging out and logging back in repeatedly during a live panel walkthrough wastes time and breaks flow.
  - *Refinement*: Implemented a top-bar switcher for the two demo personas, Lim Hean Soon and Admin Officer. It changes demo request headers and switches between the claimant and officer views. The officer view shows workload and pending liability; there is no manager dashboard.

---

## Entry 5: Demo Persona Names

### What I Asked the AI
Change the demo claimant's name to Lim Hean Soon and the claims officer's name to Admin Officer.

### What I Accepted
Updated the current persona metadata, seeded and existing local claim records, handler defaults, UI fallbacks, and tests. Kept the existing persona IDs so claims remain associated with the same demo users. Verified with four passing .NET tests and a successful Angular production build.

---

## Entry 6: Claim Form Defaults

### What I Challenged
The new claim form opened with a complete sample incident already filled in, making it easy to submit a claim with example data by mistake.

### What I Changed
Removed the hardcoded form values, added selection prompts and required-field validation, and reset the form each time it opens. Also corrected the currency label to follow the selected market. Verified with an Angular production build.

---

## Entry 7: Analytics Endpoint Name

### What I Challenged
`/api/analytics/exposure` was too narrow a name for a response that also includes claim counts, market breakdowns, officer workload, and recent events.

### What I Changed
Renamed the route to `/api/analytics/summary` and updated the Angular client. Kept exposure as the name of the financial metric. Verified both the .NET and Angular builds.

---

## Entry 8: Local Kafka Setup for VS Code Debugging

### What I Asked the AI
Document first-time local setup for Docker/Redpanda, the API started with VS Code Run and Debug, and the Angular frontend.

### What I Accepted
Added the local broker address to the API's debug launch profile and replaced the README's outdated startup commands with the actual sibling-repo workflow, topic creation, and event verification steps. Clarified that Kafka has a producer but no consumer in this POC, and that the current fallback does not guarantee eventual Kafka delivery. Validated the Compose file and launch profile syntax.

---

## Entry 9: Officer Assessment UI

### What I Challenged
The API supported `UpdateClaimAssessmentHandler`, but the officer screen had no way to adjust a reserve or add an internal note.

### What I Changed
Added an Update Assessment section for claims under review or awaiting information. Officers can change the assessed reserve, add an internal note, and see saved notes. The screen refreshes the claim and exposure summary after saving. Verified with an Angular production build.
