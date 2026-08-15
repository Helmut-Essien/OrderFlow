import { Injectable, computed, signal } from '@angular/core';
import { AuthResponse, MeResponse, PlanInfo } from '../auth/auth.models';

/**
 * Shop/plan Signals shared across features. Updated by {@link AuthService} on login, `/me`, and logout.
 * Prefer this over reading auth only in templates.
 */
@Injectable({ providedIn: 'root' })
export class ShopStateService {
  readonly shopId = signal<string | null>(null);
  readonly shopName = signal<string | null>(null);
  readonly plan = signal<PlanInfo | null>(null);

  /** Amber dashboard callout when Platform `planName` did not map to a known quota. */
  readonly planUnrecognized = computed(() => this.plan()?.isUnrecognized === true);

  setFromSession(user: MeResponse | AuthResponse | null): void {
    if (!user) {
      this.clear();
      return;
    }

    this.shopId.set(user.shopId);
    this.shopName.set(user.shopName);
    this.plan.set(user.plan);
  }

  clear(): void {
    this.shopId.set(null);
    this.shopName.set(null);
    this.plan.set(null);
  }
}
