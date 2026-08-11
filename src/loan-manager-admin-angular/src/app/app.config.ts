import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';

import { routes } from './app.routes';
import { LoanRepository } from './domain/repositories/loan.repository';
import { CashLedgerRepository } from './domain/repositories/cash-ledger.repository';
import { ReportRepository } from './domain/repositories/report.repository';
import { UserRepository } from './domain/repositories/user.repository';
import { HttpLoanRepository } from './infrastructure/repositories/http-loan.repository';
import { HttpCashLedgerRepository } from './infrastructure/repositories/http-cash-ledger.repository';
import { HttpReportRepository } from './infrastructure/repositories/http-report.repository';
import { HttpUserRepository } from './infrastructure/repositories/http-user.repository';
import { authInterceptor } from './infrastructure/auth/auth.interceptor';

// Swap the three `useClass` lines below (and drop provideHttpClient) to go
// back to in-memory mock data for an offline demo — see
// infrastructure/repositories/mock-loan.repository.ts,
// mock-cash-ledger.repository.ts, and mock-report.repository.ts, which are
// still present and unchanged.
// Note: the mocks don't require login, so also remove authGuard from
// app.routes.ts if going fully offline.
//
// import { MockLoanRepository } from './infrastructure/repositories/mock-loan.repository';
// import { MockCashLedgerRepository } from './infrastructure/repositories/mock-cash-ledger.repository';
// import { MockReportRepository } from './infrastructure/repositories/mock-report.repository';

/**
 * COMPOSITION ROOT.
 *
 * The only place that ties the abstract ports (LoanRepository,
 * CashLedgerRepository, ReportRepository) to concrete implementations.
 * Every use case and every component depends on the abstract classes only.
 *
 * Wired to the real LoanManagementSystem.Api backend (.NET 8 + EF Core +
 * SQL Server + JWT auth) at the URL configured in
 * src/environments/environment.ts. Run the API first (see its own
 * README), then `ng serve`.
 *
 * authInterceptor attaches the bearer token from AuthService to every
 * outgoing request and redirects to /login on a 401.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideAnimations(),
    provideNativeDateAdapter(),
    provideHttpClient(withInterceptors([authInterceptor])),
    // Every mat-form-field app-wide gets the "label always above the field,
    // never floating as inline placeholder text" look from one place,
    // instead of touching every template that uses <mat-form-field> —
    // see styles.scss for the accompanying outline/background restyle.
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: 'outline', floatLabel: 'always' } },
    { provide: LoanRepository, useClass: HttpLoanRepository },
    { provide: CashLedgerRepository, useClass: HttpCashLedgerRepository },
    { provide: ReportRepository, useClass: HttpReportRepository },
    { provide: UserRepository, useClass: HttpUserRepository },
  ],
};
