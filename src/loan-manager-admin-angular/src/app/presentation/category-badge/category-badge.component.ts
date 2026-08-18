import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

/**
 * Reusable category chip — reads its color/icon from a DiaryCategory's own
 * DisplayColor/Icon fields, never a hardcoded palette (requirements §5
 * explicitly forbids hardcoding category colors in the Angular app).
 */
@Component({
  selector: 'lm-category-badge',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <span class="category-badge" [style.background]="tint" [style.color]="color">
      <mat-icon *ngIf="icon">{{ icon }}</mat-icon>
      {{ name }}
    </span>
  `,
  styles: [
    `
      .category-badge {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 3px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        white-space: nowrap;
      }

      mat-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
      }
    `,
  ],
})
export class CategoryBadgeComponent {
  @Input() name = '';
  @Input() icon = '';
  @Input() color = '#6B7280';

  /** A translucent tint of `color` for the chip background — appends an alpha suffix to the stored hex value rather than a second hardcoded palette. */
  get tint(): string {
    const hex = this.color.startsWith('#') ? this.color : `#${this.color}`;
    return hex.length === 7 ? `${hex}22` : hex;
  }
}
