// Domain layer — Loan Details "Documents" tab. Metadata only; the byte content
// is never modeled here (see LoanRepository.downloadLoanDocument, which returns a Blob directly).

export interface LoanDocument {
  documentId: string;
  loanId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
  uploadedBy: string;
}
