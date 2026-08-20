import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { CustomerDocument } from '../../domain/entities/customer-document.entity';
import { LoanDocument } from '../../domain/entities/loan-document.entity';
import { GetCustomerDocumentsUseCase } from '../../application/use-cases/get-customer-documents.use-case';
import { UploadCustomerDocumentUseCase } from '../../application/use-cases/upload-customer-document.use-case';
import { DownloadCustomerDocumentUseCase } from '../../application/use-cases/download-customer-document.use-case';
import { DeleteCustomerDocumentUseCase } from '../../application/use-cases/delete-customer-document.use-case';
import { GetLoanDocumentsUseCase } from '../../application/use-cases/get-loan-documents.use-case';
import { UploadLoanDocumentUseCase } from '../../application/use-cases/upload-loan-document.use-case';
import { DownloadLoanDocumentUseCase } from '../../application/use-cases/download-loan-document.use-case';
import { DeleteLoanDocumentUseCase } from '../../application/use-cases/delete-loan-document.use-case';
import { ConfirmDialogService } from '../confirm-dialog/confirm-dialog.service';
import { AuthService } from '../../application/auth/auth.service';
import { AppDateTimePipe } from '../shared/app-date-time.pipe';

type OwnerType = 'customer' | 'loan';
type AnyDocument = CustomerDocument | LoanDocument;

const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'application/pdf'];

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * Shared between the Customer profile's Documents section and the Loan
 * Details "Documents" tab (spec 3.1 "Customer Documents Management" +
 * the Loan Details wireframe) — the upload/list/view/download/delete flow
 * is identical for both owner types, differing only in which use case
 * gets called, so this component takes ownerType/ownerId rather than
 * being duplicated per aggregate.
 */
@Component({
  selector: 'lm-document-manager',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule, AppDateTimePipe],
  templateUrl: './document-manager.component.html',
  styleUrls: ['./document-manager.component.scss'],
})
export class DocumentManagerComponent implements OnInit {
  @Input({ required: true }) ownerType!: OwnerType;
  @Input({ required: true }) ownerId!: string;

  documents: AnyDocument[] = [];
  loading = true;
  uploading = false;
  uploadError: string | null = null;

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  constructor(
    private readonly getCustomerDocuments: GetCustomerDocumentsUseCase,
    private readonly uploadCustomerDocument: UploadCustomerDocumentUseCase,
    private readonly downloadCustomerDocument: DownloadCustomerDocumentUseCase,
    private readonly deleteCustomerDocument: DeleteCustomerDocumentUseCase,
    private readonly getLoanDocuments: GetLoanDocumentsUseCase,
    private readonly uploadLoanDocument: UploadLoanDocumentUseCase,
    private readonly downloadLoanDocument: DownloadLoanDocumentUseCase,
    private readonly deleteLoanDocument: DeleteLoanDocumentUseCase,
    private readonly confirmDialog: ConfirmDialogService,
    readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading = true;
    const documents$: Observable<AnyDocument[]> = this.ownerType === 'customer'
      ? this.getCustomerDocuments.execute(this.ownerId)
      : this.getLoanDocuments.execute(this.ownerId);

    documents$.subscribe((documents) => {
      this.documents = documents;
      this.loading = false;
    });
  }

  openFilePicker(): void {
    this.fileInput.nativeElement.click();
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];
    input.value = ''; // allow re-selecting the same file later

    for (const file of files) {
      this.upload(file);
    }
  }

  private upload(file: File): void {
    if (!ACCEPTED_TYPES.includes(file.type)) {
      this.uploadError = `"${file.name}" isn't a supported file type. Only JPG, PNG, and PDF are allowed.`;
      return;
    }

    this.uploadError = null;
    this.uploading = true;
    const upload$: Observable<AnyDocument> = this.ownerType === 'customer'
      ? this.uploadCustomerDocument.execute(this.ownerId, file)
      : this.uploadLoanDocument.execute(this.ownerId, file);

    upload$.subscribe({
      next: () => {
        this.uploading = false;
        this.load();
      },
      error: (err) => {
        this.uploading = false;
        this.uploadError = err?.status === 400
          ? `"${file.name}" was rejected — check the file type and size.`
          : `Failed to upload "${file.name}".`;
      },
    });
  }

  view(doc: AnyDocument): void {
    this.download(doc, false);
  }

  download(doc: AnyDocument, forceSave = true): void {
    const download$ = this.ownerType === 'customer'
      ? this.downloadCustomerDocument.execute(this.ownerId, doc.documentId)
      : this.downloadLoanDocument.execute(this.ownerId, doc.documentId);

    download$.subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      if (forceSave) {
        const link = document.createElement('a');
        link.href = url;
        link.download = doc.originalFileName;
        link.click();
      } else {
        window.open(url, '_blank');
      }
      setTimeout(() => URL.revokeObjectURL(url), 30_000);
    });
  }

  async remove(doc: AnyDocument): Promise<void> {
    const ok = await this.confirmDialog.confirm({
      title: 'Delete document?',
      message: `Delete "${doc.originalFileName}"? This cannot be undone.`,
      confirmText: 'Yes, delete',
    });
    if (!ok) return;

    const delete$ = this.ownerType === 'customer'
      ? this.deleteCustomerDocument.execute(this.ownerId, doc.documentId)
      : this.deleteLoanDocument.execute(this.ownerId, doc.documentId);

    delete$.subscribe(() => this.load());
  }

  formatFileSize(bytes: number): string {
    return formatFileSize(bytes);
  }
}
