namespace OrderProcessing.API.Domain.Inventory;

public sealed class InventoryItem
{
    public Guid Id { get; private set; }
    public string ProductId { get; private set; } = default!;
    public int QuantityAvailable { get; private set; }

    private InventoryItem() { }

    public InventoryItem(string productId, int quantityAvailable)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        QuantityAvailable = quantityAvailable;
    }
}
