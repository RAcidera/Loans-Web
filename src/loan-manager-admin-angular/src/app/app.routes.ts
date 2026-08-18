import { Routes } from '@angular/router';
import { AdminShellComponent } from './presentation/admin-shell/admin-shell.component';
import { DashboardComponent } from './presentation/dashboard/dashboard.component';
import { CashFundsComponent } from './presentation/cash-funds/cash-funds.component';
import { CustomersComponent } from './presentation/customers/customers.component';
import { CustomerProfileComponent } from './presentation/customer-profile/customer-profile.component';
import { LoansComponent } from './presentation/loans/loans.component';
import { LoanDetailsComponent } from './presentation/loan-details/loan-details.component';
import { PaymentsComponent } from './presentation/payments/payments.component';
import { ReportsComponent } from './presentation/reports/reports.component';
import { DiaryListComponent } from './presentation/diary-list/diary-list.component';
import { DiaryDetailComponent } from './presentation/diary-detail/diary-detail.component';
import { CalendarPageComponent } from './presentation/calendar-page/calendar-page.component';
import { InterestEarnedReportComponent } from './presentation/interest-earned-report/interest-earned-report.component';
import { SettingsComponent } from './presentation/settings/settings.component';
import { LoginComponent } from './presentation/login/login.component';
import { authGuard } from './infrastructure/auth/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'cash-funds', component: CashFundsComponent },
      { path: 'customers', component: CustomersComponent },
      { path: 'customers/:id', component: CustomerProfileComponent },
      { path: 'loans', component: LoansComponent },
      { path: 'loans/:id', component: LoanDetailsComponent },
      { path: 'payments', component: PaymentsComponent },
      { path: 'diary', component: DiaryListComponent },
      { path: 'diary/:id', component: DiaryDetailComponent },
      { path: 'calendar', component: CalendarPageComponent },
      { path: 'reports', component: ReportsComponent },
      { path: 'reports/interest-earned', component: InterestEarnedReportComponent },
      { path: 'settings', component: SettingsComponent },
    ],
  },
];
