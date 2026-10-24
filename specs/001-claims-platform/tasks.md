# Task Breakdown (`001-claims-platform`)

## Phase 1: SpecKit & Repository Foundation
- [x] Initialize Git repository and `.specify/memory/constitution.md`
- [x] Create `specs/001-claims-platform/spec.md`, `plan.md`, and `tasks.md`
- [x] Initialize `AI_WORKING_JOURNAL.md` and commit planning artifacts

## Phase 2: Backend — Domain & Application Layers (.NET 10 Clean Architecture)
- [x] Scaffold `.NET 10` solution (`Chubb.Claims.sln`) with `Domain`, `Application`, `Infrastructure`, `Api`, and `UnitTests`
- [x] Implement `Chubb.Claims.Domain` (`Claim`, child entities, reserve exposure formulas, domain events); handlers enforce status transitions
- [x] Implement `Chubb.Claims.Application` (`IDispatcher`, 7 claim command handlers, 4 query handlers, response contracts, role-based note masking)
- [x] Commit Domain & Application work

## Phase 3: Backend — Infrastructure, Outbox Worker, API & Unit Tests
- [x] Implement `Chubb.Claims.Infrastructure` (`ClaimsDbContext`, outbox extraction in `SaveChangesAsync`, `ApacFxRateService`, Kafka producer, `ClaimsDbSeeder`)
- [x] Implement `Chubb.Claims.Api` (controllers, SignalR `ClaimsHub`, `OutboxRelayWorker`)
- [x] Implement and run `Chubb.Claims.UnitTests`
- [x] Commit Infrastructure, API & Unit Tests

## Phase 4: Frontend — Angular Dual-Persona Application
- [x] Create Angular application with claimant and officer views, an officer liability summary, and a two-role switcher
- [x] Build the API and frontend

## Phase 5: Local Setup
- [x] Add `docker-compose.yml` for Redpanda and document local setup in `README.md`
