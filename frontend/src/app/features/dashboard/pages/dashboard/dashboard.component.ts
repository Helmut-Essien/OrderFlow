import { DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { ShopStateService } from '../../../../core/shop/shop-state.service';
import { apiErrorMessage } from '../../../../shared/http/api-error';
import { GhsCurrencyPipe } from '../../../../shared/pipes/ghs-currency.pipe';
import { orderStatusChipClass } from '../../../orders/data/order.models';
import { DashboardApi } from '../../data/dashboard.api';
import { DashboardDto } from '../../data/dashboard.models';

/** Shop home: KPIs, low-stock list, and recent orders. */
@Component({
  selector: 'app-dashboard',
  imports: [GhsCurrencyPipe, RouterLink, DatePipe, NgClass],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
  readonly shop = inject(ShopStateService);
  private readonly dashboardApi = inject(DashboardApi);

  readonly chipClass = orderStatusChipClass;
  readonly data = signal<DashboardDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.dashboardApi
      .get()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (dto) => {
          this.data.set(dto);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(apiErrorMessage(err));
          this.loading.set(false);
        }
      });
  }
}
