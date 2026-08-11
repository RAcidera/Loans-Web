// Domain layer — mirrors the Customers table in the SRS.

export type CustomerStatus = 'active' | 'inactive';

export interface Customer {
  customerId: string;
  /** Human-friendly sequential code (e.g. "CUS00001") — display this, never customerId's raw GUID. */
  customerCode: string;
  fullName: string;
  address: string;
  contactNumber: string;
  borrowerType: string; // e.g. "Fish vendor", "Vegetable vendor", "Community member"
  /** Spec's "Nickname / Alias" — optional. */
  nicknameAlias: string;
  /** Spec's free-text Notes field — optional. */
  notes: string;
  status: CustomerStatus;
  createdAt: string;
}
