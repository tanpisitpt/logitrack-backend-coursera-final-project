using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models;

public class OrderItem
{
  [Key]
  public int Id { get; set; }

  [Required]
  public int OrderId { get; set; }

  [ForeignKey("OrderId")]
  public Order Order { get; set; }

  [Required]
  public int ItemId { get; set; }

  [ForeignKey("ItemId")]
  public InventoryItem InventoryItem { get; set; }

  public int Quantity { get; set; }
}

public record OrderItemDto(int ItemId, string ItemName, int Quantity);
