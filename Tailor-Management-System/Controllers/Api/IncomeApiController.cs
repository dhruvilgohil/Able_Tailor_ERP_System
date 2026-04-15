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
    [Route("api/income")]
    public class IncomeApiController : ControllerBase
    {
        private readonly TailorDbContext _context;

        public IncomeApiController(TailorDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet]
        public async Task<IActionResult> GetIncomes()
        {
            var incomes = await _context.Incomes
                .Where(i => i.UserId == CurrentUserId)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
            return Ok(incomes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateIncome([FromBody] IncomeItem income)
        {
            income.UserId = CurrentUserId;
            income.CreatedAt = DateTime.UtcNow;
            income.UpdatedAt = DateTime.UtcNow;
            if (income.Date == default) income.Date = DateTime.UtcNow;

            _context.Incomes.Add(income);
            await _context.SaveChangesAsync();

            return Ok(income);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIncome(int id, [FromBody] IncomeItem incomeData)
        {
            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);
            if (income == null) return NotFound();

            income.CustomerName = incomeData.CustomerName;
            income.PaymentMethod = incomeData.PaymentMethod;
            income.Amount = incomeData.Amount;
            income.Date = incomeData.Date;
            income.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(income);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIncome(int id)
        {
            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);
            if (income == null) return NotFound();

            _context.Incomes.Remove(income);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Income deleted" });
        }
    }
}
