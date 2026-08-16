import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ConfirmDialogData {
  /** Defaults to "Delete this?" if omitted — always pass a specific one, this is just a fallback. */
  title?: string;
  /** The specific, unambiguous question — e.g. "Delete this ₱500.00 payment recorded on 08/12/2026? This cannot be undone." */
  message: string;
  confirmText?: string;
  cancelText?: string;
  /** True (the default) styles the confirm button red and shows a warning icon, for destructive/irreversible actions. Set false for a neutral yes/no confirmation. */
  danger?: boolean;
}

/**
 * App-themed replacement for the browser's native confirm() — same
 * light/dark surface, radius, and shadow as every other dialog
 * (_shared-dialog-form.scss), instead of an unstyleable OS-chrome popup.
 * Never instantiate this directly; go through ConfirmDialogService.confirm()
 * so every call site gets the same Promise<boolean> ergonomics confirm()
 * had (Escape/backdrop-click both resolve to false via MatDialogRef's
 * default "closed with no result" behavior — no explicit wiring needed).
 */
@Component({
  selector: 'lm-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrls: ['./confirm-dialog.component.scss'],
})
export class ConfirmDialogComponent {
  readonly data: ConfirmDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);

  get isDanger(): boolean {
    return this.data.danger !== false;
  }

  confirm(): void {
    this.dialogRef.close(true);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
