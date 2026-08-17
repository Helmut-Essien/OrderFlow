/** Mirrors OrderFlow.Shared.DTOs.Dashboard.LowStockItemDto */
export interface LowStockItemDto {
  id: string;
  name: string;
  sku: string;
  stock: number;
  lowStockThreshold: number;
}

/** Mirrors `DashboardOrderDto` — compact recent-order row. */
export interface DashboardOrderDto {
  id: string;
  customerName: string;
  status: string;
  source: string;
  totalAmount: number;
  createdAt: string;
}

/** Mirrors `DashboardDto`. `pendingWhatsAppCount` stays 0 until the WhatsApp slice. */
export interface DashboardDto {
  todaysSales: number;
  orderCount: number;
  /** Unclarified WhatsApp drafts; gold emphasis in the UI when greater than 0. */
  pendingWhatsAppCount: number;
  lowStockCount: number;
  lowStock: LowStockItemDto[];
  /** Newest first, capped at 10. */
  recentOrders: DashboardOrderDto[];
}
