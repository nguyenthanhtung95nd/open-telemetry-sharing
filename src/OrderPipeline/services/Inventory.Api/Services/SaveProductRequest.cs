using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Services;

public sealed record SaveProductRequest(
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be 100 characters or fewer.")]
    string Name,

    [Range(0.01, 1_000_000, ErrorMessage = "Price must be greater than zero.")]
    decimal Price,

    [Range(0, int.MaxValue, ErrorMessage = "Available quantity cannot be negative.")]
    int AvailableQuantity);
