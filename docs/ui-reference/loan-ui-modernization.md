# Loan Tracking Application — UI Modernization Requirements

## 1. Objective

Modernize the existing Loan Tracking Application using the **Cambria UI Toolkit — Bright Indigo** as the primary design system.

Use the **Cambria CRM SaaS Dashboard mockup** as the reference for the overall application layout, including side navigation, header, content area, and light/dark mode behavior.

The application currently uses:
- ASP.NET Core backend
- Angular frontend
- Microsoft SQL Server database

**This is a UI modernization project, not a functional rewrite.**

Preserve all existing functionality, business logic, API integrations, database structures, calculations, and workflows.

## 2. Reference Files

Use these two files as the authoritative visual and styling references:

1. `Cambria CRM UI Kit - cambria-ui-toolkit.html`
2. `Cambria CRM SaaS Dashboard - cambria-dashboard-reference.html`

Read and analyze both files before making changes.

### Reference Priority

- **UI Toolkit:** Source of truth for design tokens, typography, component styles, CSS classes, colors, spacing, and component variants.
- **Dashboard Mockup:** Source of truth for application layout, sidebar navigation, header, dashboard composition, and responsive behavior.
- **Existing Loan Tracking Application:** Source of truth for business functionality, navigation items, data, workflows, and permissions.

When the dashboard mockup and toolkit have different component styling, follow the UI Toolkit.

Do not invent new visual styles when an equivalent component already exists in the toolkit.

## 3. Application Layout and Navigation

Apply the dashboard mockup's application shell to the Loan Tracking Application.

### Side Navigation

- Use the Bright Indigo sidebar styling.
- Support expanded and collapsed navigation.
- Display icons alongside menu labels.
- Highlight the active menu using the toolkit's selected navigation style.
- Support nested navigation where needed.
- Preserve existing routes and menu permissions.
- Adapt navigation for smaller screens.

**Important:** Use the Loan Tracking Application's existing navigation structure. Do not copy the CRM-specific menu items into the application.

### Top Header

Follow the dashboard mockup's header styling, including:

- Application title
- Sidebar collapse/expand control
- Theme toggle
- User profile/menu
- Existing header actions and notifications, where applicable

### Content Area

Maximize the available content width.

- Avoid unnecessary left and right margins.
- Use approximately 16–18px horizontal content padding on desktop.
- Keep spacing consistent between page titles, toolbars, cards, and grids.
- Avoid fixed maximum content widths that waste available screen space.
- Maintain responsive behavior.

## 4. Light Mode and Dark Mode

Implement both themes using the UI Toolkit's design tokens.

### Requirements

- Light mode must match the Bright Indigo reference.
- Dark mode must use the toolkit's corresponding dark palette.
- Provide a theme toggle in the application header.
- Persist the user's selected theme across page navigation and browser reloads.
- Support system theme preference when no explicit preference has been saved.
- Apply the theme consistently across all components.

Ensure the following work correctly in both modes:

- Sidebar and navigation
- Header
- Cards and KPI widgets
- Tables and grids
- Inputs and dropdowns
- Buttons
- Badges
- Modal dialogs
- Charts
- Tooltips and dropdown menus
- Validation messages

Avoid hardcoded colors that work only in light mode.

## 5. Typography

Use the typography defined in the Cambria UI Toolkit.

The reference toolkit uses **Plus Jakarta Sans** as its primary font.

Follow its existing:

- Font family
- Font sizes
- Font weights
- Line heights
- Text colors
- Page titles
- Section headings
- Form labels
- Table headers
- Helper and validation text

Do not introduce another font family or create inconsistent typography between pages.

## 6. Buttons and Form Controls

Standardize all existing buttons and form controls using the toolkit.

### Buttons

Use the existing toolkit variants, including:

- Primary
- Secondary
- Tertiary
- Danger
- Icon buttons
- Small buttons
- Disabled and loading states

Reuse existing CSS classes such as `btn`, `btn--primary`, `btn--secondary`, and other documented variants.

### Form Controls

Apply the toolkit's styling to:

- Text inputs
- Numeric inputs
- Currency inputs
- Date inputs and date pickers
- Dropdowns and searchable selects
- Textareas
- Checkboxes
- Radio buttons
- Switches
- Input groups
- Search fields

Maintain consistent control heights, borders, padding, corner radii, focus indicators, and validation states.

Preserve existing Angular reactive forms, validators, events, and data bindings.

## 7. Data Grids and Tables

Apply the toolkit's table and grid styling across the application.

This includes loan lists, customers, payment history, cash ledger, reports, and other existing grids.

### Grid Requirements

- Use the toolkit's **Comfortable** row spacing, approximately 40px.
- Maintain consistent cell padding and column alignment.
- Use the toolkit's table header styling.
- Apply subtle hover and selected-row backgrounds.
- Right-align currency and numeric values.
- Use consistent date and currency formatting.
- Preserve existing pagination and filtering.
- Preserve sorting and enable column sorting where supported by the underlying data.
- Preserve row actions and bulk actions.

### Grid Toolbars

Standardize grid toolbars using toolkit controls.

Where applicable, include:

- Search input
- Status dropdown
- Date-range filters
- Additional existing filters
- Clear/reset filters
- Existing action buttons

Do not introduce new filters that are unsupported by the existing data or APIs.

Ensure that search, filters, sorting, and pagination continue working together.

## 8. Badges and Status Labels

Use the **Compact Soft Badge — ALL CAPS** style as the default for status columns in data grids.

Follow the toolkit's existing CSS classes and variant names.

### Badge Specifications

- Font size: 9px
- Font weight: 600–700
- Text transform: uppercase
- Horizontal padding: 6px
- Vertical padding: 2px
- Border radius: 3px
- Minimum height: approximately 18px
- Soft pastel background with readable foreground color

Use the toolkit's existing semantic colors for success, warning, danger, primary, and neutral states.

Examples of loan-related statuses include:

- ACTIVE
- PAID
- OVERDUE
- EXTENDED
- BAD LOAN
- CLOSED

Map actual application statuses to appropriate badge colors without changing their underlying values or meanings.

For detail pages, use the toolkit's Title Case badge variation where appropriate.

Do not create new versioned badge CSS classes.

## 9. Dashboard Modernization

Use the Cambria CRM Dashboard mockup as the visual reference for the Loan Tracking dashboard.

**Do not replace the existing loan dashboard's business metrics with CRM metrics.**

Retain the existing financial KPIs, charts, and calculations.

### Dashboard Components

Apply the toolkit's styling to:

- KPI summary cards
- Financial charts
- Progress bars
- Recent transactions
- Loan activity tables
- Financial snapshots
- Capital growth charts
- Other existing dashboard widgets

Use the toolkit's Bright Indigo palette, with distinct semantic colors where appropriate.

Avoid using Indigo for every chart series or progress bar.

Use brighter, complementary colors such as teal, green, amber, and blue to improve visual differentiation.

Do not change financial formulas or how values are calculated.

## 10. Dialogs, Notifications, and Other Components

Standardize existing components using the toolkit's documented variants.

This includes:

- Modal dialogs
- Confirmation dialogs
- Dropdown menus
- Toast notifications
- Alerts
- Tabs
- Progress bars
- Loading indicators
- Empty states
- Pagination
- Tooltips
- Notification counters

Preserve the application's existing interactions and behavior.

## 11. Implementation Guidelines

### Inspect Before Modifying

First examine the existing Angular application structure, stylesheets, component libraries, and layout implementation.

Identify reusable components and determine how best to integrate the toolkit.

### Shared Design System

Create or update a centralized design system using the toolkit's CSS variables and stable class names.

Prefer reusable Angular components or shared styles over duplicating CSS in individual pages.

### CSS Naming

- Preserve the toolkit's existing CSS class names.
- Do not append version numbers to CSS classes.
- Do not create duplicate or conflicting component styles.
- Avoid excessive use of `!important`.
- Use design tokens rather than hardcoded colors.
- Reuse documented toolkit variants wherever possible.

### Compatibility

Do not replace existing Angular component libraries unnecessarily.

If the application uses Angular Material, PrimeNG, or another UI library, adapt its appearance to the toolkit without breaking its functionality.

Do not modify backend APIs or database structures for cosmetic changes.

## 12. Implementation Phases

### Phase 1 — Application Shell

Implement the sidebar, header, content layout, responsive behavior, typography, and light/dark mode.

### Phase 2 — Shared UI Components

Standardize buttons, inputs, dropdowns, tables, badges, dialogs, tabs, and other reusable components.

### Phase 3 — Dashboard

Apply the new design to the existing Loan Tracking dashboard while retaining all financial metrics and charts.

### Phase 4 — Application Pages

Apply the shared styles to the remaining application screens, including loan management, customers, payments, cash ledger, reports, diary, and other existing modules.

### Phase 5 — Validation

Verify visual consistency, responsiveness, functionality, and light/dark mode across the application.

## 13. Acceptance Criteria

The modernization is complete when:

- The application visually follows the Bright Indigo UI Toolkit.
- The sidebar and header follow the dashboard mockup.
- Light and dark modes work consistently.
- Typography, spacing, buttons, inputs, dropdowns, tables, and badges match the toolkit.
- Grid status columns use compact soft uppercase badges.
- Grid rows use comfortable spacing.
- The content area uses available screen width efficiently.
- All existing functionality, calculations, and workflows remain unchanged.
- Shared CSS and components are reusable and maintainable.
- No version numbers are introduced into CSS class names.

## 14. Instructions for Claude Code

1. Read both reference HTML files.
2. Inspect the existing Loan Tracking Angular application.
3. Identify the current layout, theme, and component architecture.
4. Prepare a brief implementation plan identifying affected files and reusable components.
5. Implement the modernization in phases.
6. Reuse the toolkit's CSS and design tokens wherever possible.
7. Preserve all existing application functionality.
8. Run available build and validation checks after implementation.
9. Report the changes made, affected files, and any remaining inconsistencies.

**Important:** Prioritize visual consistency, component reuse, and maintainability. Do not rewrite working application functionality simply to apply the new theme.