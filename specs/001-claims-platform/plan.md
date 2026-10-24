# Implementation Plan: Chubb APAC Claims Platform (`001-claims-platform`)

## 1. Solution Structure (`.NET 10` Clean Architecture + Angular)

```
Chubb/
├── chubb-api/
│   ├── Chubb.Claims.sln
│   ├── src/
│   │   ├── Chubb.Claims.Domain/
│   │   ├── Chubb.Claims.Application/
│   │   ├── Chubb.Claims.Infrastructure/
│   │   └── Chubb.Claims.Api/             # Controllers, SignalR, in-process outbox worker
│   ├── tests/Chubb.Claims.UnitTests/
│   ├── specs/001-claims-platform/
│   ├── docker-compose.yml                # Optional Redpanda broker
│   ├── AI_WORKING_JOURNAL.md
│   └── README.md
└── chubb-frontend/                        # Angular claimant and officer views
```

## 2. Clean Architecture & `IDispatcher` Design

### 2.1 Custom `IDispatcher` (`Chubb.Claims.Application/Common/Dispatcher`)
Use a small `IDispatcher` registered in Microsoft DI:
- `ICommand<TResponse>` and `ICommandHandler<TCommand, TResponse>`
- `IQuery<TResponse>` and `IQueryHandler<TQuery, TResponse>`
- `IDispatcher.SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct)`
- `IDispatcher.QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct)`

### 2.2 Application Layer Handlers
**Command Handlers (`Features/Claims/Commands/`)**:
1. `SubmitClaimHandler` (`SubmitClaimRequest` -> `ClaimDetailResponse`)
2. `PickUpClaimHandler` (`PickUpClaimRequest` -> `ClaimDetailResponse`)
3. `UpdateClaimAssessmentHandler` (`UpdateAssessmentRequest` -> `ClaimDetailResponse`)
4. `RequestClaimInfoHandler` (`RequestInfoRequest` -> `ClaimDetailResponse`)
5. `RespondToClaimInfoHandler` (`RespondInfoRequest` -> `ClaimDetailResponse`)
6. `SettleClaimHandler` (`SettleClaimRequest` -> `ClaimDetailResponse`)
7. `RejectClaimHandler` (`RejectClaimRequest` -> `ClaimDetailResponse`)

**Query Handlers (`Features/Claims/Queries/`)**:
1. `GetClaimsListHandler` (`GetClaimsListRequest` -> claim summaries)
2. `GetClaimByIdHandler` (`GetClaimByIdRequest` -> `ClaimDetailResponse`)
3. `GetExposureAnalyticsHandler` (`GetExposureAnalyticsRequest` -> `ExposureAnalyticsResponse`)
