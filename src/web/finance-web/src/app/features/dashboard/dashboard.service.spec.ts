import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { DashboardService } from './dashboard.service';
import { DashboardSummary } from './dashboard.models';

describe('DashboardService', () => {
  let service: DashboardService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DashboardService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests the current-month summary when no range is given', () => {
    let result: DashboardSummary | undefined;
    service.getSummary().subscribe((s) => (result = s));

    const req = http.expectOne('/api/summary');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush({ totalIncome: 10, totalExpenses: 4 });

    expect(result?.totalIncome).toBe(10);
  });

  it('passes the date range as query parameters', () => {
    service.getSummary({ from: '2026-11-01', to: '2026-11-30' }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/summary');
    expect(req.request.params.get('from')).toBe('2026-11-01');
    expect(req.request.params.get('to')).toBe('2026-11-30');
    req.flush({});
  });
});
