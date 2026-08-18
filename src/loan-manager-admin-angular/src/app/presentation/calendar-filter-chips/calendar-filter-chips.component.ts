import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CalendarEventType } from '../../domain/entities/calendar-event.entity';
import { CALENDAR_EVENT_META, CALENDAR_EVENT_TYPES } from '../shared/calendar-event-meta';

/** Requirements' "compact toggles" filter row — five event-source chips (all enabled by default) plus an Advanced Filters entry point. Deliberately not full checkboxes-in-a-card like the previous layout; each toggle carries its own type color per requirements' "Keep event colors centralized/configurable". */
@Component({
  selector: 'lm-calendar-filter-chips',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './calendar-filter-chips.component.html',
  styleUrls: ['./calendar-filter-chips.component.scss'],
})
export class CalendarFilterChipsComponent {
  @Input({ required: true }) enabledTypes!: Set<CalendarEventType>;
  @Input() advancedFiltersActive = false;

  @Output() toggleType = new EventEmitter<CalendarEventType>();
  @Output() openAdvancedFilters = new EventEmitter<void>();

  readonly types = CALENDAR_EVENT_TYPES;
  readonly meta = CALENDAR_EVENT_META;

  isEnabled(type: CalendarEventType): boolean {
    return this.enabledTypes.has(type);
  }
}
