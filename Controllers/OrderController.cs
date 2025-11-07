using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogiTrack.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderController : ControllerBase
{
  private readonly LogiTrackContext _db;
  private readonly IMemoryCache _cache;
  private const string OrdersListCacheKey = "orders_list";

  public OrderController(LogiTrackContext db, IMemoryCache cache)
  {
    _db = db;
    _cache = cache;
  }

  [HttpGet]
  public async Task<IActionResult> ListOrders()
  {
    var ordersDto = await _cache.GetOrCreateAsync(OrdersListCacheKey, async entry =>
    {
      entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
      return await _db.Orders
        .AsNoTracking()
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.InventoryItem)
        .Select(o => new OrderDto(
          o.OrderId,
          o.CustomerName,
          o.DatePlaced,
          o.OrderItems.Select(oi => new OrderItemDto(
            oi.ItemId,
            oi.InventoryItem.Name,
            oi.InventoryItem.Quantity
          )).ToList())
        )
        .ToListAsync();
    });

    return Ok(ordersDto);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetOrder(int id)
  {
    var order = await _cache.GetOrCreateAsync($"order_{id}", async entry =>
    {
      var orderDto = await _db.Orders
        .AsNoTracking()
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.InventoryItem)
        .Select(o => new OrderDto(
          o.OrderId,
          o.CustomerName,
          o.DatePlaced,
          o.OrderItems.Select(oi => new OrderItemDto(
            oi.ItemId,
            oi.InventoryItem.Name,
            oi.Quantity)
          ).ToList()
        ))
        .FirstOrDefaultAsync(o => o.OrderId == id);

      if (orderDto == null)
      {
        entry.AbsoluteExpiration = DateTimeOffset.MinValue;
        return null;
      }
      
      entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

      return orderDto;
    });

    if (order == null)
      return NotFound();

    return Ok(order);
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

    _db.Orders.Add(newOrder);
    // newOrder.OrderItems = 

    await _db.SaveChangesAsync();

    await _db.Entry(newOrder)
        .Collection(o => o.OrderItems)
        .Query()
        .Include(oi => oi.InventoryItem)
        .LoadAsync();

    _cache.Remove(OrdersListCacheKey);

    OrderDto newOrderDto = new OrderDto(
      newOrder.OrderId,
      newOrder.CustomerName,
      newOrder.DatePlaced,
      newOrder.OrderItems.Select(oi => new OrderItemDto(
        oi.ItemId,
        oi.InventoryItem.Name,
        oi.Quantity)).ToList()
    );

    return CreatedAtAction(nameof(GetOrder), new { id = newOrder.OrderId }, newOrderDto);
  }

  [HttpDelete("{id}")]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> DeleteOrder(int id)
  {
    var order = await _db.Orders.FindAsync(id);

    if (order == null)
      return NotFound($"Order id #{id} is not founded.");

    _db.Orders.Remove(order);

    await _db.SaveChangesAsync();

    return NoContent();
  }
}