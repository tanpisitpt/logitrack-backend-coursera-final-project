using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models;

public class InventoryItem
{
  [Key]
  public int ItemId { get; set; }

  [Required]
  public string Name { get; set; }

  [Required]
  public int Quantity { get; set; }

  [Required]
  public string Location { get; set; }

  public List<OrderItem> OrderItems { get; set; } = new();

  public string DisplayInfo()
  {
    return $"Item: {Name} | Quantity: {Quantity} | Location: {Location}";
  }
}

public class InventoryItemCreateDto
{
  public string Name { get; set; }
  public int Quantity { get; set; }
  public string Location { get; set; }
}

public record InventoryItemDto(int ItemId, string Name, int Quantity, string Location);
