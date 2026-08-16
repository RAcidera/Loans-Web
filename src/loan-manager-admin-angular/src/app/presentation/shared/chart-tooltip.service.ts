import { Injectable } from '@angular/core';

/**
 * Backs ChartTooltipDirective — a single shared floating tooltip element
 * (created once, reused by every chart on the page) positioned via
 * `position: fixed` and appended straight to <body>, the same reasoning
 * as MatDialog/CDK overlays: it needs to render above everything
 * regardless of which card/tab/scroll-container the hovered chart element
 * sits inside. Deliberately NOT Angular Material's matTooltip or the CDK
 * Overlay — both exist already in this app, but this needs to appear the
 * instant the pointer enters a data point with zero show-delay and follow
 * the cursor on every mousemove, which is simpler to guarantee with direct
 * DOM writes than by fighting a general-purpose overlay's own positioning/
 * animation lifecycle for what is, here, a very narrow use case.
 */
@Injectable({ providedIn: 'root' })
export class ChartTooltipService {
  private el: HTMLDivElement | null = null;

  show(text: string, clientX: number, clientY: number): void {
    const el = this.ensureElement();
    el.textContent = text;
    el.style.visibility = 'visible';
    this.position(clientX, clientY);
  }

  move(clientX: number, clientY: number): void {
    if (this.el) this.position(clientX, clientY);
  }

  hide(): void {
    if (this.el) this.el.style.visibility = 'hidden';
  }

  private ensureElement(): HTMLDivElement {
    if (!this.el) {
      this.el = document.createElement('div');
      this.el.className = 'lm-chart-tooltip';
      this.el.style.visibility = 'hidden';
      document.body.appendChild(this.el);
    }
    return this.el;
  }

  // Anchored above and to the right of the cursor, then flipped to the
  // left/below near the viewport edges so it never renders clipped off-screen.
  private position(clientX: number, clientY: number): void {
    if (!this.el) return;
    const offset = 14;
    const rect = this.el.getBoundingClientRect();

    let left = clientX + offset;
    if (left + rect.width > window.innerWidth - 8) {
      left = clientX - offset - rect.width;
    }

    let top = clientY - offset - rect.height;
    if (top < 8) {
      top = clientY + offset;
    }

    this.el.style.left = `${left}px`;
    this.el.style.top = `${top}px`;
  }
}
