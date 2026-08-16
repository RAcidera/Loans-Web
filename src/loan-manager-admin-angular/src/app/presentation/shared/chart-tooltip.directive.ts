import { Directive, HostListener, Input, OnDestroy, inject } from '@angular/core';
import { ChartTooltipService } from './chart-tooltip.service';

/**
 * Fast, custom replacement for a native `[title]`/SVG `<title>` tooltip on
 * chart data points (bars, dots, donut segments) — those rely on the
 * browser's own hover-delay tooltip, which is slow and can't be restyled.
 * `appChartTooltip` appears the instant the pointer enters the element,
 * follows the cursor, and is styled to match the app (see `.lm-chart-tooltip`
 * in styles.scss). Usage: `<span [appChartTooltip]="'₱1,234.00 — Jun 2026'">`.
 */
@Directive({
  selector: '[appChartTooltip]',
  standalone: true,
})
export class ChartTooltipDirective implements OnDestroy {
  @Input('appChartTooltip') text = '';

  private readonly tooltipService = inject(ChartTooltipService);

  @HostListener('mouseenter', ['$event'])
  onMouseEnter(event: MouseEvent): void {
    if (!this.text) return;
    this.tooltipService.show(this.text, event.clientX, event.clientY);
  }

  @HostListener('mousemove', ['$event'])
  onMouseMove(event: MouseEvent): void {
    if (!this.text) return;
    this.tooltipService.move(event.clientX, event.clientY);
  }

  @HostListener('mouseleave')
  onMouseLeave(): void {
    this.tooltipService.hide();
  }

  ngOnDestroy(): void {
    this.tooltipService.hide();
  }
}
