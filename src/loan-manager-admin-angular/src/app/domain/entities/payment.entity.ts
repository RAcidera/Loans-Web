// Domain layer — mirrors the Payments table in the SRS.
// Per the SRS: "Each payment automatically creates a payment_received entry
// in the Cash_Ledger table" — that side effect is an infrastructure concern,
// implemented inside the repository, not modeled here.

export type PaymentMethod = 'cash' | 'gcash' | 'bank_transfer' | 'other';

export interface Payment {
  paymentId: string;
  loanId: string;
  paymentDate: string;
  amountPaid: number;
  paymentMethod: PaymentMethod;
  notes: string;
  referenceNumber?: string;
  /** When this payment record was actually added to the system — distinct from paymentDate, which can be backdated. Server-assigned, never sent on create/update. */
  createdAt: string;
}

/** A payment enriched with the owning customer/loan — backs the dashboard's "recent payments" feed (SRS wireframe 2) and the standalone Payments list page. */
export interface PaymentWithCustomer extends Payment {
  customerId: string;
  customerName: string;
  /** Human-friendly sequential code (e.g. "LM-001") — display this, never loanId's raw GUID. */
  loanNumber: string;
}

/** Payments list page footer total — same filters as getPaymentsPage(), no paging. */
export interface PaymentsTotals {
  totalAmountPaid: number;
  count: number;
}

/** Spec's search/date-range filters for the standalone Payments list page. */
export interface PaymentPageFilters {
  loanSearch?: string;
  customerSearch?: string;
  dateFrom?: string;
  dateTo?: string;
  createdAtFrom?: string;
  createdAtTo?: string;
}
