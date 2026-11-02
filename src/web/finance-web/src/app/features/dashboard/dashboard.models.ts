export type BudgetAlertLevel = 'None' | 'Warning' | 'Exceeded';
export type BudgetPeriod = 'Weekly' | 'Monthly';

export interface RecentTransaction {
  id: string;
  description: string | null;
  category: string;
  paymentMethod: string;
  amount: number;
  transactionDate: string; // ISO date (yyyy-MM-dd)
}

export interface BudgetStatus {
  budgetId: string;
  category: string;
  period: BudgetPeriod;
  limitAmount: number;
  spentAmount: number;
  percentUsed: number;
  alertLevel: BudgetAlertLevel;
}

/** Contract of GET /api/summary served by the Summary service. */
export interface DashboardSummary {
  periodStart: string;
  periodEnd: string;
  currency: string;
  totalIncome: number;
  totalExpenses: number;
  recentTransactions: RecentTransaction[];
  budgets: BudgetStatus[];
}
