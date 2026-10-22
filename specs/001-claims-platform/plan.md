# Implementation Plan: Chubb APAC Claims Platform (`001-claims-platform`)

## 1. Solution Structure (`.NET 10` Clean Architecture + Angular)

```
chubb-claims-platform/
├── .specify/
│   └── memory/constitution.md
├── specs/
│   └── 001-claims-platform/
│       ├── spec.md
│       ├── plan.md
│       └── tasks.md
├── backend/
│   ├── Chubb.Claims.sln
│   ├── src/
│   │   ├── Chubb.Claims.Domain/          # Entities, Value Objects, Enums, Domain Events, Exceptions
│   │   ├── Chubb.Claims.Application/     # IDispatcher, Commands/Handlers, Queries/Handlers, DTOs, Interfaces
│   │   ├── Chubb.Claims.Infrastructure/  # EF Core SQLite, Outbox, Hybrid Kafka/Channel Bus, FX Service, Seeder
│   │   └── Chubb.Claims.Api/             # .NET 10 API Endpoints, SignalR Hub, Background Outbox/Kafka Workers
│   └── tests/
│       └── Chubb.Claims.UnitTests/       # Domain State Machine, Reserve Exposure & Handler Unit Tests
├── frontend/                             # Angular Standalone Application (Claimant Portal + Staff Workbench)
├── docker-compose.yml                    # Optional Redpanda (Kafka-compatible) + Backend + Frontend
├── AI_WORKING_JOURNAL.md                 # Required Deliverable: Running log of prompts, challenges, overrides
└── README.md                             # Walkthrough & Architecture defense guide
```

## 2. Clean Architecture & `IDispatcher` Design

### 2.1 Custom `IDispatcher` (`Chubb.Claims.Application/Common/Dispatcher`)
Instead of bloated third-party mediator libraries with licensing overhead, we implement a clean, zero-dependency, strongly-typed `IDispatcher` registered in Microsoft DI:
- `ICommand<TResponse>` and `ICommandHandler<TCommand, TResponse>`
- `IQuery<TResponse>` and `IQueryHandler<TQuery, TResponse>`
- `IDispatcher.SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct)`
- `IDispatcher.QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct)`

### 2.2 Application Layer Handlers
**Command Handlers (`Features/Claims/Commands/`)**:
1. `SubmitClaimHandler` (`SubmitClaimCommand` -> `ClaimDetailDto`)
2. `PickUpClaimHandler` (`PickUpClaimCommand` -> `ClaimDetailDto`)
3. `UpdateClaimAssessmentHandler` (`UpdateClaimAssessmentCommand` -> `ClaimDetailDto`)
4. `RequestClaimInfoHandler` (`RequestClaimInfoCommand` -> `ClaimDetailDto`)
5. `RespondToClaimInfoHandler` (`RespondToClaimInfoCommand` -> `ClaimDetailDto`)
6. `SettleClaimHandler` (`SettleClaimCommand` -> `ClaimDetailDto`)
7. `RejectClaimHandler` (`RejectClaimCommand` -> `ClaimDetailDto`)

**Query Handlers (`Features/Claims/Queries/`)**:
1. `GetClaimsListHandler` (`GetClaimsListQuery` -> `IReadOnlyList<ClaimSummaryDto>`)
2. `GetClaimByIdHandler` (`GetClaimByIdQuery` -> `ClaimDetailDto`)
3. `GetExposureAnalyticsHandler` (`GetExposureAnalyticsQuery` -> `ExposureAnalyticsDto`)
