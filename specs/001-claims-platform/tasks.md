# Task Breakdown (`001-claims-platform`)

## Phase 1: SpecKit & Repository Foundation
- [x] Initialize Git repository and `.specify/memory/constitution.md`
- [x] Create `specs/001-claims-platform/spec.md`, `plan.md`, and `tasks.md`
- [x] Initialize `AI_WORKING_JOURNAL.md` and commit SpecKit artifacts (`Commit 1`)

## Phase 2: Backend — Domain & Application Layers (.NET 10 Clean Architecture)
- [x] Scaffold `.NET 10` solution (`Chubb.Claims.sln`) with `Domain`, `Application`, `Infrastructure`, `Api`, and `UnitTests`
- [x] Implement `Chubb.Claims.Domain` (`Claim`, child entities, state machine, reserve exposure formulas, domain events)
- [x] Implement `Chubb.Claims.Application` (`IDispatcher`, 7 Command Handlers, 3 Query Handlers, DTOs, role-based note masking)
- [x] Commit Domain & Application layers (`Commit 2`)

## Phase 3: Backend — Infrastructure, Outbox Worker, API & Unit Tests
- [x] Implement `Chubb.Claims.Infrastructure` (`ClaimsDbContext`, Outbox interceptor, `ApacFxRateService`, Hybrid Kafka/In-Memory event bus, `ClaimsDbSeeder`)
- [x] Implement `Chubb.Claims.Api` (Minimal API endpoints, SignalR `ClaimsHub`, `OutboxRelayWorker`)
- [x] Implement and run `Chubb.Claims.UnitTests`
- [x] Commit Infrastructure, API & Unit Tests (`Commit 3`)

## Phase 4: Frontend — Angular Dual-Persona Application
- [x] Create Angular application with Claimant Portal, Claims Officer Workbench, Manager Liability Exposure Dashboard, and Live Persona Switcher
- [x] Build & verify end-to-end (`Commit 4`)

## Phase 5: Docker Compose, Walkthrough Guide & Final Sync
- [x] Add `docker-compose.yml`, finalize `AI_WORKING_JOURNAL.md` and `README.md` (`Commit 5`)
