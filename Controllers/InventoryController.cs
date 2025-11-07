using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogiTrack.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class InventoryController : ControllerBase
{
  private readonly LogiTrackContext _db;
  private readonly IMemoryCache _cache;
  private const string InventoryCacheKey = "inventory_items";


  public InventoryController(LogiTrackContext db, IMemoryCache cache)
  {
    _db = db;
    _cache = cache;
  }

  [HttpGet]
  public async Task<IActionResult> ListInventoryItems()
  {
    var inventory = await _cache.GetOrCreateAsync(InventoryCacheKey, async entry =>
    {
      entry.SlidingExpiration = TimeSpan.FromSeconds(30);

      return await _db.InventoryItems
        .AsNoTracking()
        .Select(item => new InventoryItemDto(
          item.ItemId,
          item.Name,
          item.Quantity,
          item.Location
        )).ToListAsync();
    });

    return Ok(inventory);
  }

  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<ActionResult<InventoryItem>> CreateInventoryItem([FromBody] InventoryItemCreateDto InventoryItemCreateDto)
  {
    var newItem = new InventoryItem
    {
      Name = InventoryItemCreateDto.Name,
      Quantity = InventoryItemCreateDto.Quantity,
      Location = InventoryItemCreateDto.Location,
    };

    _db.InventoryItems.Add(newItem);

    await _db.SaveChangesAsync();

    var newItemDto = new InventoryItemDto(
      newItem.ItemId,
      newItem.Name,
      newItem.Quantity,
      newItem.Location
    );

    _cache.Remove(InventoryCacheKey);

    return CreatedAtAction(nameof(ListInventoryItems), new { id = newItem.ItemId }, newItemDto);
  }

  [HttpDelete("{id}")]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> DeleteInventoryItem(int id)
  {
    InventoryItem? toDeleteItem = await _db.InventoryItems.FindAsync(id);

    if (toDeleteItem == null)
      return NotFound($"Item with id {id} is not found.");

    _db.InventoryItems.Remove(toDeleteItem!);

    await _db.SaveChangesAsync();

    return NoContent();
  }
}
