namespace Inventory.Api.Domain;

/// <summary>
/// A sellable item. Inventory.Api owns both its catalog details and its stock level,
/// so a product and the quantity available for it can never disagree.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
}
