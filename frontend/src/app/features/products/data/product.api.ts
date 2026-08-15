import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import {
  AdjustStockRequest,
  CreateProductRequest,
  ProductDto,
  ProductListResponse,
  UpdateProductRequest
} from './product.models';

/**
 * HTTP client for shop product catalog endpoints.
 * Mirrors `ProductsController`; DTOs match `OrderFlow.Shared/DTOs/Products`.
 */
@Injectable({ providedIn: 'root' })
export class ProductApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/products`;

  /**
   * Lists products for the current shop.
   * @param options `pageSize` is 1–100 (API default 20). Categories and `activeCount` are shop-wide.
   */
  list(options: {
    search?: string;
    category?: string;
    page?: number;
    pageSize?: number;
  } = {}) {
    let params = new HttpParams()
      .set('page', String(options.page ?? 1))
      .set('pageSize', String(options.pageSize ?? 20));

    const search = options.search?.trim();
    if (search) {
      params = params.set('search', search);
    }

    const category = options.category?.trim();
    if (category) {
      params = params.set('category', category);
    }

    return this.http.get<ProductListResponse>(this.baseUrl, { params });
  }

  /** Loads one product. Other shops' ids return 404. */
  get(id: string) {
    return this.http.get<ProductDto>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateProductRequest) {
    return this.http.post<ProductDto>(this.baseUrl, request);
  }

  /** Catalog update only — does not change stock. Send `expectedVersion` from the last DTO. */
  update(id: string, request: UpdateProductRequest) {
    return this.http.put<ProductDto>(`${this.baseUrl}/${id}`, request);
  }

  /** Manual stock delta. 409 with `code: concurrency` means refresh and retry. */
  adjustStock(id: string, request: AdjustStockRequest) {
    return this.http.post<ProductDto>(`${this.baseUrl}/${id}/stock`, request);
  }
}
