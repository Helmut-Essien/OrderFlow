/**
 * Client field limits; must stay in sync with `OrderConstraints` and Shared DTO `[StringLength]`.
 */
export const ORDER_FIELD_LIMITS = {
  customerName: 200,
  customerPhone: 50,
  notes: 400,
  maxLines: 50,
  minQuantity: 1,
  maxQuantity: 99_999_999,
  search: 200,
  pageSizeMax: 100
} as const;

/** Mirrors domain `OrderStatus` string names stored by the API. */
export type OrderStatus = 'Pending' | 'Confirmed' | 'Paid' | 'Fulfilled' | 'Cancelled';

/** Mirrors domain `OrderSource`. */
export type OrderSource = 'Manual' | 'WhatsApp';

/** Status chips on the orders list (All is a null filter, not an API status). */
export const ORDER_STATUS_FILTERS: readonly (OrderStatus | null)[] = [
  null,
  'Pending',
  'Confirmed',
  'Paid',
  'Fulfilled',
  'Cancelled'
];

/** Mirrors `OrderLineDto`. Catalog edits after create do not change these snapshots. */
export interface OrderLineDto {
  id: string;
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

/** Mirrors `OrderDto`. `version` is the concurrency token — do not show it in shop-facing copy. */
export interface OrderDto {
  id: string;
  shopId: string;
  customerName: string;
  customerPhone?: string | null;
  notes?: string | null;
  status: OrderStatus | string;
  source: OrderSource | string;
  needsClarification: boolean;
  totalAmount: number;
  version: number;
  createdAt: string;
  updatedAt: string;
  confirmedAt?: string | null;
  paidAt?: string | null;
  fulfilledAt?: string | null;
  cancelledAt?: string | null;
  lines: OrderLineDto[];
}

/** Mirrors `OrderListDto` — list row without lines. */
export interface OrderListDto {
  id: string;
  shopId: string;
  customerName: string;
  customerPhone?: string | null;
  status: OrderStatus | string;
  source: OrderSource | string;
  needsClarification: boolean;
  totalAmount: number;
  lineCount: number;
  version: number;
  createdAt: string;
  updatedAt: string;
}

/** Mirrors `OrderListResponse` / `PagedResult<OrderListDto>`. */
export interface OrderListResponse {
  items: OrderListDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Mirrors `CreateOrderRequest`. Unit prices are taken from the catalog, not this payload. */
export interface CreateOrderRequest {
  customerName: string;
  customerPhone?: string;
  notes?: string;
  confirmImmediately: boolean;
  lines: CreateOrderLineRequest[];
}

/** Mirrors `CreateOrderLineRequest`. */
export interface CreateOrderLineRequest {
  productId: string;
  quantity: number;
}

/** Mirrors `ChangeOrderStatusRequest`. */
export interface ChangeOrderStatusRequest {
  status: OrderStatus;
  expectedVersion: number;
}

/** One status action the shop can take from the current lifecycle state. */
export interface OrderStatusAction {
  /** Button label shown to the shop (not the API enum name). */
  label: string;
  /** Target API status. */
  status: OrderStatus;
  /** `forward` is forest primary; `cancel` is outline danger. */
  kind: 'forward' | 'cancel';
}

/**
 * Allowed next statuses from the API state machine.
 * Fulfilled and Cancelled are terminal; Pending cannot jump to Paid.
 */
export function nextOrderActions(status: string): OrderStatusAction[] {
  switch (status) {
    case 'Pending':
      return [
        { label: 'Confirm order', status: 'Confirmed', kind: 'forward' },
        { label: 'Cancel', status: 'Cancelled', kind: 'cancel' }
      ];
    case 'Confirmed':
      return [
        { label: 'Mark paid', status: 'Paid', kind: 'forward' },
        { label: 'Cancel', status: 'Cancelled', kind: 'cancel' }
      ];
    case 'Paid':
      return [
        { label: 'Mark fulfilled', status: 'Fulfilled', kind: 'forward' },
        { label: 'Cancel', status: 'Cancelled', kind: 'cancel' }
      ];
    default:
      return [];
  }
}

/**
 * Status pill classes. Paid/Fulfilled are success; Cancelled is error; Pending is neutral.
 * Confirmed uses a soft forest tint so it is distinct from Paid without using gold.
 */
export function orderStatusChipClass(status: string): string {
  switch (status) {
    case 'Paid':
    case 'Fulfilled':
      return 'bg-emerald-50 text-emerald-800';
    case 'Confirmed':
      return 'bg-forest/10 text-forest';
    case 'Cancelled':
      return 'bg-red-50 text-red-800';
    default:
      return 'bg-stone-100 text-stone-600';
  }
}
