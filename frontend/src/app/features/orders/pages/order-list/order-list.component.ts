import { DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Subject, switchMap, EMPTY } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { ShopStateService } from '../../../../core/shop/shop-state.service';
import { apiErrorMessage } from '../../../../shared/http/api-error';
import { GhsCurrencyPipe } from '../../../../shared/pipes/ghs-currency.pipe';
import { OrderApi } from '../../data/order.api';
import {
  ORDER_FIELD_LIMITS,
  ORDER_STATUS_FILTERS,
  OrderListDto,
  OrderStatus,
  orderStatusChipClass
} from '../../data/order.models';

/** Orders list: search, status chips, pagination. Plan copy is advisory — list total is not the monthly cap. */
@Component({
  selector: 'app-order-list',
  imports: [RouterLink, GhsCurrencyPipe, DatePipe, NgClass],
  templateUrl: './order-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderListComponent {
  private readonly api = inject(OrderApi);
  readonly shop = inject(ShopStateService);

  readonly limits = ORDER_FIELD_LIMITS;
  readonly statusFilters = ORDER_STATUS_FILTERS;
  readonly pageSize = 20;
  readonly fieldClass =
    'w-full min-h-[44px] rounded-lg border border-[#E5E0D5] bg-white px-3 py-2.5 text-ink placeholder:text-slate-400 focus:outline-none focus:border-forest focus:ring-2 focus:ring-forest/20';
  readonly chipClass = orderStatusChipClass;

  readonly items = signal<OrderListDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly search = signal('');
  readonly status = signal<OrderStatus | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  private readonly searchInput$ = new Subject<string>();
  /** Latest list request wins; in-flight HTTP is cancelled via switchMap. */
  private readonly load$ = new Subject<void>();

  readonly rangeStart = computed(() => {
    if (this.totalCount() === 0) {
      return 0;
    }
    return (this.page() - 1) * this.pageSize + 1;
  });

  readonly rangeEnd = computed(() =>
    Math.min(this.page() * this.pageSize, this.totalCount())
  );

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize))
  );

  /** True when search text or a status chip is narrowing the list. */
  readonly hasFilters = computed(
    () => this.search().trim().length > 0 || this.status() !== null
  );

  constructor() {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((value) => {
        this.search.set(value);
        this.page.set(1);
        this.load$.next();
      });

    this.load$
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.api
            .list({
              search: this.search(),
              status: this.status() ?? undefined,
              page: this.page(),
              pageSize: this.pageSize
            })
            .pipe(
              finalize(() => this.loading.set(false)),
              catchError((err) => {
                this.error.set(apiErrorMessage(err));
                return EMPTY;
              })
            );
        }),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.page.set(result.page);
        }
      });

    this.load$.next();
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInput$.next(value);
  }

  /** Chip label for the status filter row (`null` is All). */
  statusFilterLabel(status: OrderStatus | null): string {
    return status ?? 'All';
  }

  selectStatus(status: OrderStatus | null): void {
    this.status.set(status);
    this.page.set(1);
    this.load$.next();
  }

  previousPage(): void {
    if (this.page() <= 1) {
      return;
    }
    this.page.update((p) => p - 1);
    this.load$.next();
  }

  nextPage(): void {
    if (this.page() >= this.totalPages()) {
      return;
    }
    this.page.update((p) => p + 1);
    this.load$.next();
  }
}
