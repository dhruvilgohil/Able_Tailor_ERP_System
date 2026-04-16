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
    [Route("api/tailors")]
    public class TailorsApiController : ControllerBase
    {
        private readonly TailorDbContext _context;

        public TailorsApiController(TailorDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet]
        public async Task<IActionResult> GetTailors()
        {
            var tailors = await _context.Tailors
                .Where(t => t.UserId == CurrentUserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            var result = tailors.Select(t => new {
                _id = t.Id, id = t.Id, t.UserId,
                t.Name, t.Phone, t.Address,
                t.PaymentType, t.Salary, t.ContractRate,
                t.CreatedAt, t.UpdatedAt
            });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTailor([FromBody] Tailor tailor)
        {
            tailor.UserId = CurrentUserId;
            tailor.CreatedAt = DateTime.UtcNow;
            tailor.UpdatedAt = DateTime.UtcNow;

            _context.Tailors.Add(tailor);
            await _context.SaveChangesAsync();
            return Ok(new { _id = tailor.Id, id = tailor.Id, tailor.UserId, tailor.Name, tailor.Phone, tailor.Address, tailor.PaymentType, tailor.Salary, tailor.ContractRate, tailor.CreatedAt, tailor.UpdatedAt });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTailor(int id, [FromBody] Tailor tailorData)
        {
            var tailor = await _context.Tailors.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
            if (tailor == null) return NotFound();

            tailor.Name = tailorData.Name;
            tailor.Phone = tailorData.Phone;
            tailor.Address = tailorData.Address;
            tailor.PaymentType = tailorData.PaymentType;
            tailor.Salary = tailorData.Salary;
            tailor.ContractRate = tailorData.ContractRate;
            tailor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { _id = tailor.Id, id = tailor.Id, tailor.UserId, tailor.Name, tailor.Phone, tailor.Address, tailor.PaymentType, tailor.Salary, tailor.ContractRate, tailor.CreatedAt, tailor.UpdatedAt });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTailor(int id)
        {
            var tailor = await _context.Tailors.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
            if (tailor == null) return NotFound();

            // SOFT REMOVAL: Just unassign the tailor from their orders
            var orders = await _context.Orders.Where(o => o.AssignedTailorId == id).ToListAsync();
            foreach (var order in orders) {
                order.AssignedTailorId = null;
            }

            _context.Tailors.Remove(tailor);
            await _context.SaveChangesAsync();
            return Ok(new { _id = id, id = id, message = "Tailor deleted" });
        }
    }
}
