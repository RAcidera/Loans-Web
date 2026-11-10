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
        font: inherit;
        font-size: 11px;
        line-height: 15px;
        color: var(--text-muted);
        font-weight: 600;
        cursor: pointer;
        padding: 1px 6px;
        border-radius: 4px;
        width: 100%;
      }

      .more-events:hover,
      .more-events:focus-visible {
        color: var(--primary-text);
        background: var(--surface-sunken);
      }

      .more-events:focus-visible {
        outline: 2px solid var(--focus-ring);
        outline-offset: 1px;
      }
    `,
  ],
})
export class CalendarMoreEventsComponent {
  @Input({ required: true }) count!: number;
  @Output() clicked = new EventEmitter<void>();
}
