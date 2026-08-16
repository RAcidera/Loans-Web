import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';

import { Loan } from '../../domain/entities/loan.entity';
import { LoanExtension } from '../../domain/entities/loan-extension.entity';

interface TimelineTick {
  position: number; // percent along the track
  label: string;
}

/**
 * Signature visual for this SRS's loan model: a fixed start date and due
 * date, occasionally pushed out by extensions, rather than daily
 * installments. The track shows elapsed time against the (possibly
 * extended) due date, with a tick mark at each extension point — the one
 * thing that's actually distinct about how this product's loans behave.
 */
@Component({
  selector: 'lm-loan-timeline',
  standalone: true,
  imports: [CommonModule, MatTooltipModule],
  templateUrl: './loan-timeline.component.html',
  styleUrls: ['./loan-timeline.component.scss'],
})
export class LoanTimelineComponent implements OnChanges {
  @Input({ required: true }) loan!: Loan;
  @Input() extensions: LoanExtension[] = [];

  percentElapsed = 0;
  daysOverdue = 0;
  extensionTicks: TimelineTick[] = [];

  ngOnChanges(): void {
    const start = new Date(this.loan.startDate).getTime();
    const due = new Date(this.loan.dueDate).getTime();
    const today = Date.now();

    const totalSpan = Math.max(due - start, 1);
    const elapsed = today - start;

    this.percentElapsed = Math.min(Math.max((elapsed / totalSpan) * 100, 0), 100);
    this.daysOverdue = today > due && this.loan.status !== 'paid' ? Math.round((today - due) / 86_400_000) : 0;

    this.extensionTicks = this.extensions.map((ext) => {
      const extTime = new Date(ext.extensionDate).getTime();
      const position = Math.min(Math.max(((extTime - start) / totalSpan) * 100, 0), 100);
      return {
        position,
        label: `+${ext.extensionDays} days on ${ext.extensionDate} (+${ext.additionalChargesAmount})`,
      };
    });
  }
}
