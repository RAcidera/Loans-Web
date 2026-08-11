---
name: generate-plan
description: Turn a requirements document (SRS, feature brief, spec — markdown, .txt, or PDF) into a phased implementation plan for this repo. Required input — requirementsPath, the file name or path of the requirements document — since the skill has nothing to plan from without it; ask the user if it wasn't passed in args. Use when the user asks to "generate a plan from these requirements", "turn this spec into a plan", "create an implementation plan for <file>", or hands over a requirements document and asks what to build from it.
---

# Generate an implementation plan from a requirements document

Pairs with `execute-plan` — this skill only produces the plan document; it
never starts implementing. Handing off cleanly at the end is part of the
job, not a corner being cut.

## Required input: `requirementsPath`

This skill needs one input before doing anything: `requirementsPath`, a
file name or path to a requirements document (`.md`, `.txt`, or `.pdf`).
If it wasn't passed in `args`, ask the user with `AskUserQuestion` before
doing anything else — don't guess which file in the repo they mean.

Resolve relative paths against the repo root. If the path doesn't exist,
or its extension isn't `.md`/`.txt`/`.pdf`, say so and stop rather than
silently searching for a similarly-named file — the user may have a typo
worth catching, not a file worth guessing at.

## Step 1 — Read the requirements document in full

- `.md`/`.txt`: `Read` directly.
- `.pdf`: `Read` supports PDFs via its `pages` parameter, max 20 pages per
  call. For anything longer, read it in ≤20-page chunks across multiple
  calls and hold the whole thing in context before moving on — don't plan
  off a partial read of a long PDF.
- If the document references other files (an SRS that says "see
  Appendix B" or points at diagrams/wireframes elsewhere in the repo),
  track those down too before scoping — a plan built on a partial reading
  of its own source document will misjudge scope.

## Step 2 — Extract discrete requirements, keep them traceable

Pull out individual functional requirements, preserving whatever
section/heading numbering the source document uses (e.g. "SRS 3.5"). The
generated plan should cite these back — same convention
`loan-management-implementation-plan.md` already uses in this repo
("Reports Page (SRS wireframe 6, SRS 3.5)") — so a reviewer can trace any
phase back to the requirement that justifies it.

Separate non-functional requirements (performance, security, auth,
deployment) from feature requirements — these usually become their own
phase or a cross-cutting acceptance criterion on every phase, not a
feature phase of their own.

## Step 3 — Gap analysis against current repo state

Don't plan to build what already exists. Before sequencing phases, check
what's actually there:

- For a backend requirement: does the aggregate/command/query/endpoint
  already exist in Domain/Application/Infrastructure/Api? Use Glob/Grep,
  or delegate to an Explore agent if the check spans more than a couple of
  targeted searches.
- For a frontend requirement: does the component, route, use-case, or
  repository method already exist under `presentation/`,
  `app.routes.ts`, `application/use-cases/`?
- This mirrors a standing rule already in `CLAUDE.md` for this repo — verify
  actual state by reading code, not by trusting a doc's claims about
  what's built. Apply the same discipline to the *requirements* document
  itself: it may describe things that shipped since it was written.
- If the target is a brand-new/empty codebase, this step will correctly
  degenerate to "everything is a gap" — don't force a false distinction
  where none exists.

## Step 4 — Sequence into phases

Read `loan-management-implementation-plan.md` at the repo root once, even
if the requirements you're planning are unrelated to loans — it's this
repo's existing plan-writing convention, and consistency across plans in
the same repo matters more than any generic external template. Match its
shape:

- **Guiding principles**, 3–5 short ones, stated up front — typically some
  variant of "verify before building," "follow the existing pattern, don't
  invent a new one," "test what's actually risky, not everything," and
  "one phase should be shippable before the next starts."
- **Phase 0 — verification**, if and only if the current codebase has any
  unverified/uncompiled/untested state to gate on (a fresh clone that's
  never been built, a recent large change with no green test run). Skip it
  and say why if the repo is already known-green — don't manufacture a
  Phase 0 for its own sake.
- **Each subsequent phase** gets:
  - A one-line **"Why here"** — the sequencing rationale (risk, dependency
    on an earlier phase, how much of the backend already exists), not just
    a restatement of the feature.
  - A **backend table** (capability → implementation) and/or **frontend
    table** (component → purpose) as applicable, phrased in terms of this
    repo's actual layering from `CLAUDE.md` (Domain → Application →
    Infrastructure → Api on the backend; domain entity → use-case →
    component on the frontend) rather than generic task names.
  - Explicit **acceptance criteria** — a concrete, user-observable action
    ("select a date range, see interest earned for it, export a CSV that
    opens correctly"), never just "code compiles."
  - A rough **effort estimate** in days.
- Order phases by risk and dependency, not by the source document's
  order — and say why each phase sits where it does, the same way the
  existing plan explains "why first/second/third."
- Close with a note on which phases are independent/parallelizable, if
  that's true here — the existing plan does this for its Phases 1–4.

## Step 5 — Write the plan file

Output path: same directory as `requirementsPath`, named
`<requirements-basename>-implementation-plan.md`. Strip a trailing
`-requirements`/`-spec`/`-srs` from the basename first if present, so
`loan-srs-requirements.md` becomes `loan-srs-implementation-plan.md`, not
`loan-srs-requirements-implementation-plan.md`.

If a file already exists at that output path, ask before overwriting —
it may be a previous plan that's already partway through execution, and
clobbering it would lose track of what's already shipped.

Title the document `# Implementation Plan — <subject>` and open with a
short "current state" paragraph, mirroring the opening of
`loan-management-implementation-plan.md`.

## Step 6 — Report, don't execute

Summarize phase count, total rough effort, and the output file path. Stop
there — implementing is `execute-plan`'s job, not this skill's. If the
user wants to jump straight into building, say so explicitly and hand off
to `execute-plan` with the new file's path rather than silently starting
to code.
