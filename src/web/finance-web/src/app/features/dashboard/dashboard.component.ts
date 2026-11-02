import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { BudgetAlertLevel, DashboardSummary } from './dashboard.models';
import { DashboardService } from './dashboard.service';

@Component({
  selector: 'pfp-dashboard',
  imports: [CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  private readonly dashboardService = inject(DashboardService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly netBalance = computed(() => {
    const s = this.summary();
    return s ? s.totalIncome - s.totalExpenses : 0;
  });

  protected readonly alertingBudgets = computed(
    () => this.summary()?.budgets.filter((b) => b.alertLevel !== 'None').length ?? 0,
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardService
      .getSummary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary) => {
          this.summary.set(summary);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('We could not load your financial summary. Please try again.');
          this.loading.set(false);
        },
      });
  }

  protected progressClass(level: BudgetAlertLevel): string {
    return `progress--${level.toLowerCase()}`;
  }

  /** Bar width never exceeds the track even when the budget is overspent. */
  protected barWidth(percentUsed: number): number {
    return Math.min(Math.max(percentUsed, 0), 100);
  }
}
