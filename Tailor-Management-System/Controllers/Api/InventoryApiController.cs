using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using Tailor_Management_System.Data;
using Tailor_Management_System.Models;

namespace Tailor_Management_System.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/inventory")]
    public class InventoryApiController : ControllerBase
    {
        private readonly TailorDbContext _context;

        public InventoryApiController(TailorDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet]
        public async Task<IActionResult> GetItems()
        {
            var items = await _context.Inventory
                .Where(i => i.UserId == CurrentUserId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
            var result = items.Select(i => new {
                _id = i.Id, id = i.Id, i.UserId,
                i.ItemName, i.Category, i.StockQty,
                i.StockUnit, i.UnitPrice,
                i.CreatedAt, i.UpdatedAt
            });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] InventoryItem item)
        {
            item.UserId = CurrentUserId;
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            _context.Inventory.Add(item);
            await _context.SaveChangesAsync();
            return Ok(new { _id = item.Id, id = item.Id, item.UserId, item.ItemName, item.Category, item.StockQty, item.StockUnit, item.UnitPrice, item.CreatedAt, item.UpdatedAt });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] InventoryItem itemData)
        {
            var item = await _context.Inventory.FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);
            if (item == null) return NotFound();

            item.ItemName = itemData.ItemName;
            item.Category = itemData.Category;
            item.StockQty = itemData.StockQty;
            item.StockUnit = itemData.StockUnit;
            item.UnitPrice = itemData.UnitPrice;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { _id = item.Id, id = item.Id, item.UserId, item.ItemName, item.Category, item.StockQty, item.StockUnit, item.UnitPrice, item.CreatedAt, item.UpdatedAt });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Inventory.FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);
            if (item == null) return NotFound();

            _context.Inventory.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Item deleted" });
        }
    }
}
