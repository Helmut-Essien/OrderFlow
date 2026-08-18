import { DatePipe, NgClass } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { SeoService } from '../../../../core/seo/seo.service';
import { apiErrorMessage } from '../../../../shared/http/api-error';
import { GhsCurrencyPipe } from '../../../../shared/pipes/ghs-currency.pipe';
import { OrderApi } from '../../data/order.api';
import {
  OrderDto,
  OrderStatusAction,
  nextOrderActions,
  orderStatusChipClass
} from '../../data/order.models';

/** Order detail: line snapshots and allowed status actions. `version` is never shown. */
@Component({
  selector: 'app-order-detail',
  imports: [RouterLink, GhsCurrencyPipe, DatePipe, NgClass],
  templateUrl: './order-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderDetailComponent {
  private readonly api = inject(OrderApi);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly seo = inject(SeoService);
  private currentOrderId: string | null = null;
  private loadRequestId = 0;

  readonly chipClass = orderStatusChipClass;
  readonly order = signal<OrderDto | null>(null);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly confirmingCancel = signal(false);
  readonly error = signal<string | null>(null);

  readonly actions = computed(() => nextOrderActions(this.order()?.status ?? ''));
  readonly forwardActions = computed(() => this.actions().filter((action) => action.kind === 'forward'));
  readonly cancelAction = computed(
    () => this.actions().find((action) => action.kind === 'cancel') ?? null
  );

  /**
   * Pending has not reserved stock; Confirmed/Paid cancel returns it.
   * Copy must stay honest so the shop does not treat every cancel as a stock move.
   */
  readonly cancelWarning = computed(() => {
    const status = this.order()?.status;
    if (status === 'Confirmed' || status === 'Paid') {
      return 'This returns reserved stock to inventory. This cannot be undone.';
    }
    return 'This order has not reserved stock. This cannot be undone.';
  });

  constructor() {
    this.seo.applyPrivatePage('Order — OrderFlow');
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const id = params.get('id');
        this.currentOrderId = id;
        // Clear stale banners on navigation (including 409-reload flows).
        this.error.set(null);

        if (!id) {
          this.order.set(null);
          this.loading.set(false);
          this.error.set('Order not found.');
          return;
        }

        this.load(id);
      });
  }

  /** Moves status along the lifecycle. 409 concurrency reloads so the shop can retry. */
  changeStatus(action: OrderStatusAction): void {
    const current = this.order();
    if (!current || this.submitting()) {
      return;
    }

    this.confirmingCancel.set(false);
    this.error.set(null);
    this.submitting.set(true);
    this.api
      .changeStatus(current.id, {
        status: action.status,
        expectedVersion: current.version
      })
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (updated) => {
          this.order.set(updated);
          this.confirmingCancel.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(apiErrorMessage(err));
          if (err.status === 409) {
            this.load(current.id);
          }
        }
      });
  }

  /** First Cancel tap only asks; stock-releasing cancels must not be a single hit. */
  requestCancel(): void {
    this.confirmingCancel.set(true);
  }

  dismissCancel(): void {
    this.confirmingCancel.set(false);
  }

  confirmCancel(): void {
    const action = this.cancelAction();
    if (!action) {
      return;
    }
    this.changeStatus(action);
  }

  private load(id: string): void {
    const requestId = ++this.loadRequestId;
    this.loading.set(true);
    this.error.set(null);
    this.api.get(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (order) => {
        if (requestId !== this.loadRequestId || this.currentOrderId !== id) {
          return;
        }

        this.order.set(order);
        this.loading.set(false);
      },
      error: (err) => {
        if (requestId !== this.loadRequestId || this.currentOrderId !== id) {
          return;
        }

        this.loading.set(false);
        this.error.set(apiErrorMessage(err));
      }
    });
  }
}
