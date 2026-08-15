using OrderFlow.Shared.DTOs.Common;

namespace OrderFlow.Shared.DTOs.Products;

/// <summary>
/// Paged product list plus shop-wide category chips and the active count used for plan caps.
/// </summary>
public class ProductListResponse : PagedResult<ProductDto>
{
    /// <summary>Distinct categories across the shop, independent of search/page.</summary>
    public required IReadOnlyList<string> Categories { get; set; }

    /// <summary>Active products in the shop. Inactive items do not count toward <c>PlanQuota.MaxProducts</c>.</summary>
    public required int ActiveCount { get; set; }
}
