import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, switchMap, EMPTY, startWith } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { apiErrorMessage } from '../../../../shared/http/api-error';
import { GhsCurrencyPipe } from '../../../../shared/pipes/ghs-currency.pipe';
import { requiredTrimmed } from '../../../../shared/validators/auth.validators';
import { ProductApi } from '../../../products/data/product.api';
import { ProductDto } from '../../../products/data/product.models';
import { OrderApi } from '../../data/order.api';
import { ORDER_FIELD_LIMITS } from '../../data/order.models';

/** One catalog row staged on the new-order form before POST. */
export interface OrderDraftLine {
  productId: string;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  stock: number;
}

/**
 * Create a manual order. Product hits are paged search — never the full catalog.
 * `confirmImmediately` defaults on so stock is reserved in the same request.
 */
@Component({
  selector: 'app-order-form',
  imports: [ReactiveFormsModule, RouterLink, GhsCurrencyPipe],
  templateUrl: './order-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OrderApi);
  private readonly products = inject(ProductApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly limits = ORDER_FIELD_LIMITS;
  readonly fieldClass =
    'mt-1.5 w-full min-h-[44px] rounded-lg border border-[#E5E0D5] bg-white px-3 py-2.5 text-ink placeholder:text-slate-400 focus:outline-none focus:border-forest focus:ring-2 focus:ring-forest/20';
  readonly qtyFieldClass =
    'w-full min-h-[44px] rounded-lg border border-[#E5E0D5] bg-white px-3 py-2.5 text-ink placeholder:text-slate-400 focus:outline-none focus:border-forest focus:ring-2 focus:ring-forest/20';

  readonly lines = signal<OrderDraftLine[]>([]);
  readonly productHits = signal<ProductDto[]>([]);
  readonly catalogEmpty = signal(false);
  /** True until the first catalog page returns so the picker never flashes a false miss. */
  readonly picking = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly lineError = signal<string | null>(null);

  private readonly productSearchInput$ = new Subject<string>();
  private productQuery = '';

  readonly form = this.fb.nonNullable.group({
    customerName: ['', [requiredTrimmed, Validators.maxLength(ORDER_FIELD_LIMITS.customerName)]],
    customerPhone: ['', [Validators.maxLength(ORDER_FIELD_LIMITS.customerPhone)]],
    notes: ['', [Validators.maxLength(ORDER_FIELD_LIMITS.notes)]],
    confirmImmediately: [true]
  });

  readonly draftTotal = computed(() =>
    this.lines().reduce((sum, line) => sum + line.unitPrice * line.quantity, 0)
  );

  /** Lines whose qty is above the on-hand snapshot from when the SKU was added. */
  readonly overStockLines = computed(() =>
    this.lines().filter((line) => line.quantity > line.stock)
  );

  constructor() {
    this.productSearchInput$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        // First empty query skips debounce so open does not sit on a fake "no match".
        startWith(''),
        switchMap((query) => {
          this.picking.set(true);
          this.productQuery = query;
          return this.products
            .list({
              search: query,
              page: 1,
              pageSize: 8
            })
            .pipe(
              finalize(() => this.picking.set(false)),
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
          this.productHits.set(result.items.filter((p) => p.isActive));
          this.catalogEmpty.set(result.totalCount === 0 && this.productQuery.trim().length === 0);
        }
      });
  }

  showError(
    controlName: 'customerName' | 'customerPhone' | 'notes',
    errorCode: string
  ): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.hasError(errorCode);
  }

  /** True when the SKU is already a draft line (API also rejects duplicate product ids). */
  isOnOrder(productId: string): boolean {
    return this.lines().some((line) => line.productId === productId);
  }

  onProductSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.productSearchInput$.next(value);
  }

  /** Adds an active SKU once. Duplicate product ids are rejected by the API too. */
  addProduct(product: ProductDto): void {
    this.lineError.set(null);

    if (!product.isActive) {
      this.lineError.set('Inactive products cannot be added to an order.');
      return;
    }

    if (this.lines().some((line) => line.productId === product.id)) {
      this.lineError.set('That product is already on this order.');
      return;
    }

    if (this.lines().length >= ORDER_FIELD_LIMITS.maxLines) {
      this.lineError.set(`An order can have at most ${ORDER_FIELD_LIMITS.maxLines} lines.`);
      return;
    }

    this.lines.update((current) => [
      ...current,
      {
        productId: product.id,
        productName: product.name,
        sku: product.sku,
        unitPrice: product.price,
        quantity: 1,
        stock: product.stock
      }
    ]);
  }

  removeLine(productId: string): void {
    this.lineError.set(null);
    this.lines.update((current) => current.filter((line) => line.productId !== productId));
  }

  /** Clamps quantity to the API range; non-integers fall back to 1. */
  setQuantity(productId: string, event: Event): void {
    const raw = Number((event.target as HTMLInputElement).value);
    const quantity = Number.isInteger(raw)
      ? Math.min(ORDER_FIELD_LIMITS.maxQuantity, Math.max(ORDER_FIELD_LIMITS.minQuantity, raw))
      : ORDER_FIELD_LIMITS.minQuantity;

    this.lines.update((current) =>
      current.map((line) => (line.productId === productId ? { ...line, quantity } : line))
    );
  }

  /** Creates the order. Blank phone/notes are omitted so the API does not store whitespace. */
  submit(): void {
    this.error.set(null);
    this.lineError.set(null);
    this.form.markAllAsTouched();
    this.form.updateValueAndValidity();

    if (this.form.invalid) {
      return;
    }

    if (this.lines().length === 0) {
      this.lineError.set('Add at least one product.');
      return;
    }

    const value = this.form.getRawValue();
    if (value.confirmImmediately && this.overStockLines().length > 0) {
      this.lineError.set(
        'Quantity is more than on-hand stock. Lower the qty or uncheck Reserve stock now.'
      );
      return;
    }
    const phone = value.customerPhone.trim();
    const notes = value.notes.trim();
    this.submitting.set(true);

    this.api
      .create({
        customerName: value.customerName.trim(),
        customerPhone: phone || undefined,
        notes: notes || undefined,
        confirmImmediately: value.confirmImmediately,
        lines: this.lines().map((line) => ({
          productId: line.productId,
          quantity: line.quantity
        }))
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          this.submitting.set(false);
          void this.router.navigate(['/app/orders', order.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.error.set(apiErrorMessage(err));
        }
      });
  }
}
