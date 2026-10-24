# Chubb APAC Claims Platform — Constitution

## 1. Architectural Principles
1. **Clean Architecture & Explicit CQRS (`IDispatcher`)**:
   - **Domain Layer (`Chubb.Claims.Domain`)**: Claim entities, enums, calculated amounts, and domain event types. Command handlers enforce status transitions.
   - **Application Layer (`Chubb.Claims.Application`)**: Custom lightweight `IDispatcher` (`ICommand<TResponse>`, `ICommandHandler<TCommand, TResponse>`, `IQuery<TResponse>`, `IQueryHandler<TQuery, TResponse>`). Handlers are named explicitly (e.g., `SubmitClaimHandler`, `PickUpClaimHandler`, `GetExposureAnalyticsHandler`).
   - **Infrastructure Layer (`Chubb.Claims.Infrastructure`)**: EF Core SQLite persistence, outbox extraction inside `SaveChangesAsync`, Kafka producer, fixed FX rates, and sample APAC claims.
   - **Worker & API Layer (`Chubb.Claims.Api`)**: .NET 10 controllers, OpenAPI, SignalR `ClaimsHub`, and an in-process `OutboxRelayWorker`.

2. **REST vs. Kafka Separation of Concerns**:
   - **REST/HTTP**: Used for synchronous command execution (Claim submission, claiming/assigning a ticket, requesting info, settling/rejecting) where the caller needs immediate validation feedback, and for low-latency view queries.
   - **Kafka / Event-Driven**: The API publishes claim events to Kafka when a broker is available. SignalR notifies connected Angular clients to refresh their REST data. Analytics reads SQLite; there is no Kafka consumer in this POC.

3. **Transactional Outbox Guarantee**:
   - Handlers add domain events, and `SaveChangesAsync` saves their `OutboxMessages` with claim changes. If Kafka is unavailable, the worker still sends SignalR notifications and marks those rows processed; it does not replay them to Kafka later.

4. **Single Angular Codebase for Dual Personas**:
   - Standalone Angular components using Angular Signals for reactive state.
   - The top-bar role switcher (`Claimant` and `Claims Officer`) sets demo headers and swaps the two views in one Angular component. These headers are not production authentication.
