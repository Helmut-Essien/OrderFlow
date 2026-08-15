import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { ShopStateService } from '../../../../core/shop/shop-state.service';
import { apiErrorMessage } from '../../../../shared/http/api-error';
import { GhsCurrencyPipe } from '../../../../shared/pipes/ghs-currency.pipe';
import { ProductApi } from '../../data/product.api';
import { PRODUCT_FIELD_LIMITS, ProductDto } from '../../data/product.models';

/** Inventory list: search, category chips, pagination, and plan-cap awareness. */
@Component({
  selector: 'app-product-list',
  imports: [RouterLink, GhsCurrencyPipe],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent {
  private readonly api = inject(ProductApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly shop = inject(ShopStateService);

  readonly limits = PRODUCT_FIELD_LIMITS;
  readonly pageSize = 20;
  readonly fieldClass =
    'w-full min-h-[44px] rounded-lg border border-[#E5E0D5] bg-white px-3 py-2.5 text-ink placeholder:text-slate-400 focus:outline-none focus:border-forest focus:ring-2 focus:ring-forest/20';

  readonly items = signal<ProductDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly search = signal('');
  readonly category = signal<string | null>(null);
  readonly categories = signal<string[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  private readonly searchInput$ = new Subject<string>();

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

  /** True when the shop has reached `PlanQuota.MaxProducts`; hide Add Product. */
  readonly atPlanLimit = computed(() => {
    const max = this.shop.plan()?.maxProducts;
    return max != null && this.totalCount() >= max;
  });

  constructor() {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((value) => {
        this.search.set(value);
        this.page.set(1);
        this.load();
      });

    this.load();
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInput$.next(value);
  }

  selectCategory(category: string | null): void {
    this.category.set(category);
    this.page.set(1);
    this.load();
  }

  previousPage(): void {
    if (this.page() <= 1) {
      return;
    }
    this.page.update((p) => p - 1);
    this.load();
  }

  nextPage(): void {
    if (this.page() >= this.totalPages()) {
      return;
    }
    this.page.update((p) => p + 1);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .list({
        search: this.search(),
        category: this.category() ?? undefined,
        page: this.page(),
        pageSize: this.pageSize
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.page.set(result.page);
          this.mergeCategories(result.items);
        },
        error: (err) => this.error.set(apiErrorMessage(err))
      });
  }

  private mergeCategories(items: ProductDto[]): void {
    const next = new Set(this.categories());
    for (const item of items) {
      const category = item.category?.trim();
      if (category) {
        next.add(category);
      }
    }
    const selected = this.category();
    if (selected) {
      next.add(selected);
    }
    this.categories.set([...next].sort((a, b) => a.localeCompare(b)));
  }
}
