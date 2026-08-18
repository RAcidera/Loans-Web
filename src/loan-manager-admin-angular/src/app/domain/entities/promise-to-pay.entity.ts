// Domain layer — mirrors PromiseToPayDto from the backend's PromisesToPayController.

/** Requirements §20. Pending/Rescheduled are still-active; Kept/Missed/Cancelled are terminal. */
export type PromiseStatus = 'pending' | 'kept' | 'missed' | 'rescheduled' | 'cancelled';

export interface PromiseToPay {
  promiseId: string;
  customerId: string;
  customerName: string;
  loanId: string;
  loanNumber: string;
  promiseDate: string;
  amount: number;
  notes: string;
  status: PromiseStatus;
  createdBy: string;
  createdAt: string;
  modifiedBy: string;
  modifiedAt: string;
}

/** Backs the Promise Detail's audit section (requirements §24). */
export interface PromiseAuditLogEntry {
  auditLogId: string;
  promiseId: string;
  action: string;
  description: string;
  performedBy: string;
  occurredAt: string;
}
