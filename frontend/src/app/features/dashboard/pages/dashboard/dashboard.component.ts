import { Component, inject } from '@angular/core';
import { AuthService } from '../../../../core/auth/auth.service';
import { ShopStateService } from '../../../../core/shop/shop-state.service';
import { GhsCurrencyPipe } from '../../../../shared/pipes/ghs-currency.pipe';

@Component({
  selector: 'app-dashboard',
  imports: [GhsCurrencyPipe],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
  readonly shop = inject(ShopStateService);
}
