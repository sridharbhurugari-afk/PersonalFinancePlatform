import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../../core/api-config';
import { DashboardSummary } from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** Loads the landing-page summary. Dates are ISO (yyyy-MM-dd); omit both for "current month". */
  getSummary(range?: { from: string; to: string }): Observable<DashboardSummary> {
    let params = new HttpParams();
    if (range) {
      params = params.set('from', range.from).set('to', range.to);
    }
    return this.http.get<DashboardSummary>(`${this.baseUrl}/summary`, { params });
  }
}
