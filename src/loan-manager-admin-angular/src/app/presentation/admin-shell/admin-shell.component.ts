import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { BreakpointObserver } from '@angular/cdk/layout';
import { filter } from 'rxjs/operators';

import { AuthService } from '../../application/auth/auth.service';
import { ChangeMyPasswordDialogComponent } from '../change-my-password-dialog/change-my-password-dialog.component';
import { ThemeService } from '../shared/theme.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
}

interface NavSection {
  title: string;
  items: NavItem[];
}

const COLLAPSED_KEY = 'lm-sidebar-collapsed';

/**
 * Presentation layer — pure UI shell (Cambria dashboard-mockup layout).
 * No knowledge of loans, customers, or cash ledger data; only knows the
 * current route (for the header title), the current session (for the
 * account menu) and the UI theme.
 */
@Component({
  selector: 'lm-admin-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatDialogModule,
  ],
  templateUrl: './admin-shell.component.html',
  styleUrls: ['./admin-shell.component.scss'],
})
export class AdminShellComponent {
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  readonly authService = inject(AuthService);
  readonly theme = inject(ThemeService);

  /** Desktop icons-only rail. Persisted across reloads. */
  collapsed = signal(this.readCollapsed());
  /** Below the tablet breakpoint the sidebar is an off-canvas drawer. */
  isMobile = signal(false);
  mobileNavOpen = signal(false);

  pageTitle = signal('Dashboard');
  sectionTitle = signal('Lending');

  /** Collapsed rail only applies on desktop; the drawer always shows labels. */
  railCollapsed = computed(() => this.collapsed() && !this.isMobile());

  // Same routes, labels, icons and order as before — grouped under section
  // titles in the style of the dashboard mockup.
  readonly navSections: NavSection[] = [
    {
      title: 'Lending',
      items: [
        { label: 'Dashboard', icon: 'grid_view', route: '/dashboard' },
        { label: 'Customers', icon: 'group', route: '/customers' },
        { label: 'Loans', icon: 'receipt_long', route: '/loans' },
        { label: 'Payments', icon: 'payments', route: '/payments' },
        { label: 'Transactions', icon: 'account_balance', route: '/cash-funds' },
      ],
    },
    {
      title: 'Productivity',
      items: [
        { label: 'Diary', icon: 'menu_book', route: '/diary' },
        { label: 'Calendar', icon: 'calendar_month', route: '/calendar' },
        { label: 'Reports', icon: 'insights', route: '/reports' },
      ],
    },
    {
      title: 'Administration',
      items: [{ label: 'Settings', icon: 'tune', route: '/settings' }],
    },
  ];

  constructor() {
    const breakpointObserver = inject(BreakpointObserver);

    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.syncTitle();
      this.mobileNavOpen.set(false);
    });
    this.syncTitle();

    // Crossing the breakpoint closes a stuck-open drawer; the desktop
    // collapsed preference is kept and re-applies on return to desktop.
    breakpointObserver.observe('(max-width: 899px)').subscribe((result) => {
      this.isMobile.set(result.matches);
      if (!result.matches) this.mobileNavOpen.set(false);
    });
  }

  /** Header menu button: collapses the rail on desktop, opens the drawer on mobile. */
  toggleNav(): void {
    if (this.isMobile()) {
      this.mobileNavOpen.update((open) => !open);
      return;
    }
    const next = !this.collapsed();
    this.collapsed.set(next);
    try {
      localStorage.setItem(COLLAPSED_KEY, String(next));
    } catch {
      // ignore — preference just won't persist
    }
  }

  closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }

  initials(): string {
    const name = this.authService.username();
    return name ? name.slice(0, 2).toUpperCase() : '?';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  openChangeMyPassword(): void {
    this.dialog.open(ChangeMyPasswordDialogComponent, { width: '420px', maxWidth: '95vw' });
  }

  private syncTitle(): void {
    for (const section of this.navSections) {
      const match = section.items.find((item) => this.router.url.startsWith(item.route));
      if (match) {
        this.pageTitle.set(match.label);
        this.sectionTitle.set(section.title);
        return;
      }
    }
    this.pageTitle.set('Dashboard');
    this.sectionTitle.set('Lending');
  }

  private readCollapsed(): boolean {
    try {
      return localStorage.getItem(COLLAPSED_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
