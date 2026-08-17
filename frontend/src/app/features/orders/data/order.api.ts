import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import {
  ChangeOrderStatusRequest,
  CreateOrderRequest,
  OrderDto,
  OrderListResponse
} from './order.models';

/**
 * HTTP client for shop order endpoints.
 * Mirrors `OrdersController`; DTOs match `OrderFlow.Shared/DTOs/Orders`.
 */
@Injectable({ providedIn: 'root' })
export class OrderApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/orders`;

  /**
   * Lists orders for the current shop.
   * @param options `pageSize` is 1–100 (API default 20). `status` is an `OrderStatus` name.
   */
  list(options: {
    search?: string;
    status?: string;
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

    const status = options.status?.trim();
    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<OrderListResponse>(this.baseUrl, { params });
  }

  /** Loads one order with line snapshots. Other shops' ids return 404. */
  get(id: string) {
    return this.http.get<OrderDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * Creates a manual order. Prices are snapshotted from the catalog.
   * `confirmImmediately` reserves stock in the same request.
   */
  create(request: CreateOrderRequest) {
    return this.http.post<OrderDto>(this.baseUrl, request);
  }

  /**
   * Moves status along the lifecycle. Send `expectedVersion` from the last DTO.
   * 409 with `code: concurrency` means refresh and retry.
   */
  changeStatus(id: string, request: ChangeOrderStatusRequest) {
    return this.http.post<OrderDto>(`${this.baseUrl}/${id}/status`, request);
  }
}
