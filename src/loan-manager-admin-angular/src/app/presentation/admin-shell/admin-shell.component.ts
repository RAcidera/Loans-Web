import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { BreakpointObserver } from '@angular/cdk/layout';
import { filter } from 'rxjs/operators';

import { AuthService } from '../../application/auth/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
}

/**
 * Presentation layer — pure UI shell. No knowledge of loans, customers, or
 * cash ledger data; only knows the current route (for the topbar title)
 * and the current session (for the avatar/logout menu).
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
    MatButtonModule,
    MatBadgeModule,
    MatMenuModule,
  ],
  templateUrl: './admin-shell.component.html',
  styleUrls: ['./admin-shell.component.scss'],
})
export class AdminShellComponent {
  collapsed = signal(false);
  mobileNavOpen = signal(false);
  pageTitle = signal('Dashboard');

  navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'grid_view', route: '/dashboard' },
    { label: 'Customers', icon: 'group', route: '/customers' },
    { label: 'Loans', icon: 'receipt_long', route: '/loans' },
    { label: 'Cash & Funds', icon: 'account_balance', route: '/cash-funds' },
    { label: 'Reports', icon: 'insights', route: '/reports' },
    { label: 'Settings', icon: 'tune', route: '/settings' },
  ];

  constructor(
    private readonly router: Router,
    private readonly breakpointObserver: BreakpointObserver,
    readonly authService: AuthService,
  ) {
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      const match = this.navItems.find((item) => this.router.url.startsWith(item.route));
      this.pageTitle.set(match?.label ?? 'Dashboard');
      this.mobileNavOpen.set(false);
    });

    // Below the tablet breakpoint the sidebar becomes an off-canvas drawer;
    // clear both signals when crossing the breakpoint so neither the desktop
    // icons-only mode nor a stuck-open drawer carries over.
    this.breakpointObserver.observe('(max-width: 899px)').subscribe((result) => {
      if (result.matches) {
        this.collapsed.set(false);
      } else {
        this.mobileNavOpen.set(false);
      }
    });
  }

  toggleSidebar() {
    this.collapsed.set(!this.collapsed());
  }

  toggleMobileNav() {
    this.mobileNavOpen.set(!this.mobileNavOpen());
  }

  closeMobileNav() {
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
}
