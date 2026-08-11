// Domain layer — spec 3.1 "Customer Documents Management". Metadata only; the byte content
// is never modeled here (see LoanRepository.downloadCustomerDocument, which returns a Blob directly).

export interface CustomerDocument {
  documentId: string;
  customerId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
  uploadedBy: string;
}
