import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { DashboardComponent } from './dashboard.component';
import { DashboardSummary } from './dashboard.models';
import { DashboardService } from './dashboard.service';

const summary: DashboardSummary = {
  periodStart: '2026-11-01',
  periodEnd: '2026-11-30',
  currency: 'USD',
  totalIncome: 5000,
  totalExpenses: 1200,
  recentTransactions: [
    {
      id: 't1',
      description: '<b>Groceries</b>',
      category: 'Groceries',
      paymentMethod: 'DebitCard',
      amount: 54.2,
      transactionDate: '2026-11-02',
    },
  ],
  budgets: [
    { budgetId: 'b1', category: 'Dining', period: 'Monthly', limitAmount: 200, spentAmount: 170, percentUsed: 85, alertLevel: 'Warning' },
    { budgetId: 'b2', category: 'Travel', period: 'Monthly', limitAmount: 300, spentAmount: 20, percentUsed: 6.7, alertLevel: 'None' },
  ],
};

function setup(getSummary: () => ReturnType<DashboardService['getSummary']>) {
  TestBed.configureTestingModule({
    imports: [DashboardComponent],
    providers: [{ provide: DashboardService, useValue: { getSummary } }],
  });
  return TestBed.createComponent(DashboardComponent);
}

describe('DashboardComponent', () => {
  it('renders totals, budgets and recent transactions', async () => {
    const fixture = setup(() => of(summary));
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('$5,000.00');
    expect(text).toContain('$1,200.00');
    expect(text).toContain('$3,800.00'); // net balance
    expect(text).toContain('1 need attention');
    expect(text).toContain('Dining');
  });

  it('escapes transaction descriptions instead of rendering HTML', async () => {
    const fixture = setup(() => of(summary));
    await fixture.whenStable();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('table b')).toBeNull();
    expect(root.querySelector('table')?.textContent).toContain('<b>Groceries</b>');
  });

  it('shows an error with a retry action when loading fails', async () => {
    const fixture = setup(() => throwError(() => new Error('boom')));
    await fixture.whenStable();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('could not load');
    expect(root.querySelector('button')?.textContent).toContain('Retry');
  });

  it('caps the progress bar at 100% for overspent budgets', () => {
    const fixture = setup(() => of(summary));
    const component = fixture.componentInstance as unknown as { barWidth(p: number): number };

    expect(component.barWidth(135)).toBe(100);
    expect(component.barWidth(-5)).toBe(0);
  });
});
