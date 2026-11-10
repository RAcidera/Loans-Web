import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

/**
 * Reusable category chip — reads its color/icon from a DiaryCategory's own
 * DisplayColor/Icon fields, never a hardcoded palette (requirements §5
 * explicitly forbids hardcoding category colors in the Angular app).
 *
 * Rendered as the toolkit's compact soft badge (`badge-compact soft`), with
 * the category colour fed into the badge's own --badge-tint/--badge-ink
 * variables: the tint is the colour over transparent, the ink is the colour
 * mixed toward var(--text) so it stays readable in both light and dark mode.
 * `casing="upper"` for lists/grids, the default Title Case for detail views.
 */
@Component({
  selector: 'lm-category-badge',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <span
      class="badge-compact soft category-badge"
      [class.upper]="casing === 'upper'"
      [class.titlecase]="casing !== 'upper'"
      [style.--badge-tint]="tint"
      [style.--badge-ink]="ink"
    >
      <mat-icon *ngIf="icon" aria-hidden="true">{{ icon }}</mat-icon>
      {{ name }}
    </span>
  `,
  styles: [
    `
      .category-badge mat-icon {
        font-size: 12px;
        width: 12px;
        height: 12px;
      }

      .category-badge.upper mat-icon {
        font-size: 11px;
        width: 11px;
        height: 11px;
      }
    `,
  ],
})
export class CategoryBadgeComponent {
  @Input() name = '';
  @Input() icon = '';
  @Input() color = '#6B7280';
  /** `upper` = toolkit ALL CAPS list/grid badge; `titlecase` = detail-view badge. */
  @Input() casing: 'upper' | 'titlecase' = 'titlecase';

  private get hex(): string {
    return this.color.startsWith('#') ? this.color : `#${this.color}`;
  }

  /** Translucent tint of the category colour for the badge background. */
  get tint(): string {
    return `color-mix(in srgb, ${this.hex} 16%, transparent)`;
  }

  /** Category colour pulled toward the theme's text colour — readable on the tint in light and dark mode. */
  get ink(): string {
    return `color-mix(in srgb, ${this.hex} 60%, var(--text))`;
  }
}
