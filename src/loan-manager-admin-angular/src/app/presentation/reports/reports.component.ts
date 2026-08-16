import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

/**
 * Presentation layer — the Reports landing page. Its own quick-look
 * sections (all-time interest, period summary, per-customer summary) were
 * superseded by the dedicated, exportable Interest Earned Report and
 * removed rather than kept as a redundant second view of the same data;
 * this page is now just an entry point into that (and future) reports.
 */
@Component({
  selector: 'lm-reports',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
})
export class ReportsComponent {
  constructor(readonly router: Router) {}
}
