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
}

/** A payment enriched with the customer name, for the dashboard's "recent payments" feed (SRS wireframe 2). */
export interface PaymentWithCustomer extends Payment {
  customerName: string;
  /** Human-friendly sequential code (e.g. "LM-001") — display this, never loanId's raw GUID. */
  loanNumber: string;
}
