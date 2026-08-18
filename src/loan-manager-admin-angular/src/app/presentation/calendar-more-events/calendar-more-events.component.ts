import { Component, EventEmitter, Input, Output } from '@angular/core';

/** Requirements' overflow control — "+N more" on a crowded day cell. Never expands the cell; clicking drills into Day view for that date (handled by the caller). */
@Component({
  selector: 'lm-calendar-more-events',
  standalone: true,
  template: `
    <button type="button" class="more-events" (click)="clicked.emit()">+{{ count }} more</button>
  `,
  styles: [
    `
      .more-events {
        border: none;
        background: transparent;
        text-align: left;
        font-size: 10.5px;
        color: var(--lm-text-muted);
        font-weight: 700;
        cursor: pointer;
        padding: 1px 6px;
        width: 100%;
      }

      .more-events:hover,
      .more-events:focus-visible {
        color: var(--lm-primary);
      }

      .more-events:focus-visible {
        outline: 2px solid var(--lm-primary);
        outline-offset: 1px;
      }
    `,
  ],
})
export class CalendarMoreEventsComponent {
  @Input({ required: true }) count!: number;
  @Output() clicked = new EventEmitter<void>();
}
