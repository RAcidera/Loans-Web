import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog.component';

/**
 * Drop-in replacement for the browser's native confirm() — same call
 * shape (ask a yes/no question, get a boolean back), but themed to match
 * the app instead of an unstyleable OS popup. Every destructive action
 * across the app (delete payment/extension/document/transaction, deactivate
 * a user, and any future one) should go through this rather than a raw
 * confirm() call or a one-off dialog, so confirmations stay visually and
 * behaviorally consistent app-wide.
 *
 * Usage (mirrors the old `if (!confirm(...)) return;` shape almost exactly):
 *   async deleteThing(thing: Thing): Promise<void> {
 *     if (!(await this.confirmDialog.confirm({ message: `Delete ${thing.name}?` }))) return;
 *     this.deleteThingUseCase.execute(thing.id).subscribe(() => this.load());
 *   }
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly dialog = inject(MatDialog);

  async confirm(data: ConfirmDialogData): Promise<boolean> {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '380px',
      maxWidth: '95vw',
      data,
      autoFocus: 'first-tabbable',
    });
    const result = await firstValueFrom(dialogRef.afterClosed());
    return result === true;
  }
}
