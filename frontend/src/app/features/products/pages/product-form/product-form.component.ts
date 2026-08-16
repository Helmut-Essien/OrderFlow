import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../../../shared/http/api-error';
import { GhsCurrencyPipe } from '../../../../shared/pipes/ghs-currency.pipe';
import {
  integerNumber,
  nonZeroNumber,
  requiredTrimmed
} from '../../../../shared/validators/auth.validators';
import { ProductApi } from '../../data/product.api';
import {
  generateSku,
  PRODUCT_FIELD_LIMITS,
  ProductDto
} from '../../data/product.models';

/** Add/edit product. Stock changes use a separate form so catalog updates do not race inventory. */
@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, RouterLink, GhsCurrencyPipe],
  templateUrl: './product-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ProductApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly limits = PRODUCT_FIELD_LIMITS;
  readonly fieldClass =
    'mt-1.5 w-full min-h-[44px] rounded-lg border border-[#E5E0D5] bg-white px-3 py-2.5 text-ink placeholder:text-slate-400 focus:outline-none focus:border-forest focus:ring-2 focus:ring-forest/20';

  readonly productId = signal<string | null>(this.route.snapshot.paramMap.get('id'));
  readonly isNew = computed(() => this.productId() === null);
  readonly product = signal<ProductDto | null>(null);
  readonly loading = signal(!this.isNew());
  readonly submitting = signal(false);
  readonly adjusting = signal(false);
  readonly error = signal<string | null>(null);
  readonly stockError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [requiredTrimmed, Validators.maxLength(PRODUCT_FIELD_LIMITS.name)]],
    sku: ['', [requiredTrimmed, Validators.maxLength(PRODUCT_FIELD_LIMITS.sku)]],
    category: ['', [Validators.maxLength(PRODUCT_FIELD_LIMITS.category)]],
    price: this.fb.control<number | null>(null, {
      validators: [
        Validators.required,
        Validators.min(0),
        Validators.max(PRODUCT_FIELD_LIMITS.price)
      ]
    }),
    stock: [0, [Validators.required, integerNumber, Validators.min(0), Validators.max(PRODUCT_FIELD_LIMITS.stock)]],
    lowStockThreshold: [
      0,
      [Validators.required, integerNumber, Validators.min(0), Validators.max(PRODUCT_FIELD_LIMITS.stock)]
    ],
    isActive: [true]
  });

  readonly stockForm = this.fb.nonNullable.group({
    quantityDelta: this.fb.control<number | null>(null, {
      validators: [
        Validators.required,
        integerNumber,
        nonZeroNumber,
        Validators.min(-PRODUCT_FIELD_LIMITS.stock),
        Validators.max(PRODUCT_FIELD_LIMITS.stock)
      ]
    }),
    notes: ['', [Validators.maxLength(PRODUCT_FIELD_LIMITS.notes)]]
  });

  constructor() {
    const id = this.productId();
    if (id) {
      // Stock is adjusted in a separate form; disable so hidden validators cannot block Save.
      this.form.controls.stock.disable({ emitEvent: false });
      this.api.get(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (product) => this.patchProduct(product),
        error: (err) => {
          this.loading.set(false);
          this.error.set(apiErrorMessage(err));
        }
      });
    }
  }

  showError(
    controlName: keyof ProductFormComponent['form']['controls'],
    errorCode: string
  ): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.hasError(errorCode);
  }

  showStockError(errorCode: string): boolean {
    const control = this.stockForm.controls.quantityDelta;
    return control.touched && control.hasError(errorCode);
  }

  /** Fills SKU from the current name; the API also uppercases on save. */
  generateSkuFromName(): void {
    const name = this.form.controls.name.value;
    this.form.controls.sku.setValue(generateSku(name));
    this.form.controls.sku.markAsTouched();
  }

  /** Creates or updates catalog fields. SKU is uppercased; stock is not sent on update. */
  submit(): void {
    this.error.set(null);
    this.form.markAllAsTouched();
    this.form.updateValueAndValidity();

    const catalogInvalid =
      this.form.controls.name.invalid ||
      this.form.controls.sku.invalid ||
      this.form.controls.category.invalid ||
      this.form.controls.price.invalid ||
      this.form.controls.lowStockThreshold.invalid ||
      (this.isNew() && this.form.controls.stock.invalid);

    if (catalogInvalid) {
      return;
    }

    const value = this.form.getRawValue();
    const category = value.category.trim();
    const price = Number(value.price);
    this.submitting.set(true);

    if (this.isNew()) {
      this.api
        .create({
          name: value.name.trim(),
          sku: value.sku.trim().toUpperCase(),
          category: category || undefined,
          price,
          stock: Number(value.stock),
          lowStockThreshold: Number(value.lowStockThreshold)
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => this.goToInventory(),
          error: (err) => this.handleSaveError(err)
        });
      return;
    }

    const current = this.product();
    if (!current) {
      this.submitting.set(false);
      return;
    }

    this.api
      .update(current.id, {
        name: value.name.trim(),
        sku: value.sku.trim().toUpperCase(),
        category: category || undefined,
        price,
        lowStockThreshold: Number(value.lowStockThreshold),
        isActive: value.isActive,
        expectedVersion: current.version
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        // Do not patch the form first — signal writes in this tick can abort the navigation.
        next: () => this.goToInventory(),
        error: (err) => this.handleSaveError(err)
      });
  }

  /** Manual stock delta using the product's current `version`. Reloads on 409 concurrency. */
  adjustStock(): void {
    const current = this.product();
    if (!current) {
      return;
    }

    this.stockError.set(null);
    this.stockForm.markAllAsTouched();
    this.stockForm.updateValueAndValidity();
    if (this.stockForm.invalid) {
      return;
    }

    const value = this.stockForm.getRawValue();
    const notes = value.notes.trim();
    this.adjusting.set(true);

    this.api
      .adjustStock(current.id, {
        quantityDelta: Number(value.quantityDelta),
        expectedVersion: current.version,
        notes: notes || undefined
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        // Same as Save: skip form patch so signal writes cannot cancel the route change.
        next: () => this.goToInventory(),
        error: (err: HttpErrorResponse) => {
          this.adjusting.set(false);
          this.stockError.set(apiErrorMessage(err));
          if (err.status === 409) {
            this.reload();
          }
        }
      });
  }

  /** Leaves the form after a successful create, catalog save, or stock adjust so Inventory shows the new qty. */
  private goToInventory(): void {
    this.submitting.set(false);
    this.adjusting.set(false);
    void this.router.navigateByUrl('/app/products');
  }

  private patchProduct(product: ProductDto): void {
    this.product.set(product);
    this.productId.set(product.id);
    this.form.patchValue({
      name: product.name,
      sku: product.sku,
      category: product.category ?? '',
      price: product.price,
      stock: product.stock,
      lowStockThreshold: product.lowStockThreshold,
      isActive: product.isActive
    });
    this.loading.set(false);
  }

  private reload(): void {
    const id = this.productId();
    if (!id) {
      return;
    }
    this.api.get(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (product) => this.patchProduct(product),
      error: (err) => this.error.set(apiErrorMessage(err))
    });
  }

  private handleSaveError(err: HttpErrorResponse): void {
    this.submitting.set(false);
    this.error.set(apiErrorMessage(err));
    if (err.status === 409 && !this.isNew()) {
      this.reload();
    }
  }
}
