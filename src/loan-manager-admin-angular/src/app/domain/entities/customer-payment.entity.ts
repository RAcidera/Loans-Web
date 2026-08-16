import { Payment } from './payment.entity';

/** A payment enriched with its loan number — Customer Profile's "Payment History" tab, one customer already known so no CustomerName needed (contrast PaymentWithCustomer). */
export interface CustomerPayment extends Payment {
  loanNumber: string;
}
