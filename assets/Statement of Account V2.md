TASK: Create Statement of Account V2 as downloadable pdf format using Loan Ledger

IMPORTANT:
Do NOT replace, modify, rename, or remove the existing Generate SOA functionality.

The existing Statement of Account must remain fully functional.

Create a NEW Statement of Account version called:

Statement of Account V2

The purpose of V2 is to present all financial activity affecting a loan in one chronological Account Activity ledger, rather than displaying Extension History and Payment History separately.

==================================================
1. REVIEW EXISTING IMPLEMENTATION FIRST
==================================================

Before coding:

1. Review the existing Generate SOA implementation end-to-end.
2. Identify:
   - Angular component/page/button that triggers Generate SOA
   - API endpoint
   - backend service
   - DTO/view model
   - PDF generation implementation
   - database queries
   - existing SOA formatting/styles
3. Review the loan_ledger table/entity and how records are created.
4. Verify the existing loan ledger fields and their actual names/types.

The loan ledger already contains fields equivalent to:

- transaction_date
- transaction_type
- debit
- credit
- remarks

Use the existing schema and naming conventions.

Do not create a duplicate ledger table.

Also determine the existing primary key / sequence field for loan_ledger and use it as a deterministic secondary sort when multiple transactions have the same transaction_date.

Do not modify unrelated functionality.

==================================================
2. KEEP CURRENT SOA
==================================================

The existing:

Generate SOA

function must remain unchanged and available.

Add a separate function/action for:

Generate SOA V2

For now, both versions should coexist so the output can be compared before deciding whether V2 will eventually replace the original.

Prefer reusing existing common services, PDF utilities, formatting, customer retrieval, loan retrieval, etc., but do not introduce changes that could break the original SOA.

==================================================
3. PURPOSE OF SOA V2
==================================================

The current SOA separates:

- Extension History
- Payment History

This can make the running balance confusing because an extension increases the loan balance but appears outside Payment History.

SOA V2 must instead display ONE chronological ledger called:

ACCOUNT ACTIVITY

Every transaction affecting the amount owed must appear in this table.

Examples:

- Loan Released
- Interest Added
- Extension Charge
- Payment
- Adjustment
- Credit
- Other ledger transaction types already supported by the system

Do not hard-code only these transaction types if the existing ledger supports additional types.

==================================================
4. ACCOUNT ACTIVITY TABLE
==================================================

Use this layout:

| Date | Transaction | Details / Remarks | Debit | Credit | Balance |

Definitions:

Date
= transaction_date

Transaction
= user-friendly display value of transaction_type

Details / Remarks
= remarks

Debit
= amount added to the customer's obligation

Credit
= amount reducing the customer's obligation

Balance
= running outstanding balance after the transaction

Example:

Date        Transaction        Details / Remarks                 Debit       Credit      Balance
Jul 02      Loan Released                                         100,000                  100,000
Jul 02      Interest Added                                          7,000                  107,000
Aug 04      Payment            Interest only                                    7,000     100,000
Sep 02      Payment            Interest only                                    7,000      93,000
Sep 04      Extension Charge   Additional interest Aug 2-Sep 2      7,000                  100,000
Sep 13      Payment                                                          1,000       99,000
Sep 15      Payment                                                          1,000       98,000

The purpose is that the reader can follow the balance from beginning to end without referring to another section.

==================================================
5. RUNNING BALANCE
==================================================

Calculate the running balance sequentially:

Running Balance =
Previous Running Balance
+ Debit
- Credit

Initial running balance = 0.

Pseudo logic:

decimal runningBalance = 0;

foreach (var transaction in transactions)
{
    runningBalance += transaction.Debit ?? 0;
    runningBalance -= transaction.Credit ?? 0;

    transaction.RunningBalance = runningBalance;
}

Do NOT derive running balance independently from Payment History.

The Account Activity ledger must be the source for the V2 running balance.

The final running balance should reconcile with the loan's current outstanding balance.

==================================================
6. TRANSACTION ORDER
==================================================

Transactions must be displayed chronologically:

ORDER BY transaction_date ASC, created_at ASC

IMPORTANT:
There may be multiple transactions on the same date.

Use the existing loan_ledger primary key, transaction ID, sequence, created timestamp, or other reliable ordering field as the secondary ordering.

For example:

ORDER BY
    transaction_date ASC,
    created_at ASC

Do NOT rely on database default ordering.

This is especially important when Loan Released and Interest Added occur on the same date.

==================================================
7. SUMMARY SECTION
==================================================

Keep the useful summary at the top of the SOA.

Recommended KPIs:

PRINCIPAL
INTEREST
EXTENSIONS
PAYMENTS
OUTSTANDING

Example:

Principal       ₱100,000.00
Interest          ₱7,000.00
Extensions        ₱7,000.00
Payments         ₱19,000.00
Outstanding      ₱95,000.00

The Outstanding value should be visually emphasized.

Also include the existing relevant:

Customer information
Loan information
Statement Date
Loan Status
Classification if currently displayed

Reuse the existing SOA information and styling where appropriate.

==================================================
8. TOTALS AT BOTTOM OF ACCOUNT ACTIVITY
==================================================

Add a totals row at the bottom:

TOTAL                         ₱114,000.00    ₱19,000.00    ₱95,000.00

Where:

Total Debit
= SUM(debit)

Total Credit
= SUM(credit)

Final Balance
= Total Debit - Total Credit

The final balance must match the final Running Balance.

==================================================
9. RECONCILIATION CHECK
==================================================

Add backend validation before generating the PDF.

Calculate:

Ledger Balance =
SUM(Debit) - SUM(Credit)

Compare it against the application's calculated Outstanding Balance for the loan.

Normally:

Ledger Balance == Outstanding Balance

Do NOT silently manipulate ledger values merely to force them to match.

If they do not reconcile:

- identify the discrepancy
- log a warning with Loan ID and amounts
- investigate how the existing application handles ledger entries

Do not alter production financial data as part of SOA generation.

If appropriate based on the existing architecture, prevent V2 generation when the discrepancy would make the statement materially incorrect and return a meaningful error.

Otherwise log/report the discrepancy according to the application's existing error-handling pattern.

==================================================
10. DETAILS / REMARKS COLUMN
==================================================

The Details / Remarks column is important and must remain visible.

Use:

loan_ledger.remarks

Examples:

Interest only
Additional interest for Aug 2 to Sep 2
30-day extension
Partial payment
Goodwill interest adjustment

Allow the Details column more width than Transaction.

Suggested relative widths:

Date                 12%
Transaction          18%
Details / Remarks    30%
Debit                13%
Credit               13%
Balance              14%

Adjust slightly if necessary for portrait PDF layout.

Long remarks should wrap rather than overlap other columns.

==================================================
11. DISPLAY FORMATTING
==================================================

Dates:

MMM dd, yyyy

Example:

Sep 04, 2026

Currency:

₱100,000.00

Use the application's existing currency formatting if currency can vary by loan.

For zero/null Debit or Credit values, display:

—

rather than:

₱0.00

Example:

Extension Charge:

Debit: ₱7,000.00
Credit: —

Payment:

Debit: —
Credit: ₱1,000.00

==================================================
12. TRANSACTION TYPE DISPLAY
==================================================

Do not expose ugly/internal database values if transaction_type contains technical codes.

Map them to clean labels.

Examples:

LOAN_RELEASE
→ Loan Released

INTEREST
→ Interest Added

EXTENSION
→ Extension Charge

PAYMENT
→ Payment

ADJUSTMENT
→ Adjustment

However, inspect the actual transaction types currently used by the application before creating mappings.

Do not invent mappings that conflict with existing business rules.

Create a centralized display mapping/helper rather than scattering string replacements throughout the PDF code.

==================================================
13. PDF LAYOUT
==================================================

Continue using the existing SOA portrait format unless there is a strong technical reason not to.

Target:

A4 Portrait

Keep the layout compact enough to support many transaction records.

Preferred structure:

--------------------------------------------------

Loan Management                         STATEMENT OF ACCOUNT V2
Borrower Statement of Account           LOA00020
                                        Statement Date: Sep 18, 2026

--------------------------------------------------

CUSTOMER                    LOAN DETAILS

Name                        Loan Date
Customer Code               Due Date
Contact                     Interest Rate
Address                     Status
                            Classification

--------------------------------------------------

PRINCIPAL   INTEREST   EXTENSIONS   PAYMENTS   OUTSTANDING

--------------------------------------------------

ACCOUNT ACTIVITY

Date | Transaction | Details / Remarks | Debit | Credit | Balance

...

--------------------------------------------------

TOTAL                         Debit Total | Credit Total | Balance

--------------------------------------------------

Notes

Authorized Signature

Generated by Loan Management System

--------------------------------------------------

Do not include separate Extension History and Payment History sections in V2.

All financial transactions belong in Account Activity.

==================================================
14. MULTIPLE PAGES
==================================================

Although most statements may fit on one page, V2 must support a large number of ledger transactions.

Do not assume a maximum number of rows.

If Account Activity exceeds one page:

- continue onto the next page
- repeat the table column headers
- avoid splitting a transaction row awkwardly
- preserve readable margins
- keep page numbers if supported by the existing PDF library

Example:

Page 1 of 2
Page 2 of 2

Do not reduce the font to an unreadable size just to force everything onto one page.

==================================================
15. STATEMENT DATE
==================================================

Use the application's configured Business Time Zone when determining/displaying Statement Date.

Do not use server-local timezone.

Follow the centralized timezone handling implemented by the application.

==================================================
16. BUTTON / UI
==================================================

On the Loan Details page, preserve:

Generate SOA

Add another option:

Generate SOA V2

If the UI would become cluttered, it is acceptable to change this into a dropdown:

Generate SOA ▼

Options:

- Current SOA
- SOA V2 - Account Activity

However, do not remove access to the current SOA.

Use the existing application's visual style.

==================================================
17. API / BACKEND
==================================================

Prefer creating a separate V2 endpoint/service method.

Example only:

GET /api/loans/{loanId}/statement-of-account-v2

or follow the existing API naming convention.

Do not rename or repurpose the current endpoint.

Suggested separation:

GenerateStatementOfAccount(...)
GenerateStatementOfAccountV2(...)

Reuse shared data retrieval/helpers where safe.

Avoid duplicating large amounts of existing SOA code if common functionality can be extracted without risking the existing implementation.

==================================================
18. DATA SOURCE
==================================================

SOA V2 Account Activity should primarily come from:

loan_ledger

Use:

transaction_date
transaction_type
debit
credit
remarks

plus the ledger primary key/sequence field for deterministic ordering.

Do NOT reconstruct the ledger by separately querying payments and extensions unless required to investigate missing ledger data.

The purpose of having loan_ledger is to provide the chronological financial history.

==================================================
19. TEST CASE USING CURRENT SOA SCENARIO
==================================================

Use the existing LOA00020 scenario as a reference.

Current loan information includes approximately:

Principal:          ₱100,000
Original Interest:    ₱7,000
Extension Charges:    ₱7,000
Payments:             ₱19,000
Outstanding:          ₱95,000

The current SOA contains an extension charge of ₱7,000 and seven payment records. The V2 ledger must make the extension's effect on the running balance obvious.

Expected conceptual flow:

Loan Released             +100,000    Balance 100,000
Interest Added              +7,000    Balance 107,000
Payment                     -7,000    Balance 100,000
Payment                     -7,000    Balance 93,000
Extension Charge            +7,000    Balance 100,000
Payment                     -1,000    Balance 99,000
Payment                     -1,000    Balance 98,000
Payment                     -1,000    Balance 97,000
Payment                     -1,000    Balance 96,000
Payment                     -1,000    Balance 95,000

Final:

Total Debits  = ₱114,000
Total Credits = ₱19,000
Balance       = ₱95,000

This demonstrates the reason for SOA V2.

==================================================
20. TESTING
==================================================

Add tests for at least:

1. Loan with no payments
2. Loan with one payment
3. Loan with many payments
4. Loan with one extension
5. Loan with multiple extensions
6. Loan with extension between payments
7. Fully paid loan
8. Overpaid loan if supported
9. Bad loan
10. Written-off loan if supported
11. Adjustment/credit transactions if supported
12. Multiple transactions occurring on the same date
13. Null/empty remarks
14. Long remarks that wrap in PDF
15. 60+ ledger transactions requiring PDF pagination

Validate for every test:

SUM(Debit) - SUM(Credit) = Final Running Balance

and where applicable:

Final Running Balance = Loan Outstanding Balance

==================================================
21. IMPORTANT SAFETY / PRODUCTION RULES
==================================================

This application is already in production.

Do NOT:

- alter existing loan balances
- regenerate ledger records
- modify historical ledger records
- migrate financial data unless explicitly required
- delete existing SOA code
- replace the current Generate SOA function
- change payment calculations
- change extension calculations
- change loan calculations

SOA V2 is initially a READ-ONLY reporting feature.

It should read existing financial data and generate the new statement.

If inconsistencies are found in loan_ledger, report them before attempting to fix production data.

==================================================
22. DELIVERABLES
==================================================

After implementation, provide:

1. Summary of existing SOA implementation reviewed
2. Summary of loan_ledger structure found
3. Files added
4. Files modified
5. New API endpoint
6. New Angular action/button
7. V2 PDF implementation
8. Running balance logic
9. Reconciliation validation
10. Tests added
11. Any data inconsistencies discovered
12. Confirmation that the original Generate SOA remains unchanged and functional

Before making major architectural changes, inspect and follow the existing project patterns.

Keep the implementation focused specifically on Statement of Account V2.