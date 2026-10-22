# Chubb APAC Claims Platform — Constitution

## 1. Architectural Principles
1. **Clean Architecture & Explicit CQRS (`IDispatcher`)**:
   - **Domain Layer (`Chubb.Claims.Domain`)**: Pure domain entities (`Claim`, `ClaimInformationRequest`, `ClaimAuditEntry`, `ClaimNote`), value objects, enums, and domain events. Zero external framework dependencies. Rich domain methods enforce state machine invariants.
   - **Application Layer (`Chubb.Claims.Application`)**: Custom lightweight `IDispatcher` (`ICommand<TResponse>`, `ICommandHandler<TCommand, TResponse>`, `IQuery<TResponse>`, `IQueryHandler<TQuery, TResponse>`). Handlers are named explicitly (e.g., `SubmitClaimHandler`, `PickUpClaimHandler`, `GetExposureAnalyticsHandler`).
   - **Infrastructure Layer (`Chubb.Claims.Infrastructure`)**: EF Core SQLite persistence, Transactional Outbox saving inside `SaveChangesAsync`, hybrid Kafka/In-Memory event producer, FX conversion service, and realistic APAC data seeder.
   - **Worker & API Layer (`Chubb.Claims.Api`)**: .NET 10 API endpoints, OpenAPI specification, SignalR `ClaimsHub`, `OutboxRelayWorker`, global problem-details exception handling, and persona simulation middleware.

2. **REST vs. Kafka Separation of Concerns**:
   - **REST/HTTP**: Used for synchronous command execution (Claim submission, claiming/assigning a ticket, requesting info, settling/rejecting) where the caller needs immediate validation feedback, and for low-latency view queries.
   - **Kafka / Event-Driven**: Used for asynchronous downstream consequences of state transitions:
     1. Real-time recalculation of **APAC Liability Exposure Projections** across 6 markets.
     2. Immutable **Compliance & Audit Log Streaming**.
     3. Real-time **SignalR UI Push** to notify Claimants and Claims Managers when a claim status or reserve changes.

3. **Transactional Outbox Guarantee**:
   - No dual-write bugs. Domain events raised by the `Claim` aggregate are serialized into `OutboxMessages` within the exact same database transaction as the `Claim` state change.

4. **Single Angular Codebase for Dual Personas**:
   - Standalone Angular components using Angular Signals for reactive state.
   - Persona Switcher in top navigation (`Claimant` vs. `Claims Officer` vs. `Claims Manager`) sets `X-Demo-User-Id` and `X-Demo-Role` headers and switches views seamlessly (`/portal` vs. `/workbench`).
