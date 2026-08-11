---
name: enhance-mobile-responsive-design
description: Redesign the admin dashboard's visual identity to match a reference "modern fintech SaaS" template (dark indigo sidebar, icon+trend KPI cards, tabbed status-filtered tables, status pills, icon-in-circle list widgets) and make the result usable on phone/tablet widths. The app currently has almost no mobile handling — only 4 SCSS files have any @media query — and its current palette (pine green/amber) and component styling are visually plain. Use when the user asks to "make it mobile friendly", "improve the responsive design", "make the app more user friendly", "redesign the UI", or points at a reference screenshot/template to follow.
---

# Redesign visual theme + mobile responsiveness

## Re-verify before starting

This skill was written from a snapshot of `src/loan-manager-admin-angular/`. Before
acting on any fact below, re-grep for `@media` and `.chip`, and re-open
`_tokens.scss` / `admin-shell.component.*` — someone may have already started this
work since the skill was written.

## The reference template — what to take from it, and what not to

The visual target is a dark-sidebar fintech dashboard reference (screenshot supplied
in conversation, not a file in this repo — re-ask the user for it if it isn't
available when this skill is next invoked). Its **information architecture is a
different product** (a lead-gen CRM with "Leads Management" / "Task & Reminders"
nav items) — **do not** copy its nav items or invent matching business concepts.
This app's domain stays Customers / Loans / Cash & Funds / Reports / Settings. Only
port over the *visual and interaction patterns*, reskinned onto our existing pages
and entities:

- Dark near-black/indigo sidebar (currently pine green `#16423C`) with a solid
  rounded-pill active-nav highlight, a promo card pinned above the user footer, and
  a user row with avatar + name + role + settings icon.
- Light topbar with icon actions (search / notifications / account) — the current
  topbar (`admin-shell.component.html:26-50`) already matches this shape; it mainly
  needs recoloring, not restructuring.
- KPI stat cards built from **icon-in-a-tinted-circle + label + big value + a
  colored trend line** (e.g. "+11.02% ↗ vs last month"), instead of this app's
  current plain-text-plus-left-accent-bar KPI cards
  (`dashboard.component.scss:14-19` `border-left: 3px solid var(--lm-primary-light)`).
- A tabbed status filter across the top of a data table (`Posts 40 / New 8 / In
  Progress 16 / Pending 12 / Approved 8 / Rejected 4`), each tab showing a live
  count badge. **This maps directly onto data already on hand** — the `Loan`/
  `Customer` entities already have a `status` field
  (`domain/entities/loan.entity.ts`'s `LoanStatus = 'active'|'extended'|'paid'|'overdue'`)
  that the tabs would just filter by, client-side, alongside the existing search
  box. This is a genuinely buildable feature, not just a re-skin — treat it as one.
- Status pills recolored per-status (light tint background + saturated text), same
  concept as this app's existing `.chip`/`.chip--<status>` pattern
  (`customers.component.html:38`, `loans.component.html:52`, etc.) — just a new
  palette, not a new mechanism.
- Two icon-in-circle action buttons per table row (call / message) plus an overflow
  menu — the current app has one action icon (`visibility`, opens the details
  dialog). Only add call/message actions if the user actually wants that
  functionality; don't add inert decorative buttons just to match the screenshot.
- Right-rail list widgets ("Today's Task", "Alerts & Notifications") built from
  **icon-in-circle + title + subtitle + trailing value** rows. The dashboard's
  existing "Recent payments" feed (`dashboard.component.html:72-91`,
  `.feed-item` in `dashboard.component.scss:161-203`) already has this exact
  shape (icon slot is currently unused — it's icon-less today) — reskinning it is
  a visual pass. A literal second "Alerts & Notifications" panel populated with
  real overdue-loan/pending-task data would be a **new feature requiring new
  application-layer queries**, not styling — confirm with the user before building
  it rather than inventing mock alert content to match the screenshot.
- Small line-sparkline stat cards and a circular "Target Achieved" progress/illustration
  widget are the most bespoke, lowest-priority pieces (custom SVG or illustration
  asset needed for the target/bullseye graphic) — treat as stretch, not core.

## New design tokens (`_tokens.scss`)

The current palette (`--lm-primary: #16423C` pine green, `--lm-amber`, `--lm-rose`,
warm-paper `--lm-bg: #F6F5F1`) does **one thing the reference doesn't**: it reuses
the same `--lm-primary` token for both the sidebar's background *and* the brand's
interactive accent color. The reference uses two distinct values — a near-black
indigo sidebar container, and a separate, brighter indigo/purple for buttons,
links, active-nav pills, and tab underlines. **Splitting these into two tokens is
the key architectural change**, not just new hex values:

```scss
:root {
  // Brand accent (buttons, links, active-nav pill, tab underline) — was --lm-primary
  --lm-primary: #6C5DD3;
  --lm-primary-dark: #5847C4;   // hover/pressed

  // Sidebar container — new, decoupled from --lm-primary
  --lm-sidebar-bg: #14121F;
  --lm-sidebar-elevated: #1E1B2E; // promo card surface inside the sidebar
  --lm-sidebar-text-muted: rgba(255, 255, 255, 0.6);

  // Status semantics — adds --lm-info (blue), which doesn't exist today
  --lm-success: #22A06B;  // Approved / Active / positive trend
  --lm-warning: #E1AA36;  // Pending / Follow-up (keep existing --lm-amber value here)
  --lm-danger: #C1666B;   // Overdue / Rejected (keep existing --lm-rose value here)
  --lm-info: #4C8DFF;     // In Progress — net-new, no equivalent today

  // Surface/neutrals — lighter, cooler than the current warm-paper palette
  --lm-bg: #F6F6FB;
  --lm-surface: #FFFFFF;
  --lm-border: #ECECF3;
  --lm-text: #1B1B29;
  --lm-text-muted: #8B8FA3;
}
```

These are **inferred from a screenshot, not exact brand hex values** — treat them
as a reasonable starting point and adjust against real design assets (Figma, brand
guide) if the user has them, rather than treating these as pixel-exact.

Because most color usage already flows through these CSS custom properties, most
components will re-theme automatically once `_tokens.scss` changes. **The exception
is status-chip colors**, which are hand-duplicated as literal rgba/hex shades
(not `var(--lm-*)`) in each page's own SCSS, per this project's deliberate
per-component-duplication convention (see `CLAUDE.md`). Confirmed via grep, these
7 files each define their own `.chip--<status>` rules and must be edited
individually when the status palette changes: `customers.component.scss`,
`loans.component.scss`, `dashboard.component.scss`, `reports.component.scss`,
`customer-profile.component.scss`, `cash-funds.component.scss`,
`settings.component.scss`.

## Component-by-component mapping

- **Sidebar** (`admin-shell.component.scss:11-24`): `background: var(--lm-primary)`
  → `var(--lm-sidebar-bg)`. Active nav item (`:82-86` `.nav__item--active`,
  currently a translucent `rgba(255,255,255,0.14)` overlay) → a solid
  `background: var(--lm-primary)` rounded pill, matching the reference's filled
  active state. Add a promo/CTA card above the user footer (new markup — doesn't
  exist today) using `--lm-sidebar-elevated` as its surface.
- **Topbar** (`admin-shell.component.html:26-50`): structurally already matches
  (title left, icon actions right) — this is a recolor, not a rebuild.
- **KPI cards** (`dashboard.component.html:1-14`, `.kpi-card` in
  `dashboard.component.scss:14-60`): add an icon-in-tinted-circle slot per card and
  a trend row (value + arrow + percent + "vs last month"), replacing the current
  `border-left` accent-bar treatment. `reports.component.scss` has an analogous
  `.kpi-row`/`.kpi-card` block (`:49` `repeat(4, 1fr)`) that should get the same
  treatment for consistency.
- **Tables + status tabs**: `customers.component.ts`/`loans.component.ts` currently
  filter only via free-text search (`applyFilter`, `dataSource.filter`). Adding
  status tabs means adding a `MatTabsModule`/button-group control bound to a status
  signal that further narrows `dataSource.filteredData` (or composes with the
  existing text filter) — needs a small logic change, not just template/CSS.
- **Status pills**: recolor `.chip--*` rules in the 7 files listed above to the new
  `--lm-success`/`--lm-warning`/`--lm-danger`/`--lm-info` tokens, keeping today's
  light-tint-background + saturated-text formula.
- **Buttons**: primary actions (`mat-flat-button color="primary"`) inherit the new
  `--lm-primary` automatically via the Material theme in `styles.scss` — verify
  `mat.define-theme`'s `primary` palette is updated to match (`mat.$green-palette`
  → a Material indigo/violet palette, or a custom palette built from `#6C5DD3` via
  `mat.define-palette`/M3 tooling) rather than leaving the theme's button color out
  of sync with the new CSS-custom-property accent used elsewhere.

## Suggested order of work

This is a broad ask spanning both a visual re-theme and structural responsive
work — sequence it as reviewable steps, and confirm scope if the user only meant
part of this (e.g. "just restyle the dashboard" vs. the whole app):

1. **Tokens first.** Land the `_tokens.scss` changes above (including the
   sidebar/primary split) and update `styles.scss`'s Material theme palette to
   match — this alone re-themes most surfaces, borders, and text automatically.
2. **Status chip recolor** across the 7 files listed above, introducing
   `--lm-info` for "in progress"-style statuses that have no current equivalent.
3. **Sidebar + topbar reskin** (`admin-shell.component.*`) — dark container, solid
   active-nav pill, promo card, user footer polish.
4. **KPI card pattern** (icon-in-circle + trend row) on dashboard and reports.
5. **Table status tabs** (new interactive filter, confirm with user if in scope)
   and the **right-rail widget reskin** (recent-payments feed → icon-in-circle
   list rows) — keep the "Alerts & Notifications" panel as a flagged, separate
   product decision (real data plumbing) rather than inventing content for it.
6. **Breakpoint foundation.** Add mobile/tablet breakpoint mixins to `_tokens.scss`
   alongside the existing (currently-unused) `lm-card`/`lm-mono-figure` mixins, so
   every component's existing `@use '../../styles/tokens';` can reach them via
   `@include tokens.mobile { ... }`:
   ```scss
   $lm-bp-mobile: 599px;
   $lm-bp-tablet: 899px;

   @mixin mobile {
     @media (max-width: $lm-bp-mobile) { @content; }
   }

   @mixin tablet {
     @media (max-width: $lm-bp-tablet) { @content; }
   }
   ```
   The existing 900–1100px desktop breakpoints in dashboard/reports/customer-profile/
   cash-funds (see table below) are a different concern (desktop grid reflow) and
   can stay as-is.
7. **Admin shell mobile nav.** Convert the now-restyled sidebar into an off-canvas
   drawer below the tablet breakpoint, with a hamburger button in `.topbar`. CDK
   20.2 is already a dependency with `BreakpointObserver`
   (`@angular/cdk/layout`) unused anywhere in `src/app` — use it to drive a
   `mobileNavOpen` signal instead of hand-rolled `matchMedia`. Keep the existing
   desktop `collapsed` icons-only mode (`admin-shell.component.ts:40`,
   `toggleSidebar()` at `:59-61`) untouched — the drawer is a separate state.
8. **Table overflow.** Wrap every `<table mat-table>` (7 usages: customers, loans,
   dashboard, reports, cash-funds, customer-profile, loan-details-dialog's history
   table) in a scroll container — `<div class="table-scroll"><table ...>` with
   `.table-scroll { overflow-x: auto; }`. Don't hide columns on mobile unless asked.
9. **Dialog mobile fallback.** Add `maxWidth: '95vw'` to the 5 `MatDialog.open()`
   calls that lack it: `customers.component.ts:104` (`AddCustomerDialogComponent`),
   `loans.component.ts:96` (`AddLoanDialogComponent`),
   `loan-details-dialog.component.ts:81-84` and `:94-97`
   (`AddPaymentDialogComponent`/`ExtendLoanDialogComponent`), and
   `cash-funds.component.ts:79` (`AddCashTransactionDialogComponent`). Then relax
   the conflicting fixed `min-width` rules for phones — `_shared-dialog-form.scss:10-14`
   (`min-width: 320px`) and `loan-details-dialog.component.scss:12-24`
   (`min-width: 420px`) — via the new `mobile` mixin rather than deleting them
   outright. Also give `loan-details-dialog.component.scss:47`'s
   `.dialog__summary` (`grid-template-columns: repeat(4, 1fr)`, currently
   unguarded) a mobile breakpoint, e.g. 4→2 columns.
10. **Touch targets.** Audit icon-buttons for real ≥44px hit areas (nav items,
    table action buttons, dialog close/cancel). Raise
    `admin-shell.component.scss:141-143`'s touch-target suppression
    (`&::ng-deep .mat-mdc-button-touch-target { display: none; }` on the 34×34px
    topbar avatar) with the user before changing it — it may be deliberate.

## Current responsive-state audit (facts, unaffected by the re-theme)

| File | Line | Rule |
|---|---|---|
| `dashboard.component.scss` | 9 | `@media (max-width: 1100px)` — `.kpi-row` 5→2 cols |
| `dashboard.component.scss` | 67 | `@media (max-width: 1000px)` — `.two-col` 2fr/1fr → 1fr |
| `reports.component.scss` | 53 | `@media (max-width: 1100px)` — `.kpi-row` 4→2 cols |
| `reports.component.scss` | 92 | `@media (max-width: 1000px)` — `.two-col` → 1fr |
| `customer-profile.component.scss` | 26 | `@media (max-width: 1000px)` — `.two-col` → 1fr |
| `cash-funds.component.scss` | 18 | `@media (max-width: 900px)` — `.funds-row` → 1fr |

Everything else — `admin-shell`, `customers`, `loans`, `loan-timeline`, every
dialog, `login`, `settings`, `app.scss`, `styles.scss` — has **zero** responsive
handling. No dark-mode support exists either (`styles.scss` hardcodes
`mat.define-theme((color: (theme-type: light, ...)))`) — out of scope here unless
asked separately.

## Verification

This is almost entirely visual/layout work — `ng build` passing proves nothing
about whether it actually looks right. Do a real browser check (this project's
standing rule for UI changes):

- Both the new palette **and** layout at **375px** (iPhone SE/mini — tightest
  target; dialogs and the sidebar drawer must not overflow horizontally),
  **768px** (tablet — verify the new mobile-nav breakpoint and the existing
  900/1000/1100px desktop breakpoints don't fight each other in between), and
  **1440px** (desktop — confirm the re-theme didn't break the already-working
  layout).
- Contrast-check new status pill and sidebar text colors — a dark sidebar with
  light text and light pill backgrounds with saturated text both need a real
  contrast pass, not just visual approximation from a screenshot.
- Re-test the row-click + action-button double-dialog bug
  (`$event.stopPropagation()` pattern, see the loans/dashboard/customer-profile
  tables) if table-wrapper or action-button changes touch those templates — don't
  reintroduce it while adding a scroll wrapper or new call/message icons.
