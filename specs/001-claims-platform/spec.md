# Feature Specification: Chubb APAC Motor & Property Claims Platform (`001-claims-platform`)

## 1. Problem Statement & Business Context
Chubb APAC processes motor and property insurance claims across six markets: **Singapore (SG), Hong Kong (HK), Australia (AU), Malaysia (MY), Thailand (TH), and Indonesia (ID)**.
Currently:
- **Claimants** submit claims via phone/email with zero visibility into status, officer requests, or decisions.
- **Claims Officers** work off fragmented shared inboxes and spreadsheets with no unified queue or structured assessment workflow.
- **Claims Managers** lack real-time visibility into team workload, SLA bottlenecks, and consolidated multi-currency **Outstanding Liability Exposure**.

## 2. Core Domain Model & State Machine

### 2.1 Markets & Currencies
| Market Code | Country | Native Currency | Indicative USD FX Rate (1 Unit -> USD) |
|---|---|---|---|
| `SG` | Singapore | `SGD` | `0.74` |
| `HK` | Hong Kong | `HKD` | `0.128` |
| `AU` | Australia | `AUD` | `0.66` |
| `MY` | Malaysia | `MYR` | `0.23` |
| `TH` | Thailand | `THB` | `0.029` |
| `ID` | Indonesia | `IDR` | `0.000064` |

### 2.2 Claim Lifecycle & State Machine
```
[Submitted] ──(Officer Picks Up)──> [UnderReview]
     ▲                                   │
     │                                   ├──(Request Info)──> [InfoRequested]
     │                                   │                          │
     │                                   ◄──(Claimant Responds)─────┘
     │                                   │
     │                                   ├──(Approve / Settle)──> [Settled] (Terminal)
     │                                   └──(Reject Claim)──────> [Rejected] (Terminal)
```

### 2.3 Dynamic Liability Exposure & Reserve Rules
Every claim tracks three financial figures in its native currency (and normalized USD equivalent):
1. **`EstimatedLoss`**: Initial loss amount reported by the Claimant at submission.
2. **`AssessedReserve`**: Adjusted reserve set by the Claims Officer during review/assessment. Defaults to `EstimatedLoss` until explicitly revised by an officer.
3. **`SettledAmount`**: Final payout approved when transitioning to `Settled`.

**Outstanding Liability Exposure Formula (Per Claim)**:
- When Status is `Submitted`, `UnderReview`, or `InfoRequested`:
  $$\text{ActiveReserveExposure} = \text{AssessedReserve} ?? \text{EstimatedLoss}$$
- When Status is `Settled`:
  $$\text{ActiveReserveExposure} = 0, \quad \text{RealizedPayout} = \text{SettledAmount}$$
- When Status is `Rejected`:
  $$\text{ActiveReserveExposure} = 0, \quad \text{ReleasedReserve} = \text{AssessedReserve} ?? \text{EstimatedLoss}$$

## 3. Functional Requirements

### FR-1: Claimant Self-Service Portal (`/portal`)
- **FR-1.1 Report Incident (FNOL)**: Submit a new Motor or Property claim specifying Policy Number, Market (`SG`, `HK`, `AU`, `MY`, `TH`, `ID`), Claim Type (`Motor` | `Property`), Incident Date, Incident Location, Description, Estimated Loss (in market currency), and Asset Identifier (Vehicle Plate or Property Address).
- **FR-1.2 Track Claim & Timeline**: View list of personal claims with real-time status stepper (`Submitted` -> `Under Review` -> `Action Required: Info Requested` -> `Settled` / `Rejected`) and full chronological audit history.
- **FR-1.3 Respond to Information Requests**: When a claim is in `InfoRequested` status, clearly highlight the officer's question and allow the claimant to submit their response/evidence, automatically transitioning the claim back to `UnderReview` and notifying the assigned officer.
- **FR-1.4 View Final Decision**: Display settlement payout breakdown or rejection rationale once decided.

### FR-2: Claims Officer Workbench (`/workbench`)
- **FR-2.1 Consolidated Triage & Queue**: Filterable queue across all 6 APAC markets, Claim Types (`Motor`, `Property`), and Statuses (`Unassigned / Submitted`, `My Active Queue`, `Awaiting Claimant Info`, `Closed`).
- **FR-2.2 Pick Up / Assign Claim**: One-click "Pick Up Claim" assigns the claim to the officer, moves status to `UnderReview`, and logs an event.
- **FR-2.3 Assessment & Reserve Adjustment**: Allow the officer to update the `AssessedReserve` amount and add internal assessment notes (`ClaimNote`), immediately recalculating outstanding liability exposure via domain events.
- **FR-2.4 Request Additional Information**: Send a structured question to the claimant, transitioning status to `InfoRequested`.
- **FR-2.5 Progress to Settlement or Rejection**:
  - **Settle**: Specify final `SettledAmount` and settlement decision notes -> transitions to `Settled`, zeroes out active reserve exposure, and increments settled payout totals.
  - **Reject**: Specify rejection reason -> transitions to `Rejected` and releases active reserve exposure.

### FR-3: Claims Manager Analytics & Workload View (`/workbench/analytics`)
- **FR-3.1 Real-Time Liability Exposure Dashboard**:
  - Total Consolidated Active Reserve Exposure (in USD).
  - Total Settled Payouts (in USD) & Total Released Reserves (in USD).
  - Breakdown by APAC Market (`SG`, `HK`, `AU`, `MY`, `TH`, `ID`) showing both native currency reserve totals and USD equivalent.
  - Breakdown by Line of Business (`Motor` vs. `Property`).
- **FR-3.2 Team Workload & Performance Matrix**:
  - Per-officer active claim count, awaiting-info count, settled/rejected count, total active reserve under management (USD), and average handling cycle time.
- **FR-3.3 Live Event Stream Ticker**:
  - Visual feed showing Outbox -> Kafka/EventBus domain events processed in real time via SignalR (`ClaimSubmittedDomainEvent`, `ClaimReserveUpdatedDomainEvent`, `ClaimInfoRequestedDomainEvent`, `ClaimSettledDomainEvent`, etc.).
