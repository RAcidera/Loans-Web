---
name: execute-plan
description: Execute or continue work on a phased implementation plan document (markdown, .txt, or PDF — e.g. one produced by `generate-plan`, or any other hand-written plan). Required input — planPath, the file name or path of the plan — since the skill has nothing to execute without it; ask the user if it wasn't passed in args. Use when the user asks to "execute this plan", "work on <plan file>", "continue the plan", "do the next phase", or references phases/deliverables from a plan file other than `loan-management-implementation-plan.md` (for that specific file, prefer the `execute-implementation-plan` skill instead — see Step 0).
---

# Execute a phased implementation plan

Pairs with `generate-plan` — that skill produces the plan document, this
one drives work against it. The plan file itself is always the source of
truth for scope; re-read it at the start of every invocation rather than
relying on memory of a previous run, since it may have been edited (by a
person, or by a previous run of this same skill).

## Required input: `planPath`

This skill needs one input before doing anything: `planPath`, a file name
or path to a plan document (`.md`, `.txt`, or `.pdf`). If it wasn't passed
in `args`, ask the user with `AskUserQuestion` before doing anything else —
don't guess which plan in the repo they mean, especially once more than
one exists.

Resolve relative paths against the repo root. If the path doesn't exist,
or its extension isn't `.md`/`.txt`/`.pdf`, say so and stop.

## Step 0 — Special-case this repo's existing plan

If `planPath` resolves to `loan-management-implementation-plan.md` at the
repo root, stop here and invoke the `execute-implementation-plan` skill
instead. That skill already encodes a pre-built phase-evidence table
specific to that exact document (which files/commands prove each phase is
done) — re-deriving the same thing generically here would duplicate it and
risk drifting out of sync as that plan evolves. This skill is for every
*other* plan document.

## Step 1 — Read the plan in full

- `.md`/`.txt`: `Read` directly.
- `.pdf`: `Read` supports PDFs via `pages`, max 20 per call — chunk through
  longer documents rather than planning off a partial read.
- Identify every `## Phase N` (or equivalent) heading, its "why here"
  rationale if stated, its task table(s), and its acceptance criteria.
  These are what Steps 2–5 below operate on.

## Step 2 — Determine current phase

Don't ask the user which phase to run if it's derivable from repo state.
For each phase, earliest first, check whether its stated acceptance
criteria and named deliverables (the specific files, commands, classes,
or endpoints its own tables cite) already exist — use Glob/Grep for
direct name checks, or delegate to an Explore agent if a phase's evidence
spans more than a couple of targeted searches. Pick the first phase whose
evidence is NOT yet satisfied — that's the phase to work on.

If an earlier phase reads as a hard gate (the plan says so explicitly, or
it's obviously a build/environment-verification phase everything else
depends on), don't skip past it even if the user names a later phase —
flag the gap and confirm before proceeding on unverified ground.

If the user explicitly names a phase, honor it, but still surface a
warning if an earlier phase's evidence looks incomplete.

## Step 3 — Scope the phase's tasks

Re-read the matching phase section in full — every table row, every
acceptance criterion, the effort estimate. Use TodoWrite to turn its
task/table rows into discrete todos, plus one final "verify acceptance
criteria" todo. If the plan's closing notes call out phase independence
(some phases don't depend on each other), a legitimate reordering across
separate work is fine — a stated hard gate (Step 2) is the one exception
that always comes first regardless.

## Step 4 — Implement

For each task:
- Find the nearest existing analog already in the codebase and mirror its
  shape — same layering, same naming convention, same DI registration
  style, same test style. Don't invent a new pattern when one already
  exists to copy; if the plan names a specific analog, start there, but
  verify it still exists and still looks the way the plan describes
  before treating it as gospel.
- Backend work in this repo follows `CLAUDE.md`'s layering: Domain change
  (if any) → Application command/query + handler → Infrastructure EF
  config (if a new entity) → Api controller endpoint, in that order.
- Frontend work follows the Angular app's layering: domain entity →
  application use-case → presentation component, wired through an
  existing (or, if genuinely new, newly added) repository port in
  `app.config.ts`.
- Stay inside the current phase's scope. Don't pull forward work that
  belongs to a later phase, and don't leave half-finished stubs behind
  for a future phase to clean up.

## Step 5 — Verify against the phase's stated acceptance criteria

Run exactly what the phase calls for — don't invent a stricter or looser
bar than the plan states. Typical checks in this repo:

- `dotnet build` and `dotnet test` clean, for any backend change.
- `ng build` clean, plus a real browser click-through, for any
  frontend-facing change — this repo's standing rule is that UI changes
  need an actual browser check before being called done, not just a clean
  build. Say explicitly if you weren't able to exercise the browser
  yourself.
- Anything the phase's table names specifically — a migration command, a
  direct HTTP round-trip via curl against a running API, a specific query
  handler's unit test.

Flag anything you couldn't verify yourself (no reachable database, no
browser) rather than reporting it as done.

## Step 6 — Report and checkpoint

Summarize what shipped against this phase's acceptance criteria, call out
anything left unverified, and stop — don't automatically continue to the
next phase. Each phase is meant to be a shippable, reviewable increment;
hand control back with a clear "Phase N done, Phase N+1 is next: <one-line
summary>" rather than barreling through the whole plan unprompted. If the
user says "keep going" / "do the next one," re-run from Step 2.
