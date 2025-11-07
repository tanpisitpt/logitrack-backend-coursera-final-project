using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderController : ControllerBase
{
  private readonly LogiTrackContext _db;
  private readonly DbSet<Order> _orders;

  public OrderController(LogiTrackContext db)
  {
    _db = db;
    _orders = _db.Orders;
  }

  [HttpGet]
  public async Task<IActionResult> ListOrders()
  {
    var orders = await _orders.Include(o => o.OrderItems).ThenInclude(oi => oi.InventoryItem).ToListAsync();

    var ordersDto = orders.Select(o => new OrderDto(
      o.OrderId,
      o.CustomerName,
      o.DatePlaced,
      o.OrderItems.Select(oi => new OrderItemDto(
        oi.ItemId,
        oi.InventoryItem.Name,
        oi.InventoryItem.Quantity
      )).ToList())
    );

    return Ok(ordersDto);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetOrder(int id)
  {
    var order = await _orders
      .Include(o => o.OrderItems)
      .ThenInclude(oi => oi.InventoryItem)
      .FirstOrDefaultAsync(o => o.OrderId == id);

    if (order == null)
      return NotFound();

    var dto = new OrderDto(
      order.OrderId,
      order.CustomerName,
      order.DatePlaced,
      order.OrderItems.Select(oi => new OrderItemDto(
        oi.ItemId,
        oi.InventoryItem.Name,
        oi.Quantity)
      ).ToList()
    );

    return Ok(dto);
  }

  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto OrderCreateDto)
  {
    List<int> itemIds = OrderCreateDto.ItemIds;
    List<InventoryItem> items = await _db.InventoryItems.Where(i => itemIds.Contains(i.ItemId)).ToListAsync();

    var newOrder = new Order
    {
      CustomerName = OrderCreateDto.CustomerName,
      DatePlaced = DateTime.UtcNow,
    };

    foreach (var item in items)
    {
      newOrder.AddItem(item);
    }

    _orders.Add(newOrder);
    // newOrder.OrderItems = 

    await _db.SaveChangesAsync();

    OrderDto newOrderDto = new OrderDto(
      newOrder.OrderId,
      newOrder.CustomerName,
      newOrder.DatePlaced,
      newOrder.OrderItems.Select(oi => new OrderItemDto(oi.ItemId, oi.InventoryItem.Name, oi.Quantity)).ToList()
    );

    return CreatedAtAction(nameof(GetOrder), new { id = newOrder.OrderId }, newOrderDto);
  }

  [HttpDelete("{id}")]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> DeleteOrder(int id)
  {
    var order = await _orders.FindAsync(id);

    if (order == null)
      return NotFound($"Order id #{id} is not founded.");

    _orders.Remove(order);

    await _db.SaveChangesAsync();

    return NoContent();
  }
}