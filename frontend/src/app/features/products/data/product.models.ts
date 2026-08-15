/**
 * Client field limits; must stay in sync with `ProductConstraints` and Shared DTO `[StringLength]`.
 */
export const PRODUCT_FIELD_LIMITS = {
  name: 200,
  sku: 50,
  category: 80,
  notes: 400,
  stock: 99_999_999,
  price: 999_999_999.99,
  search: 200,
  pageSizeMax: 100
} as const;

/** Mirrors OrderFlow.Shared.DTOs.Products.ProductDto */
export interface ProductDto {
  id: string;
  shopId: string;
  name: string;
  sku: string;
  category?: string | null;
  price: number;
  stock: number;
  lowStockThreshold: number;
  isActive: boolean;
  /** Computed on the server: `stock <= lowStockThreshold`. */
  isLowStock: boolean;
  /** Optimistic concurrency token; send back as `expectedVersion`. */
  version: number;
  createdAt: string;
  updatedAt: string;
}

/** Mirrors OrderFlow.Shared.DTOs.Products.CreateProductRequest */
export interface CreateProductRequest {
  name: string;
  sku: string;
  category?: string;
  price: number;
  stock: number;
  lowStockThreshold: number;
}

/** Mirrors OrderFlow.Shared.DTOs.Products.UpdateProductRequest */
export interface UpdateProductRequest {
  name: string;
  sku: string;
  category?: string;
  price: number;
  lowStockThreshold: number;
  isActive: boolean;
  expectedVersion: number;
}

/** Mirrors OrderFlow.Shared.DTOs.Products.AdjustStockRequest */
export interface AdjustStockRequest {
  quantityDelta: number;
  expectedVersion: number;
  notes?: string;
}

/** Mirrors OrderFlow.Shared.DTOs.Common.PagedResult */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/**
 * Builds an uppercase SKU from the product name, capped at {@link PRODUCT_FIELD_LIMITS.sku}.
 * The API also uppercases on save; this keeps the form in sync before submit.
 */
export function generateSku(name: string): string {
  const slug = name
    .trim()
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  const suffix = Math.random().toString(36).slice(2, 6).toUpperCase();
  const base = (slug || 'SKU').slice(0, Math.max(1, PRODUCT_FIELD_LIMITS.sku - suffix.length - 1));
  return `${base}-${suffix}`.slice(0, PRODUCT_FIELD_LIMITS.sku);
}
