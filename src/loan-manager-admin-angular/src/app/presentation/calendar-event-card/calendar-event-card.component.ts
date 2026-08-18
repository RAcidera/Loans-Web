import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarEvent } from '../../domain/entities/calendar-event.entity';

/**
 * Requirements' "compact structured card": tinted background, 3px colored
 * left border, bold event-type label, then up to two detail lines
 * (Subtitle, then either a formatted Amount or DetailText) with ellipsis
 * overflow. Used by Month/Week cells (compact=true) and the Day view
 * (compact=false, full text never truncated). Colored via
 * CalendarEvent.color (server-resolved, never hardcoded here) — see
 * CalendarEventDto's doc comment.
 */
@Component({
  selector: 'lm-calendar-event-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      type="button"
      class="event-card"
      [class.event-card--compact]="compact"
      [style.background]="tint"
      [style.borderLeftColor]="event.color"
      (click)="onClick($event)"
      [attr.aria-label]="ariaLabel"
    >
      <div class="event-card__head">
        <span class="event-card__label" [style.color]="event.color">{{ event.title }}</span>
        <span class="event-card__time mono" *ngIf="event.time">{{ formattedTime }}</span>
      </div>
      <span class="event-card__line" *ngIf="event.subtitle">{{ event.subtitle }}</span>
      <span class="event-card__line event-card__line--amount mono" *ngIf="event.amount != null">₱{{ event.amount | number: '1.2-2' }}</span>
      <span class="event-card__line" *ngIf="event.amount == null && event.detailText">{{ event.detailText }}</span>
    </button>
  `,
  styles: [
    `
      .mono {
        font-family: var(--lm-font-mono);
      }

      .event-card {
        display: flex;
        flex-direction: column;
        width: 100%;
        border: none;
        border-left: 3px solid;
        border-radius: 5px;
        padding: 3px 7px 4px;
        text-align: left;
        cursor: pointer;
        overflow: hidden;
        color: var(--lm-text);
        font-family: var(--lm-font-body);
        gap: 1px;
      }

      .event-card:hover,
      .event-card:focus-visible {
        filter: brightness(0.97);
      }

      .event-card:focus-visible {
        outline: 2px solid var(--lm-primary);
        outline-offset: 1px;
      }

      .event-card__head {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 6px;
      }

      .event-card__label {
        font-size: 11px;
        font-weight: 700;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .event-card__time {
        font-size: 9.5px;
        color: var(--lm-text-muted);
        flex-shrink: 0;
      }

      .event-card__line {
        font-size: 11px;
        color: var(--lm-text);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .event-card__line--amount {
        font-weight: 600;
      }

      // Compact (Month/Week cell) mode caps the card at label + 2 detail
      // lines per requirements ("max 2 detail lines where possible") — Day
      // view (compact=false) never sets this, so full text can wrap there.
      .event-card--compact .event-card__line {
        max-width: 100%;
      }
    `,
  ],
})
export class CalendarEventCardComponent {
  @Input({ required: true }) event!: CalendarEvent;
  /** Month/Week cell rendering (single-line ellipsis) vs. Day view's fuller layout. Defaults true. */
  @Input() compact = true;
  @Output() eventClick = new EventEmitter<CalendarEvent>();

  get tint(): string {
    const hex = this.event.color.startsWith('#') ? this.event.color : `#${this.event.color}`;
    return hex.length === 7 ? `${hex}1c` : hex;
  }

  get formattedTime(): string {
    if (!this.event.time) return '';
    const [h, m] = this.event.time.split(':').map(Number);
    const period = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 === 0 ? 12 : h % 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${period}`;
  }

  /** Accessibility — requirements "Always include event type text"; the full text is read even though the visual line clamps to two details. */
  get ariaLabel(): string {
    const parts = [this.event.title, this.event.subtitle, this.event.amount != null ? `₱${this.event.amount.toFixed(2)}` : this.event.detailText, this.event.time ? this.formattedTime : null];
    return parts.filter(Boolean).join(', ');
  }

  onClick(domEvent: Event): void {
    domEvent.stopPropagation();
    this.eventClick.emit(this.event);
  }
}
