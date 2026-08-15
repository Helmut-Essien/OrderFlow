/** Mirrors OrderFlow.Shared.DTOs.Dashboard.LowStockItemDto */
export interface LowStockItemDto {
  id: string;
  name: string;
  sku: string;
  stock: number;
  lowStockThreshold: number;
}

/** Mirrors `DashboardDto`. Sales/orders/WhatsApp stay 0 until those slices exist. */
export interface DashboardDto {
  todaysSales: number;
  orderCount: number;
  /** Unclarified WhatsApp drafts; gold emphasis in the UI when greater than 0. */
  pendingWhatsAppCount: number;
  lowStockCount: number;
  lowStock: LowStockItemDto[];
}
