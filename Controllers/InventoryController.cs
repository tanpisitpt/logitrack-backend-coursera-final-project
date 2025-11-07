using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class InventoryController : ControllerBase
{
  private readonly LogiTrackContext _db;
  private readonly DbSet<InventoryItem> _inventoryContext;

  public InventoryController(LogiTrackContext db)
  {
    _db = db;
    _inventoryContext = db.InventoryItems;
  }

  [HttpGet]
  public async Task<IActionResult> ListInventoryItems()
  {
    var inventory = await _inventoryContext
      .Select(item => new InventoryItemDto(
        item.ItemId,
        item.Name,
        item.Quantity,
        item.Location
      )).ToListAsync();

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

    _inventoryContext.Add(newItem);

    await _db.SaveChangesAsync();

    var newItemDto = new InventoryItemDto(
      newItem.ItemId,
      newItem.Name,
      newItem.Quantity,
      newItem.Location
    );

    return CreatedAtAction(nameof(ListInventoryItems), new { id = newItem.ItemId }, newItemDto);
  }

  [HttpDelete("{id}")]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> DeleteInventoryItem(int id)
  {
    InventoryItem? toDeleteItem = await _inventoryContext.FindAsync(id);

    if (toDeleteItem == null)
      return NotFound($"Item with id {id} is not found.");

    _inventoryContext.Remove(toDeleteItem!);

    await _db.SaveChangesAsync();

    return NoContent();
  }
}
