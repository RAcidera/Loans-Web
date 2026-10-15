import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';

import { CapitalGrowthPanelComponent } from './capital-growth-panel.component';
import { GetMonthlyCapitalGrowthUseCase } from '../../../application/use-cases/get-monthly-capital-growth.use-case';
import { CapitalGrowthMonth, MonthlyCapitalGrowth } from '../../../domain/entities/capital-growth.entity';

function month(monthKey: string, partial: Partial<CapitalGrowthMonth> = {}): CapitalGrowthMonth {
  return {
    monthKey, startDate: `${monthKey}-01`, endCutoffDate: `${monthKey}-28`, isPartialMonth: false, status: 'complete',
    beginningCash: 1000, endingCash: 1000, beginningGrossReceivables: 0, endingGrossReceivables: 0,
    beginningPosition: 1000, endingPosition: 1000, ownerWithdrawals: 0, ownerDeposits: 0,
    positionChange: 0, adjustedGrowth: 0, growthPercent: 0,
    ...partial,
  };
}

const NO_HISTORY: Partial<CapitalGrowthMonth> = {
  status: 'nohistory', beginningCash: null, endingCash: null, beginningGrossReceivables: null, endingGrossReceivables: null,
  beginningPosition: null, endingPosition: null, ownerWithdrawals: null, ownerDeposits: null,
  positionChange: null, adjustedGrowth: null, growthPercent: null,
};

function response(series: CapitalGrowthMonth[]): MonthlyCapitalGrowth {
  return {
    timeZone: 'Asia/Manila', asOfUtc: '2026-10-15T04:00:00Z', asOfDate: '2026-10-15', currency: 'PHP', months: 6,
    historyBasis: 'restated', historyStartDate: '2026-07-01', series,
    summary: {
      totalGrowth: 500, monthsInTotal: 3, averageCompletedMonthGrowth: 250, completedMonthCount: 2,
      averageGrowthIncludingCurrentMonth: 166.67, bestMonthKey: '2026-08', bestMonthGrowth: 800,
      previousPeriodGrowth: null, changeVsPreviousPeriodPercent: null,
    },
    reconciliation: { dashboardPosition: 1500, currentEndingPosition: 1500, difference: 0, matches: true, snapshots: [] },
    warnings: [],
  };
}

const SERIES: CapitalGrowthMonth[] = [
  month('2026-05', NO_HISTORY),
  month('2026-06', NO_HISTORY),
  month('2026-07', { status: 'partialhistory', beginningPosition: 0, growthPercent: null, adjustedGrowth: 300 }),
  month('2026-08', { adjustedGrowth: 800, growthPercent: 80, positionChange: 500, ownerWithdrawals: 300, beginningPosition: 1000 }),
  month('2026-09', { adjustedGrowth: -300, growthPercent: null, positionChange: -300, beginningPosition: -10 }),
  month('2026-10', { isPartialMonth: true, status: 'partialmonth', endCutoffDate: '2026-10-15', adjustedGrowth: 0 }),
];

describe('CapitalGrowthPanelComponent', () => {
  let fixture: ComponentFixture<CapitalGrowthPanelComponent>;
  let useCase: jasmine.SpyObj<GetMonthlyCapitalGrowthUseCase>;

  function create(result = of(response(SERIES))): HTMLElement {
    useCase = jasmine.createSpyObj('GetMonthlyCapitalGrowthUseCase', ['execute']);
    useCase.execute.and.returnValue(result);
    TestBed.configureTestingModule({
      imports: [CapitalGrowthPanelComponent],
      providers: [provideRouter([]), provideNoopAnimations(), { provide: GetMonthlyCapitalGrowthUseCase, useValue: useCase }],
    });
    fixture = TestBed.createComponent(CapitalGrowthPanelComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders no-history months as shaded slots without bars, never as zero bars', () => {
    const el = create();
    const slots = el.querySelectorAll('g.cg-slot');

    expect(slots.length).toBe(6);
    expect(slots[0].querySelector('.cg-bar')).toBeNull();
    expect(slots[0].querySelector('.cg-slot__bg--none')).not.toBeNull();
    expect(slots[3].querySelector('.cg-bar--up')).not.toBeNull();
    expect(slots[4].querySelector('.cg-bar--down')).not.toBeNull();
    expect(slots[5].querySelector('.cg-bar--zero.cg-bar--mtd')).not.toBeNull();
  });

  it('selects the latest completed month by default, not the month-to-date one', () => {
    const el = create();

    expect(el.querySelector('.cg-detail__title')?.textContent).toContain('September 2026');
  });

  it('shows N/A growth % when the beginning position is not positive', () => {
    const el = create();

    expect(el.querySelector('.cg-result__pct')?.textContent?.trim()).toBe('N/A');
  });

  it('moves the selection with arrow keys and selects on click', () => {
    const el = create();
    const september = el.querySelector('[data-slot="4"]')!;

    september.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    fixture.detectChanges();
    expect(el.querySelector('.cg-detail__title')?.textContent).toContain('August 2026');
    expect(el.querySelector('.cg-result__pct')?.textContent?.trim()).toBe('+80.00%');

    (el.querySelector('[data-slot="0"]') as SVGGElement).dispatchEvent(new MouseEvent('click'));
    fixture.detectChanges();
    expect(el.querySelector('.cg-detail__empty')).not.toBeNull();
  });

  it('labels the current month as month to date with an as-of date', () => {
    const el = create();

    (el.querySelector('[data-slot="5"]') as SVGGElement).dispatchEvent(new MouseEvent('click'));
    fixture.detectChanges();

    expect(el.querySelector('.cg-pill')?.textContent).toContain('Month to Date');
    expect(el.textContent).toContain('as of Oct 15, 2026');
  });

  it('refetches when the range changes', () => {
    const el = create();
    const buttons = el.querySelectorAll<HTMLButtonElement>('.range-toggle__btn');

    buttons[2].click();

    expect(useCase.execute).toHaveBeenCalledWith(24);
  });

  it('states record coverage relative to the selected range in the summary subtitles', () => {
    const el = create();
    const subs = Array.from(el.querySelectorAll('.cg-stat__sub')).map((s) => s.textContent?.trim());

    expect(subs).toContain('3 of 6 months have records, incl. month to date');
    expect(subs).toContain('2 of 5 completed months have records');
  });

  it('colors positive summary figures green', () => {
    const el = create();
    const total = el.querySelector('.cg-stat__value.tone--up') as HTMLElement;
    const best = el.querySelector('.cg-stat__amount.tone--up') as HTMLElement;

    expect(getComputedStyle(total).color).toBe(getComputedStyle(best).color);
    expect(getComputedStyle(best).color).not.toBe(getComputedStyle(el.querySelector('.cg-stat__sub')!).color);
  });

  it('shows an inline error on failure without throwing', () => {
    const el = create(throwError(() => ({ status: 500 })));

    expect(el.querySelector('.cg-error .error')?.textContent).toContain('could not be loaded');
    expect(el.querySelector('svg')).toBeNull();
  });

  it('links the selected month to the cash ledger filtered to that month', () => {
    const el = create();
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    (el.querySelector('.cg-detail .cg-link') as HTMLButtonElement).click();

    expect(router.navigate).toHaveBeenCalledWith(['/cash-funds'], { queryParams: { dateFrom: '2026-09-01', dateTo: '2026-09-28' } });
  });

  it('shows the empty state when no month has history', () => {
    const el = create(of(response(SERIES.map((m) => ({ ...m, ...NO_HISTORY })))));

    expect(el.querySelector('.empty-hint')?.textContent).toContain('Not enough history yet');
    expect(el.querySelector('svg')).toBeNull();
  });
});
