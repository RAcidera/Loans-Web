---
name: execute-implementation-plan
description: Execute or continue work on loan-management-implementation-plan.md — the phased backend+frontend delivery plan for this repo (Phase 0 build verification through Phase 6 perf testing). Use when the user asks to "work on the implementation plan", "continue the plan", "do the next phase", "implement phase N", or references phases/deliverables from that file.
---

# Execute the Loan Management implementation plan

This skill drives work against `loan-management-implementation-plan.md` at the
repo root. That file is the source of truth for scope — re-read it at the
start of every invocation rather than relying on memory of a past run; it may
have been edited.

## Ground rules from the plan itself

- **Phase 0 is a hard gate.** Nothing else in the plan may start until Phase 0's
  acceptance criteria pass. If asked to jump to a later phase, still check
  Phase 0's evidence first (below) and flag it if unverified rather than
  silently skipping ahead.
- **Follow existing patterns, don't invent new ones.** Every backend feature is
  domain change (if any) → use case/command+handler → EF config (if new
  entity) → controller endpoint. Every frontend feature is domain entity →
  use case → component, wired through existing repository ports. Before
  writing a new command/handler/component, find and mirror the nearest
  existing analog named in the plan (e.g. `CreateCustomerCommand` as the
  template for `UpdateCustomerCommand`, `add-payment-dialog` as the template
  for `add-customer-dialog`).
- **One phase = one shippable increment.** Do not spread partial work across
  multiple phases in one pass. Finish a phase's acceptance criteria before
  starting the next phase's tasks.
- **Test what's risky, not everything.** Domain logic → unit tests. Auth/money
  movement (cross-cutting) → integration tests. Pure UI → neither, per the
  plan's own guidance.

## Step 1 — Determine current phase

Don't ask the user which phase to run if it's derivable from repo state.
Check evidence in this order and pick the first phase whose acceptance
criteria are NOT yet satisfied — that's the phase to work on:

| Phase | Quick evidence check |
|---|---|
| 0 | Has `dotnet build`, `dotnet test`, and an `ef migrations`/`ef database update` run succeeded recently? No local migration under `src/**/Migrations/` and no record of a green `dotnet test` run means Phase 0 is still open. |
| 1 | Does `UpdateCustomerCommand`, `PUT /api/customers/{id}`, and a `presentation/customers/` Angular component all exist? |
| 2 | Do `GetInterestSummaryQuery`, `GetCustomerSummaryQuery`, `GetPeriodSummaryQuery`, a reports controller, and `presentation/reports/` exist? |
| 3 | Does `UserStatus`, `ChangePasswordCommand`, `DeactivateUserCommand`, a `UsersController`, and a routed `presentation/settings/` exist? |
| 4 | Is `FluentValidation` referenced in the Application/Api `.csproj`, and does a `ValidationBehavior<,>` pipeline behavior exist? |
| 5 | Does `.github/workflows/ci.yml` exist? |
| 6 | Do Karma/Jasmine specs exist for use cases beyond the default scaffold, and is there a k6 (or similar) load-test script? |

Use Glob/Grep to check these directly — don't guess from memory of a
previous session, the repo may have changed.

If the user explicitly names a phase, honor it, but still surface a warning
if an earlier phase's evidence looks incomplete (they may want to know before
building on shaky ground).

## Step 2 — Scope the phase's tasks

Open the matching `## Phase N` section in `loan-management-implementation-plan.md`
and re-read it in full — table rows, acceptance criteria, and effort estimate.
Use TodoWrite to break it into the backend/frontend rows from that phase's
tables as discrete todos, plus one final todo for "verify acceptance
criteria." Phases 1–4 have no dependencies on each other (per the plan's
closing note), so if the user wants two phases done in parallel across
separate work, that's a legitimate reordering — Phase 0 is still the
exception that must always come first.

## Step 3 — Implement

For each task:
- Grep for the nearest existing analog mentioned in the plan (or the closest
  equivalent if the plan doesn't name one) and mirror its shape: same layering,
  same naming convention, same DI registration style, same test style.
- Backend: Domain change → Command/Query + Handler → EF config if a new
  entity → controller endpoint, in that order.
- Frontend: use-case wrapper → component → route addition (guarded like
  existing routes), in that order.
- Keep changes scoped to the current phase. Don't pull forward work from a
  later phase (e.g. don't add FluentValidation while doing Phase 1 — that's
  Phase 4) and don't leave half-finished stubs for future phases.

## Step 4 — Verify against the phase's acceptance criteria

Run exactly what the plan's acceptance criteria for this phase call for
before declaring it done. Common ones:

- `dotnet build` and `dotnet test` clean (all backend phases).
- For Phase 0 specifically: `dotnet ef migrations add InitialCreate`,
  `dotnet ef database update` against a real MySQL instance, and a manual
  `ng serve` click-through (login → dashboard → loan details → record a
  payment → cash-funds page reflects it). Flag to the user which of these you
  could not exercise yourself (e.g. no live MySQL instance, no browser) so
  they know what still needs manual confirmation.
- For frontend-facing phases: `ng build` clean at minimum; note explicitly
  if you were not able to click through the actual UI and that this still
  needs human verification, per this project's own standing rule that UI
  changes need a real browser check before being called done.

## Step 5 — Report and checkpoint

Summarize what shipped against the phase's acceptance criteria, call out
anything unverified, and stop. Do not auto-start the next phase — each phase
is a multi-day, shippable unit per the plan's own design, so hand control
back to the user with a clear "Phase N done, Phase N+1 is next: <one-line
summary>" rather than continuing unprompted through the whole 11–18 day plan.
If the user says "keep going" / "do the next one", proceed to the next phase
by re-running this skill's steps.
