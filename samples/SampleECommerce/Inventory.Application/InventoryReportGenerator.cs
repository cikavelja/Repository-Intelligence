using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Reports;

/// <summary>
/// Generates inventory reports. Unrelated to payment or order creation flow.
/// </summary>
public sealed class InventoryReportGenerator
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryReportGenerator(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<InventoryReport> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var items = await _inventoryRepository.GetAllAsync(cancellationToken);
        var lowStock = new List<InventoryItem>();
        decimal totalValue = 0;

        foreach (var item in items)
        {
            totalValue += item.Quantity * item.UnitPrice;
            if (item.Quantity < item.ReorderThreshold)
                lowStock.Add(item);
        }

        return new InventoryReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalItems = items.Count,
            TotalStockValue = totalValue,
            LowStockItems = lowStock
        };
    }
}

public sealed class InventoryItem
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int ReorderThreshold { get; set; }
}

public sealed class InventoryReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalStockValue { get; set; }
    public IReadOnlyList<InventoryItem> LowStockItems { get; set; } = [];
}

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default);
}
