import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { DashboardDto } from './dashboard.models';

/** HTTP client for `GET /api/dashboard`. Mirrors `DashboardController`. */
@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);

  /** Loads shop KPIs, low-stock rows, and up to 10 recent orders. */
  get() {
    return this.http.get<DashboardDto>(`${environment.apiUrl}/api/dashboard`);
  }
}
