import { Injectable } from '@angular/core';
import { Observable, of, delay, switchMap } from 'rxjs';

import { CustomerListItem, LoanRepository, UpdateLoanFields } from '../../domain/repositories/loan.repository';
import { Customer, CustomerStatus } from '../../domain/entities/customer.entity';
import { CustomerDocument } from '../../domain/entities/customer-document.entity';
import { CustomerPayment } from '../../domain/entities/customer-payment.entity';
import { CustomerTotals } from '../../domain/entities/customer-totals.entity';
import { DashboardReceivables } from '../../domain/entities/dashboard-receivables.entity';
import { DailyCollection, DashboardSummary, MonthlyCollection } from '../../domain/entities/dashboard-summary.entity';
import { Loan, LoanClassification, LoanPageFilters, LoanStatus } from '../../domain/entities/loan.entity';
import { LoanAuditLogEntry } from '../../domain/entities/loan-audit-log-entry.entity';
import { LoanDocument } from '../../domain/entities/loan-document.entity';
import { LoanExtension } from '../../domain/entities/loan-extension.entity';
import { LoanLedgerEntry } from '../../domain/entities/loan-ledger-entry.entity';
import { LoanTotals } from '../../domain/entities/loan-totals.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';
import { Payment, PaymentMethod, PaymentPageFilters, PaymentWithCustomer, PaymentsTotals } from '../../domain/entities/payment.entity';

function addDays(dateStr: string, days: number): string {
  const d = new Date(dateStr);
  d.setDate(d.getDate() + days);
  return d.toISOString().slice(0, 10);
}

/**
 * Hand-built minimal single-page PDF (no library — this mock has no real
 * PDF generator, unlike the QuestPDF-backed HTTP repository) so the
 * "Generate SOA" button still produces something openable in offline/demo
 * mode. The xref table's byte offsets are approximate; every mainstream
 * PDF viewer (Chrome, Adobe Reader, Preview) falls back to scanning for
 * "obj" markers when xref is imprecise, so this still opens correctly.
 */
function buildSimplePdfBlob(lines: string[]): Blob {
  const escape = (s: string) => s.replace(/[\\()]/g, (c) => `\\${c}`);
  const textOps = lines.map((line, i) => `${i === 0 ? '' : '0 -16 Td '}(${escape(line)}) Tj`).join('\n');
  const content = `BT /F1 11 Tf 50 760 Td\n${textOps}\nET`;

  const objects = [
    '<</Type/Catalog/Pages 2 0 R>>',
    '<</Type/Pages/Kids[3 0 R]/Count 1>>',
    '<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 5 0 R>>>>/Contents 4 0 R>>',
    `<</Length ${content.length}>>stream\n${content}\nendstream`,
    '<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>',
  ];

  let pdf = '%PDF-1.4\n';
  const offsets: number[] = [];
  objects.forEach((obj, i) => {
    offsets.push(pdf.length);
    pdf += `${i + 1} 0 obj${obj}endobj\n`;
  });
  const xrefStart = pdf.length;
  pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  pdf += offsets.map((o) => `${String(o).padStart(10, '0')} 00000 n \n`).join('');
  pdf += `trailer<</Size ${objects.length + 1}/Root 1 0 R>>\nstartxref\n${xrefStart}\n%%EOF`;

  return new Blob([pdf], { type: 'application/pdf' });
}

const CUSTOMERS: Customer[] = [
  { customerId: 'C-001', customerCode: 'CUS00001', fullName: 'Maria Santos', address: 'Blk 4 Lot 2, Riverside', contactNumber: '+63 917 555 0142', borrowerType: 'Fish vendor', nicknameAlias: 'Ate Maria', notes: 'Long-time customer, always pays early.', status: 'active', createdAt: '2026-05-02' },
  { customerId: 'C-002', customerCode: 'CUS00002', fullName: 'Jun Dela Cruz', address: 'Purok 3, San Isidro', contactNumber: '+63 918 222 0987', borrowerType: 'Vegetable vendor', nicknameAlias: 'Kuya Jun', notes: '', status: 'active', createdAt: '2026-04-18' },
  { customerId: 'C-003', customerCode: 'CUS00003', fullName: 'Liza Ramos', address: '12 Mabini St.', contactNumber: '+63 920 111 4432', borrowerType: 'Vegetable vendor', nicknameAlias: '', notes: '', status: 'active', createdAt: '2026-06-01' },
  { customerId: 'C-004', customerCode: 'CUS00004', fullName: 'Ronnie Bautista', address: 'Sitio Bagong Buhay', contactNumber: '+63 917 888 1234', borrowerType: 'Fish vendor', nicknameAlias: '', notes: '', status: 'active', createdAt: '2026-03-22' },
  { customerId: 'C-005', customerCode: 'CUS00005', fullName: 'Ana Villanueva', address: '45 Rizal Ave.', contactNumber: '+63 919 333 5566', borrowerType: 'Community member', nicknameAlias: '', notes: '', status: 'active', createdAt: '2026-06-15' },
];

// Raw shape: [loanId, customerId, principal, interestRate, startDate, dueDate, totalPaid, status]
const RAW_LOANS: Array<[string, string, number, number, string, string, number, Loan['status']]> = [
  ['L-2034', 'C-001', 5000, 0.03, '2026-06-10', '2026-08-09', 2000, 'active'],
  ['L-2035', 'C-002', 3000, 0.03, '2026-05-01', '2026-07-15', 3090, 'paid'],
  ['L-2036', 'C-003', 2000, 0.03, '2026-06-20', '2026-08-19', 0, 'active'],
  ['L-2037', 'C-004', 4000, 0.03, '2026-05-20', '2026-07-19', 1000, 'overdue'],
  ['L-2038', 'C-005', 1000, 0.03, '2026-06-25', '2026-08-24', 400, 'active'],
  ['L-2039', 'C-001', 3500, 0.03, '2026-04-15', '2026-06-29', 500, 'extended'],
];

function buildLoan(raw: (typeof RAW_LOANS)[number], index: number): Loan {
  const [loanId, customerId, principalAmount, interestRate, startDate, dueDate, totalPaid, status] = raw;
  const totalInterest = Math.round(principalAmount * interestRate);
  const totalExtensionCharges = 0;
  const totalAmountDue = principalAmount + totalInterest + totalExtensionCharges;
  const customer = CUSTOMERS.find((c) => c.customerId === customerId);
  const paymentTermsMonths = 2;
  return {
    loanId,
    loanNumber: `LOA${String(index + 1).padStart(5, '0')}`,
    customerId,
    customerName: customer?.fullName ?? 'Unknown',
    principalAmount,
    interestRate,
    startDate,
    dueDate,
    paymentTermsMonths,
    totalInterest,
    totalExtensionCharges,
    totalAmountDue,
    totalPaid,
    balance: Math.max(totalAmountDue - totalPaid, 0),
    dailyPayment: Math.round(((principalAmount + totalInterest) / (paymentTermsMonths * 30)) * 100) / 100,
    status,
    classification: 'normal',
    remarks: '',
    createdAt: startDate,
  };
}

const EXTENSIONS: Record<string, LoanExtension[]> = {
  'L-2039': [
    {
      extensionId: 'EXT-0091',
      loanId: 'L-2039',
      extensionDate: '2026-06-29',
      extensionDays: 30,
      additionalChargesAmount: 105,
      remarks: 'Customer requested extra month, business slow this week',
    },
  ],
};

const PAYMENTS: Record<string, Payment[]> = {
  'L-2034': [
    { paymentId: 'P-5001', loanId: 'L-2034', paymentDate: '2026-07-10', amountPaid: 1000, paymentMethod: 'cash', notes: '', createdAt: '2026-07-10T00:00:00.000Z' },
    { paymentId: 'P-5002', loanId: 'L-2034', paymentDate: '2026-07-25', amountPaid: 1000, paymentMethod: 'gcash', notes: '', createdAt: '2026-07-25T00:00:00.000Z' },
  ],
  'L-2035': [
    { paymentId: 'P-5010', loanId: 'L-2035', paymentDate: '2026-07-01', amountPaid: 1545, paymentMethod: 'cash', notes: '', createdAt: '2026-07-01T00:00:00.000Z' },
    { paymentId: 'P-5011', loanId: 'L-2035', paymentDate: '2026-07-14', amountPaid: 1545, paymentMethod: 'cash', notes: 'Full settlement', createdAt: '2026-07-14T00:00:00.000Z' },
  ],
  'L-2037': [
    { paymentId: 'P-5020', loanId: 'L-2037', paymentDate: '2026-06-05', amountPaid: 1000, paymentMethod: 'cash', notes: '', createdAt: '2026-06-05T00:00:00.000Z' },
  ],
  'L-2038': [
    { paymentId: 'P-5030', loanId: 'L-2038', paymentDate: '2026-07-20', amountPaid: 400, paymentMethod: 'gcash', notes: '', createdAt: '2026-07-20T00:00:00.000Z' },
  ],
  'L-2039': [
    { paymentId: 'P-5040', loanId: 'L-2039', paymentDate: '2026-05-30', amountPaid: 500, paymentMethod: 'cash', notes: 'Partial payment before extension', createdAt: '2026-05-30T00:00:00.000Z' },
  ],
};

/**
 * Stand-in data source. Implements LoanRepository so it can be swapped for
 * an HTTP-backed repository (see http-loan.repository.example.ts) without
 * touching use cases or components.
 */
@Injectable()
export class MockLoanRepository extends LoanRepository {
  private loans: Loan[] = RAW_LOANS.map((raw, index) => buildLoan(raw, index));
  private extensions = EXTENSIONS;
  private payments = PAYMENTS;

  // Documents keep both the metadata (returned to callers) and the actual
  // File/Blob (so downloadXDocument can hand back real bytes) — a real
  // backend keeps these together in one VARBINARY(MAX) row; the mock keeps
  // them in two parallel maps keyed by documentId for simplicity.
  private customerDocuments: Record<string, CustomerDocument[]> = {};
  private customerDocumentBlobs: Record<string, Blob> = {};
  private loanDocuments: Record<string, LoanDocument[]> = {};
  private loanDocumentBlobs: Record<string, Blob> = {};

  /** Appended to by updateLoan/changeLoanClassification — see getLoanAuditLog. */
  private auditLog: Record<string, LoanAuditLogEntry[]> = {};

  getCustomers(): Observable<Customer[]> {
    return of(CUSTOMERS).pipe(delay(150));
  }

  getCustomersPage(
    pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir: 'asc' | 'desc' = 'asc',
    status?: CustomerStatus,
  ): Observable<PagedResult<CustomerListItem>> {
    const filtered = this.filterCustomers(search, status);

    const dir = sortDir === 'desc' ? -1 : 1;
    const key: 'fullName' | 'borrowerType' = sortBy === 'borrowerType' ? 'borrowerType' : 'fullName';
    const sorted = [...filtered].sort((a, b) => dir * a[key].localeCompare(b[key]));

    const totalCount = sorted.length;
    const items: CustomerListItem[] = sorted
      .slice(pageIndex * pageSize, pageIndex * pageSize + pageSize)
      .map((c) => this.toListItem(c));

    return of({ items, totalCount }).pipe(delay(150));
  }

  getCustomersTotals(search: string, status?: CustomerStatus): Observable<CustomerTotals> {
    const filtered = this.filterCustomers(search, status);
    const items = filtered.map((c) => this.toListItem(c));

    return of({
      totalCustomersCount: filtered.length,
      activeCustomersCount: filtered.filter((c) => c.status === 'active').length,
      inactiveCustomersCount: filtered.filter((c) => c.status === 'inactive').length,
      totalLoansCount: items.reduce((sum, c) => sum + c.loanCount, 0),
      totalOutstandingBalance: items.reduce((sum, c) => sum + c.outstandingBalance, 0),
    }).pipe(delay(150));
  }

  /**
   * Plain CSV text, not a real .xlsx — same offline-mock trade-off as
   * LoanRepository.exportLoans (see that method's comment).
   */
  exportCustomers(search: string, status?: CustomerStatus): Observable<Blob> {
    const filtered = this.filterCustomers(search, status).map((c) => this.toListItem(c));
    const header = 'Code,Name,Contact,Borrower Type,Status,Total Loans,Outstanding Balance,Date Added';
    const rows = filtered.map((c) =>
      [c.customerCode, c.fullName, c.contactNumber, c.borrowerType, c.status, c.loanCount, c.outstandingBalance, c.createdAt].join(','),
    );
    return of(new Blob([[header, ...rows].join('\n')], { type: 'text/csv' })).pipe(delay(150));
  }

  private filterCustomers(search: string, status?: CustomerStatus): Customer[] {
    const term = search.trim().toLowerCase();
    return CUSTOMERS.filter(
      (c) =>
        (!term || c.fullName.toLowerCase().includes(term) || c.contactNumber.toLowerCase().includes(term)) &&
        (!status || c.status === status),
    );
  }

  private toListItem(c: Customer): CustomerListItem {
    const customerLoans = this.loans.filter((l) => l.customerId === c.customerId);
    return { ...c, loanCount: customerLoans.length, outstandingBalance: customerLoans.reduce((sum, l) => sum + l.balance, 0) };
  }

  getCustomerById(customerId: string): Observable<Customer | undefined> {
    return of(CUSTOMERS.find((c) => c.customerId === customerId)).pipe(delay(150));
  }

  createCustomer(
    fullName: string, address: string, contactNumber: string, borrowerType: string,
    nicknameAlias = '', notes = '',
  ): Observable<Customer> {
    const customer: Customer = {
      customerId: `C-${Math.floor(Math.random() * 9000 + 1000)}`,
      customerCode: `CUS${String(CUSTOMERS.length + 1).padStart(5, '0')}`,
      fullName,
      address,
      contactNumber,
      borrowerType,
      nicknameAlias,
      notes,
      status: 'active',
      createdAt: new Date().toISOString().slice(0, 10),
    };
    CUSTOMERS.push(customer);
    return of(customer).pipe(delay(150));
  }

  updateCustomer(
    customerId: string, fullName: string, address: string, contactNumber: string, borrowerType: string,
    nicknameAlias = '', notes = '',
  ): Observable<Customer> {
    const customer = CUSTOMERS.find((c) => c.customerId === customerId);
    if (!customer) throw new Error(`Customer ${customerId} not found`);

    customer.fullName = fullName;
    customer.address = address;
    customer.contactNumber = contactNumber;
    customer.borrowerType = borrowerType;
    customer.nicknameAlias = nicknameAlias;
    customer.notes = notes;

    return of(customer).pipe(delay(150));
  }

  getCustomerDocuments(customerId: string): Observable<CustomerDocument[]> {
    return of(this.customerDocuments[customerId] ?? []).pipe(delay(150));
  }

  uploadCustomerDocument(customerId: string, file: File): Observable<CustomerDocument> {
    const documentId = `CDOC-${Math.floor(Math.random() * 900000 + 100000)}`;
    const document: CustomerDocument = {
      documentId,
      customerId,
      originalFileName: file.name,
      contentType: file.type,
      fileSizeBytes: file.size,
      uploadedAt: new Date().toISOString(),
      uploadedBy: 'admin',
    };
    this.customerDocuments[customerId] = [...(this.customerDocuments[customerId] ?? []), document];
    this.customerDocumentBlobs[documentId] = file;
    return of(document).pipe(delay(150));
  }

  downloadCustomerDocument(customerId: string, documentId: string): Observable<Blob> {
    const blob = this.customerDocumentBlobs[documentId];
    if (!blob) throw new Error(`Document ${documentId} not found`);
    return of(blob).pipe(delay(150));
  }

  deleteCustomerDocument(customerId: string, documentId: string): Observable<void> {
    this.customerDocuments[customerId] = (this.customerDocuments[customerId] ?? []).filter((d) => d.documentId !== documentId);
    delete this.customerDocumentBlobs[documentId];
    return of(undefined).pipe(delay(150));
  }

  getLoans(): Observable<Loan[]> {
    return of(this.loans).pipe(delay(150));
  }

  getLoansPage(
    pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir: 'asc' | 'desc' = 'asc',
    filters?: LoanPageFilters,
  ): Observable<PagedResult<Loan>> {
    const filtered = this.applyFilters(search, filters);

    const dir = sortDir === 'desc' ? -1 : 1;
    const sorted = [...filtered].sort((a, b) => {
      switch (sortBy) {
        case 'principalAmount':
          return dir * (a.principalAmount - b.principalAmount);
        case 'dueDate':
          return dir * a.dueDate.localeCompare(b.dueDate);
        case 'balance':
          return dir * (a.balance - b.balance);
        case 'status':
          return dir * a.status.localeCompare(b.status);
        case 'loanNumber':
          return dir * a.loanNumber.localeCompare(b.loanNumber);
        default:
          return b.createdAt.localeCompare(a.createdAt);
      }
    });

    const totalCount = sorted.length;
    const items = sorted.slice(pageIndex * pageSize, pageIndex * pageSize + pageSize);

    return of({ items, totalCount }).pipe(delay(150));
  }

  getLoansTotals(search: string, filters?: LoanPageFilters): Observable<LoanTotals> {
    const filtered = this.applyFilters(search, filters);

    return of({
      totalPrincipal: filtered.reduce((sum, l) => sum + l.principalAmount, 0),
      totalInterest: filtered.reduce((sum, l) => sum + l.totalInterest, 0),
      totalExtensionCharges: filtered.reduce((sum, l) => sum + l.totalExtensionCharges, 0),
      totalPayments: filtered.reduce((sum, l) => sum + l.totalPaid, 0),
      totalOutstandingBalance: filtered.reduce((sum, l) => sum + l.balance, 0),
      totalLoansCount: filtered.length,
      activeLoansCount: filtered.filter((l) => l.status === 'active' || l.status === 'extended').length,
      overdueLoansCount: filtered.filter((l) => l.status === 'overdue').length,
      paidLoansCount: filtered.filter((l) => l.status === 'paid').length,
    }).pipe(delay(150));
  }

  /**
   * Plain CSV text, not a real .xlsx (no zip/OOXML library available to
   * this offline mock) — same spirit as buildSimplePdfBlob's hand-rolled
   * minimal PDF above: good enough for the mock/demo path, since the
   * registered repository in app.config.ts is HttpLoanRepository.
   */
  exportLoans(search: string, filters?: LoanPageFilters): Observable<Blob> {
    const filtered = this.applyFilters(search, filters);
    const header = 'Loan #,Customer,Loan Date,Due Date,Principal,Interest,Ext. Charges,Payments,Balance,Status,Classification';
    const rows = filtered.map((l) =>
      [l.loanNumber, l.customerName, l.startDate, l.dueDate, l.principalAmount, l.totalInterest, l.totalExtensionCharges, l.totalPaid, l.balance, l.status, l.classification].join(','),
    );
    return of(new Blob([[header, ...rows].join('\n')], { type: 'text/csv' })).pipe(delay(150));
  }

  getDashboardReceivables(): Observable<DashboardReceivables> {
    const today = new Date().toISOString().slice(0, 10);
    const weekFromNow = addDays(today, 7);
    const isEffectivelyOverdue = (l: Loan) => l.dueDate < today && l.status !== 'paid' && l.status !== 'writtenoff';

    const grossReceivables = this.loans.filter((l) => l.status !== 'writtenoff').reduce((sum, l) => sum + l.balance, 0);
    const badLoanReceivables = this.loans.filter((l) => l.classification === 'badloan').reduce((sum, l) => sum + l.balance, 0);

    return of({
      grossReceivables,
      collectibleReceivables: grossReceivables - badLoanReceivables,
      badLoanReceivables,
      activeLoansCount: this.loans.filter((l) => (l.status === 'active' || l.status === 'extended') && !isEffectivelyOverdue(l)).length,
      overdueLoansCount: this.loans.filter((l) => l.status === 'overdue' || isEffectivelyOverdue(l)).length,
      writtenOffLoansCount: this.loans.filter((l) => l.status === 'writtenoff').length,
      loansDueThisWeekCount: this.loans.filter(
        (l) => l.status !== 'paid' && l.status !== 'writtenoff' && l.dueDate >= today && l.dueDate <= weekFromNow,
      ).length,
    }).pipe(delay(150));
  }

  getDashboardSummary(): Observable<DashboardSummary> {
    const today = new Date();
    const allPayments = Object.values(this.payments).flat();

    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const monthlyCollections: MonthlyCollection[] = monthNames.map((month, i) => ({
      month,
      thisYear: allPayments.filter((p) => {
        const d = new Date(p.paymentDate);
        return d.getFullYear() === today.getFullYear() && d.getMonth() === i;
      }).reduce((sum, p) => sum + p.amountPaid, 0),
      lastYear: 0, // mock data doesn't carry a prior year's worth of payments
    }));

    const last7DaysCollections: DailyCollection[] = Array.from({ length: 7 }, (_, i) => {
      const date = new Date(today);
      date.setDate(date.getDate() - (6 - i));
      const iso = date.toISOString().slice(0, 10);
      return { date: iso, amount: allPayments.filter((p) => p.paymentDate === iso).reduce((sum, p) => sum + p.amountPaid, 0) };
    });

    const active = this.loans.filter((l) => (l.status === 'active' || l.status === 'extended') && l.classification !== 'badloan');
    const overdue = this.loans.filter((l) => l.status === 'overdue' && l.classification !== 'badloan');
    const badLoan = this.loans.filter((l) => l.classification === 'badloan' && l.status !== 'writtenoff');
    const paid = this.loans.filter((l) => l.status === 'paid');

    const recentLoans = [...this.loans].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 5);

    return of({
      grossReceivables: this.loans.filter((l) => l.status !== 'writtenoff').reduce((sum, l) => sum + l.balance, 0),
      grossReceivablesChangePercent: null,
      collectibleReceivables: this.loans.filter((l) => l.status !== 'writtenoff').reduce((sum, l) => sum + l.balance, 0),
      collectibleReceivablesChangePercent: null,
      badLoanReceivables: this.loans.filter((l) => l.classification === 'badloan').reduce((sum, l) => sum + l.balance, 0),
      badLoanReceivablesChangePercent: null,
      activeLoansCount: this.loans.filter((l) => l.status === 'active' || l.status === 'extended').length,
      activeLoansChangePercent: null,
      overdueLoansCount: this.loans.filter((l) => l.status === 'overdue').length,
      overdueLoansChangePercent: null,
      loansDueThisWeekCount: 0,
      monthlyCollections,
      last7DaysCollections,
      receivablesBreakdown: {
        current: active.reduce((sum, l) => sum + l.totalAmountDue, 0),
        overdue: overdue.reduce((sum, l) => sum + l.totalAmountDue, 0),
        badLoan: badLoan.reduce((sum, l) => sum + l.totalAmountDue, 0),
        paid: paid.reduce((sum, l) => sum + l.totalAmountDue, 0),
      },
      recentLoans,
    }).pipe(delay(150));
  }

  getCustomerReceivables(customerId: string): Observable<DashboardReceivables> {
    const today = new Date().toISOString().slice(0, 10);
    const weekFromNow = addDays(today, 7);
    const isEffectivelyOverdue = (l: Loan) => l.dueDate < today && l.status !== 'paid' && l.status !== 'writtenoff';

    const customerLoans = this.loans.filter((l) => l.customerId === customerId);
    const grossReceivables = customerLoans.filter((l) => l.status !== 'writtenoff').reduce((sum, l) => sum + l.balance, 0);
    const badLoanReceivables = customerLoans.filter((l) => l.classification === 'badloan').reduce((sum, l) => sum + l.balance, 0);

    return of({
      grossReceivables,
      collectibleReceivables: grossReceivables - badLoanReceivables,
      badLoanReceivables,
      activeLoansCount: customerLoans.filter((l) => (l.status === 'active' || l.status === 'extended') && !isEffectivelyOverdue(l)).length,
      overdueLoansCount: customerLoans.filter((l) => l.status === 'overdue' || isEffectivelyOverdue(l)).length,
      writtenOffLoansCount: customerLoans.filter((l) => l.status === 'writtenoff').length,
      loansDueThisWeekCount: customerLoans.filter(
        (l) => l.status !== 'paid' && l.status !== 'writtenoff' && l.dueDate >= today && l.dueDate <= weekFromNow,
      ).length,
    }).pipe(delay(150));
  }

  private applyFilters(search: string, filters?: LoanPageFilters): Loan[] {
    const term = search.trim().toLowerCase();
    const today = new Date().toISOString().slice(0, 10);
    const isEffectivelyOverdue = (l: Loan) => l.dueDate < today && l.status !== 'paid' && l.status !== 'writtenoff';

    return this.loans.filter((l) => {
      if (term && !l.loanNumber.toLowerCase().includes(term) && !l.customerName.toLowerCase().includes(term) && !l.status.toLowerCase().includes(term)) {
        return false;
      }
      if (filters?.statuses?.length) {
        const wantsOverdue = filters.statuses.includes('overdue');
        const otherStatuses: LoanStatus[] = filters.statuses.filter((s) => s !== 'overdue');
        const matchesOverdue = wantsOverdue && isEffectivelyOverdue(l);
        const matchesOther = otherStatuses.includes(l.status) && !isEffectivelyOverdue(l);
        if (!matchesOverdue && !matchesOther) return false;
      }
      if (filters?.overdueOnly && !isEffectivelyOverdue(l)) return false;
      if (filters?.classifications?.length && !filters.classifications.includes(l.classification)) return false;
      if (filters?.badLoansOnly && l.classification !== 'badloan') return false;
      if (filters?.loanDateFrom && l.startDate < filters.loanDateFrom) return false;
      if (filters?.loanDateTo && l.startDate > filters.loanDateTo) return false;
      if (filters?.dueDateFrom && l.dueDate < filters.dueDateFrom) return false;
      if (filters?.dueDateTo && l.dueDate > filters.dueDateTo) return false;
      return true;
    });
  }

  getLoanById(loanId: string): Observable<Loan | undefined> {
    return of(this.loans.find((l) => l.loanId === loanId)).pipe(delay(150));
  }

  getLoansByCustomer(customerId: string): Observable<Loan[]> {
    return of(this.loans.filter((l) => l.customerId === customerId)).pipe(delay(150));
  }

  getCustomerPaymentsPage(
    customerId: string, pageIndex: number, pageSize: number, sortBy?: string, sortDir?: 'asc' | 'desc',
  ): Observable<PagedResult<CustomerPayment>> {
    const loanNumbers = new Map(this.loans.filter((l) => l.customerId === customerId).map((l) => [l.loanId, l.loanNumber]));
    const dir = sortDir === 'asc' ? 1 : -1;
    const all = Object.values(this.payments)
      .flat()
      .filter((p) => loanNumbers.has(p.loanId))
      .map((p): CustomerPayment => ({ ...p, loanNumber: loanNumbers.get(p.loanId)! }))
      .sort((a, b) => {
        if (sortBy === 'amount') return (a.amountPaid - b.amountPaid) * dir;
        if (sortBy === 'loan') return a.loanNumber.localeCompare(b.loanNumber) * dir;
        return (new Date(a.paymentDate).getTime() - new Date(b.paymentDate).getTime()) * (sortBy === 'date' ? dir : -1);
      });

    const start = pageIndex * pageSize;
    return of({ items: all.slice(start, start + pageSize), totalCount: all.length }).pipe(delay(150));
  }

  createLoan(
    customerId: string, principal: number, interestRate = 0.07, paymentTermsMonths = 2,
    startDate?: string, interestAmount?: number,
  ): Observable<Loan> {
    const customer = CUSTOMERS.find((c) => c.customerId === customerId);
    if (!customer) throw new Error(`Customer ${customerId} not found`);

    const start = startDate ?? new Date().toISOString().slice(0, 10);
    const totalInterest = interestAmount ?? Math.round(principal * interestRate * paymentTermsMonths * 100) / 100;
    const totalAmountDue = principal + totalInterest;

    const loan: Loan = {
      loanId: `L-${Math.floor(Math.random() * 9000 + 1000)}`,
      loanNumber: `LOA${String(this.loans.length + 1).padStart(5, '0')}`,
      customerId,
      customerName: customer.fullName,
      principalAmount: principal,
      interestRate,
      startDate: start,
      dueDate: addDays(start, paymentTermsMonths * 30),
      paymentTermsMonths,
      totalInterest,
      totalExtensionCharges: 0,
      totalAmountDue,
      totalPaid: 0,
      balance: totalAmountDue,
      dailyPayment: Math.round((totalAmountDue / (paymentTermsMonths * 30)) * 100) / 100,
      status: 'active',
      classification: 'normal',
      remarks: '',
      createdAt: start,
    };
    this.loans.push(loan);
    return of(loan).pipe(delay(150));
  }

  updateLoan(loanId: string, fields: UpdateLoanFields): Observable<Loan> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);

    if (fields.principal !== undefined) loan.principalAmount = fields.principal;
    if (fields.startDate !== undefined) loan.startDate = fields.startDate;
    if (fields.dueDate !== undefined) loan.dueDate = fields.dueDate;
    if (fields.interestRate !== undefined) loan.interestRate = fields.interestRate;
    if (fields.interestAmount !== undefined) loan.totalInterest = fields.interestAmount;
    if (fields.paymentTermsMonths !== undefined) loan.paymentTermsMonths = fields.paymentTermsMonths;
    if (fields.remarks !== undefined) loan.remarks = fields.remarks;

    loan.totalAmountDue = loan.principalAmount + loan.totalInterest + loan.totalExtensionCharges;
    loan.balance = Math.max(loan.totalAmountDue - loan.totalPaid, 0);
    loan.dailyPayment = Math.round((loan.totalAmountDue / (loan.paymentTermsMonths * 30)) * 100) / 100;

    this.addAuditEntry(loanId, 'edited', 'Loan details edited.');
    return of(loan).pipe(delay(150));
  }

  changeLoanClassification(loanId: string, classification: LoanClassification): Observable<Loan> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);

    if (loan.classification !== classification) {
      this.addAuditEntry(loanId, 'classification_changed', `Classification changed from ${loan.classification} to ${classification}.`);
    }
    loan.classification = classification;
    return of(loan).pipe(delay(150));
  }

  private addAuditEntry(loanId: string, action: LoanAuditLogEntry['action'], description: string): void {
    const entry: LoanAuditLogEntry = {
      auditLogId: `AUD-${Math.floor(Math.random() * 900000 + 100000)}`,
      loanId,
      action,
      description,
      performedBy: 'admin',
      occurredAt: new Date().toISOString(),
    };
    this.auditLog[loanId] = [...(this.auditLog[loanId] ?? []), entry];
  }

  /**
   * Recomputed on every call from the loan's current Extensions/Payments —
   * simpler than maintaining a parallel ledger array kept in sync with
   * every mutation method above, and the mock only needs to look
   * consistent with whatever state those methods have already produced.
   */
  private buildLedger(loanId: string): LoanLedgerEntry[] {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) return [];

    const entries: LoanLedgerEntry[] = [];
    let runningBalance = 0;
    let seq = 0;
    const nextId = () => `LEDG-${loanId}-${seq++}`;

    runningBalance += loan.principalAmount;
    entries.push({
      ledgerId: nextId(), loanId, transactionDate: loan.startDate, transactionType: 'loan_released',
      debit: loan.principalAmount, credit: 0, runningBalance, remarks: 'Loan released', createdAt: loan.startDate,
    });

    runningBalance += loan.totalInterest;
    entries.push({
      ledgerId: nextId(), loanId, transactionDate: loan.startDate, transactionType: 'interest_added',
      debit: loan.totalInterest, credit: 0, runningBalance, remarks: 'Interest added', createdAt: loan.startDate,
    });

    const movements = [
      ...(this.extensions[loanId] ?? []).map((e) => ({
        date: e.extensionDate, isPayment: false, amount: e.additionalChargesAmount,
        referenceId: e.extensionId, remarks: `Extension +${e.extensionDays} days`,
      })),
      ...(this.payments[loanId] ?? []).map((p) => ({
        date: p.paymentDate, isPayment: true, amount: p.amountPaid, referenceId: p.paymentId, remarks: 'Payment received',
      })),
    ].sort((a, b) => a.date.localeCompare(b.date));

    for (const m of movements) {
      runningBalance += m.isPayment ? -m.amount : m.amount;
      entries.push({
        ledgerId: nextId(), loanId, transactionDate: m.date,
        transactionType: m.isPayment ? 'payment' : 'extension',
        debit: m.isPayment ? 0 : m.amount, credit: m.isPayment ? m.amount : 0,
        runningBalance, remarks: m.remarks, createdAt: m.date, referenceId: m.referenceId,
      });
    }

    return entries;
  }

  getExtensions(loanId: string): Observable<LoanExtension[]> {
    return of(this.extensions[loanId] ?? []).pipe(delay(150));
  }

  extendLoan(loanId: string, extensionDays: number, remarks: string, additionalChargesAmount = 0): Observable<Loan> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);

    const extension: LoanExtension = {
      extensionId: `EXT-${Math.floor(Math.random() * 9000 + 1000)}`,
      loanId,
      extensionDate: new Date().toISOString().slice(0, 10),
      extensionDays,
      additionalChargesAmount,
      remarks,
    };
    this.extensions[loanId] = [...(this.extensions[loanId] ?? []), extension];

    loan.dueDate = addDays(loan.dueDate, extensionDays);
    loan.totalExtensionCharges += additionalChargesAmount;
    loan.totalAmountDue += additionalChargesAmount;
    loan.balance = Math.max(loan.totalAmountDue - loan.totalPaid, 0);
    loan.status = 'extended';

    return of(loan).pipe(delay(150));
  }

  updateExtension(
    loanId: string, extensionId: string, extensionDays: number,
    remarks: string, additionalChargesAmount = 0,
  ): Observable<LoanExtension> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);
    const extension = (this.extensions[loanId] ?? []).find((e) => e.extensionId === extensionId);
    if (!extension) throw new Error(`Extension ${extensionId} not found`);

    // Roll back this extension's old contribution before applying the new one.
    loan.dueDate = addDays(addDays(loan.dueDate, -extension.extensionDays), extensionDays);
    loan.totalExtensionCharges = loan.totalExtensionCharges - extension.additionalChargesAmount + additionalChargesAmount;
    loan.totalAmountDue = loan.principalAmount + loan.totalInterest + loan.totalExtensionCharges;
    loan.balance = Math.max(loan.totalAmountDue - loan.totalPaid, 0);

    extension.extensionDays = extensionDays;
    extension.additionalChargesAmount = additionalChargesAmount;
    extension.remarks = remarks;

    return of(extension).pipe(delay(150));
  }

  deleteExtension(loanId: string, extensionId: string): Observable<Loan> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);
    const extension = (this.extensions[loanId] ?? []).find((e) => e.extensionId === extensionId);
    if (!extension) throw new Error(`Extension ${extensionId} not found`);

    this.extensions[loanId] = (this.extensions[loanId] ?? []).filter((e) => e.extensionId !== extensionId);
    loan.dueDate = addDays(loan.dueDate, -extension.extensionDays);
    loan.totalExtensionCharges -= extension.additionalChargesAmount;
    loan.totalAmountDue = loan.principalAmount + loan.totalInterest + loan.totalExtensionCharges;
    loan.balance = Math.max(loan.totalAmountDue - loan.totalPaid, 0);

    return of(loan).pipe(delay(150));
  }

  getPayments(loanId: string): Observable<Payment[]> {
    return of(this.payments[loanId] ?? []).pipe(delay(150));
  }

  getPaymentsPage(
    loanId: string, pageIndex: number, pageSize: number, sortBy?: string, sortDir: 'asc' | 'desc' = 'desc',
  ): Observable<PagedResult<Payment>> {
    const dir = sortDir === 'asc' ? 1 : -1;
    const all = [...(this.payments[loanId] ?? [])].sort(
      (a, b) => (new Date(a.paymentDate).getTime() - new Date(b.paymentDate).getTime()) * dir,
    );
    const start = pageIndex * pageSize;
    return of({ items: all.slice(start, start + pageSize), totalCount: all.length }).pipe(delay(150));
  }

  getRecentPayments(limit: number): Observable<PaymentWithCustomer[]> {
    const all = this.allPaymentsWithCustomer().sort((a, b) => new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime());
    return of(all.slice(0, limit)).pipe(delay(150));
  }

  private allPaymentsWithCustomer(): PaymentWithCustomer[] {
    return Object.values(this.payments)
      .flat()
      .map((p) => {
        const loan = this.loans.find((l) => l.loanId === p.loanId);
        return {
          ...p,
          customerId: loan?.customerId ?? '',
          customerName: loan?.customerName ?? 'Unknown',
          loanNumber: loan?.loanNumber ?? '—',
        };
      });
  }

  getPaymentsListPage(
    pageIndex: number, pageSize: number, sortBy?: string, sortDir?: 'asc' | 'desc', filters?: PaymentPageFilters,
  ): Observable<PagedResult<PaymentWithCustomer>> {
    let items = this.allPaymentsWithCustomer();

    if (filters?.loanSearch) {
      const digits = filters.loanSearch.replace(/\D/g, '');
      items = digits ? items.filter((p) => p.loanNumber.includes(digits)) : [];
    }
    if (filters?.customerSearch) {
      const term = filters.customerSearch.toLowerCase();
      items = items.filter((p) => p.customerName.toLowerCase().includes(term));
    }
    if (filters?.dateFrom) items = items.filter((p) => p.paymentDate >= filters.dateFrom!);
    if (filters?.dateTo) items = items.filter((p) => p.paymentDate <= filters.dateTo!);

    const desc = sortDir === 'desc';
    const sorters: Record<string, (a: PaymentWithCustomer, b: PaymentWithCustomer) => number> = {
      loan: (a, b) => a.loanNumber.localeCompare(b.loanNumber),
      customer: (a, b) => a.customerName.localeCompare(b.customerName),
      amount: (a, b) => a.amountPaid - b.amountPaid,
      date: (a, b) => new Date(a.paymentDate).getTime() - new Date(b.paymentDate).getTime(),
    };
    const sorter = sortBy ? sorters[sortBy] : undefined;
    items = sorter
      ? [...items].sort((a, b) => (desc ? -sorter(a, b) : sorter(a, b)))
      : [...items].sort((a, b) => new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime());

    const totalCount = items.length;
    const page = items.slice(pageIndex * pageSize, pageIndex * pageSize + pageSize);
    return of({ items: page, totalCount }).pipe(delay(150));
  }

  getPaymentsListTotals(filters?: PaymentPageFilters): Observable<PaymentsTotals> {
    return this.getPaymentsListPage(0, Number.MAX_SAFE_INTEGER, undefined, undefined, filters).pipe(
      switchMap((result) =>
        of({
          totalAmountPaid: result.items.reduce((sum, p) => sum + p.amountPaid, 0),
          count: result.items.length,
        }),
      ),
    );
  }

  exportPaymentsList(filters?: PaymentPageFilters): Observable<Blob> {
    return of(new Blob(['mock export'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })).pipe(delay(150));
  }

  recordPayment(loanId: string, amountPaid: number, paymentMethod: PaymentMethod, notes: string, referenceNumber?: string): Observable<Payment> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);

    const payment: Payment = {
      paymentId: `P-${Math.floor(Math.random() * 9000 + 1000)}`,
      loanId,
      paymentDate: new Date().toISOString().slice(0, 10),
      amountPaid,
      paymentMethod,
      notes,
      referenceNumber,
      createdAt: new Date().toISOString(),
    };
    this.payments[loanId] = [...(this.payments[loanId] ?? []), payment];

    loan.totalPaid += amountPaid;
    loan.balance = Math.max(loan.totalAmountDue - loan.totalPaid, 0);
    if (loan.balance === 0) loan.status = 'paid';

    // Per the SRS: "Each payment automatically creates a payment_received
    // entry in the Cash_Ledger table." In a real system this would go
    // through a shared transactional boundary (e.g. a domain event or an
    // application-service call into CashLedgerRepository) rather than this
    // repository reaching directly into another one.

    return of(payment).pipe(delay(150));
  }

  updatePayment(
    loanId: string, paymentId: string, amountPaid: number, paymentMethod: PaymentMethod,
    notes?: string, referenceNumber?: string,
  ): Observable<Payment> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);
    const payment = (this.payments[loanId] ?? []).find((p) => p.paymentId === paymentId);
    if (!payment) throw new Error(`Payment ${paymentId} not found`);

    loan.totalPaid = loan.totalPaid - payment.amountPaid + amountPaid;
    loan.balance = Math.max(loan.totalAmountDue - loan.totalPaid, 0);
    if (loan.balance === 0) loan.status = 'paid';
    else if (loan.status === 'paid') loan.status = 'active';

    payment.amountPaid = amountPaid;
    payment.paymentMethod = paymentMethod;
    payment.notes = notes ?? '';
    payment.referenceNumber = referenceNumber;

    return of(payment).pipe(delay(150));
  }

  deletePayment(loanId: string, paymentId: string): Observable<Loan> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);
    const payment = (this.payments[loanId] ?? []).find((p) => p.paymentId === paymentId);
    if (!payment) throw new Error(`Payment ${paymentId} not found`);

    this.payments[loanId] = (this.payments[loanId] ?? []).filter((p) => p.paymentId !== paymentId);
    loan.totalPaid -= payment.amountPaid;
    loan.balance = Math.max(loan.totalAmountDue - loan.totalPaid, 0);
    if (loan.balance === 0) loan.status = 'paid';
    else if (loan.status === 'paid') loan.status = 'active';

    return of(loan).pipe(delay(150));
  }

  getLoanDocuments(loanId: string): Observable<LoanDocument[]> {
    return of(this.loanDocuments[loanId] ?? []).pipe(delay(150));
  }

  uploadLoanDocument(loanId: string, file: File): Observable<LoanDocument> {
    const documentId = `LDOC-${Math.floor(Math.random() * 900000 + 100000)}`;
    const document: LoanDocument = {
      documentId,
      loanId,
      originalFileName: file.name,
      contentType: file.type,
      fileSizeBytes: file.size,
      uploadedAt: new Date().toISOString(),
      uploadedBy: 'admin',
    };
    this.loanDocuments[loanId] = [...(this.loanDocuments[loanId] ?? []), document];
    this.loanDocumentBlobs[documentId] = file;
    return of(document).pipe(delay(150));
  }

  downloadLoanDocument(loanId: string, documentId: string): Observable<Blob> {
    const blob = this.loanDocumentBlobs[documentId];
    if (!blob) throw new Error(`Document ${documentId} not found`);
    return of(blob).pipe(delay(150));
  }

  deleteLoanDocument(loanId: string, documentId: string): Observable<void> {
    this.loanDocuments[loanId] = (this.loanDocuments[loanId] ?? []).filter((d) => d.documentId !== documentId);
    delete this.loanDocumentBlobs[documentId];
    return of(undefined).pipe(delay(150));
  }

  getLoanLedger(loanId: string): Observable<LoanLedgerEntry[]> {
    return of(this.buildLedger(loanId)).pipe(delay(150));
  }

  getLoanAuditLog(loanId: string): Observable<LoanAuditLogEntry[]> {
    const entries = [...(this.auditLog[loanId] ?? [])].sort((a, b) => b.occurredAt.localeCompare(a.occurredAt));
    return of(entries).pipe(delay(150));
  }

  downloadLoanSoa(loanId: string): Observable<Blob> {
    const loan = this.loans.find((l) => l.loanId === loanId);
    if (!loan) throw new Error(`Loan ${loanId} not found`);

    const lines = [
      'Statement of Account',
      `Loan ${loan.loanNumber}`,
      '',
      `Customer: ${loan.customerName}`,
      `Loan Date: ${loan.startDate}   Due Date: ${loan.dueDate}`,
      `Principal: PHP ${loan.principalAmount.toLocaleString()}   Interest: PHP ${loan.totalInterest.toLocaleString()}`,
      `Extension Charges: PHP ${loan.totalExtensionCharges.toLocaleString()}`,
      `Total Amount Due: PHP ${loan.totalAmountDue.toLocaleString()}`,
      `Total Payments: PHP ${loan.totalPaid.toLocaleString()}`,
      `Outstanding Balance: PHP ${loan.balance.toLocaleString()}`,
      `Status: ${loan.status}   Classification: ${loan.classification}`,
    ];

    return of(buildSimplePdfBlob(lines)).pipe(delay(150));
  }
}
