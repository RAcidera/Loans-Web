import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'lm-theme'; // also read pre-boot by index.html

/**
 * Presentation-only UI state: light/dark theme.
 *
 * - `preference` is the user's explicit choice, or null = follow the OS.
 * - `effective` is what is actually shown (explicit choice, else OS).
 * - The choice is persisted in localStorage and applied as
 *   `html[data-theme]`, which _cambria-tokens.scss and Angular Material's
 *   color-scheme theme both key off.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly root = inject(DOCUMENT).documentElement;
  private readonly media = window.matchMedia?.('(prefers-color-scheme: dark)');

  private readonly systemDark = signal(this.media?.matches ?? false);
  readonly preference = signal<ThemeMode | null>(this.readStored());
  readonly effective = computed<ThemeMode>(
    () => this.preference() ?? (this.systemDark() ? 'dark' : 'light'),
  );

  constructor() {
    this.apply(this.preference());
    this.media?.addEventListener('change', (e) => this.systemDark.set(e.matches));
  }

  toggle(): void {
    this.set(this.effective() === 'dark' ? 'light' : 'dark');
  }

  set(mode: ThemeMode | null): void {
    this.preference.set(mode);
    try {
      if (mode) localStorage.setItem(STORAGE_KEY, mode);
      else localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Storage unavailable (private mode etc.) — theme still applies for this session.
    }
    this.apply(mode);
  }

  private apply(mode: ThemeMode | null): void {
    if (mode) this.root.setAttribute('data-theme', mode);
    else this.root.removeAttribute('data-theme');
  }

  private readStored(): ThemeMode | null {
    try {
      const v = localStorage.getItem(STORAGE_KEY);
      return v === 'light' || v === 'dark' ? v : null;
    } catch {
      return null;
    }
  }
}
