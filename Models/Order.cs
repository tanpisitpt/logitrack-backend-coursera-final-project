using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Models;

public class Order
{
  [Key]
  public int OrderId { get; set; }

  [Required]
  public string CustomerName { get; set; }
  
  [Required]
  public DateTime DatePlaced { get; set; }

  public List<OrderItem> OrderItems { get; set; } = new();

  public void AddItem(InventoryItem item)
  {
    var OrderItem = new OrderItem
    {
      ItemId = item.ItemId,
      InventoryItem = item,
      Quantity = 1,
    };

    OrderItems.Add(OrderItem);
    Console.WriteLine($"Add item #{item.ItemId} successfully.");
  }

  public void RemoveItem(int itemId)
  {
    var item = OrderItems.FirstOrDefault(oi => oi.ItemId == itemId);
    if (item == null)
    {
      Console.WriteLine($"Item #{itemId} doesn't exist in this order!");
      return;
    }
    OrderItems.RemoveAll(orderItem => orderItem.ItemId == itemId);
    Console.WriteLine($"Remove item #{itemId} successfully.");
  }
  
  public string GetOrderSummary()
  {
    return $"Order #{OrderId} for {CustomerName} | Items: {OrderItems.Count} | Placed: {DatePlaced}";
  }
}

public class OrderCreateDto
{
  public string CustomerName { get; set; }
  public List<int> ItemIds { get; set; }
}

public record OrderDto(int OrderId, string CustomerName, DateTime DatePlaced, List<OrderItemDto> OrderItems);
