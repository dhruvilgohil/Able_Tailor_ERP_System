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
    [Route("api/customers")]
    public class CustomersApiController : ControllerBase
    {
        private readonly TailorDbContext _context;

        public CustomersApiController(TailorDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _context.Customers
                .Where(c => c.UserId == CurrentUserId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            var result = customers.Select(c => new {
                _id = c.Id, id = c.Id, c.UserId,
                c.CustomerName, c.ContactNo, c.Address,
                c.CreatedAt, c.UpdatedAt
            });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
        {
            customer.UserId = CurrentUserId;
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return Ok(new { _id = customer.Id, id = customer.Id, customer.UserId, customer.CustomerName, customer.ContactNo, customer.Address, customer.CreatedAt, customer.UpdatedAt });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] Customer customerData)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (customer == null) return NotFound();

            customer.CustomerName = customerData.CustomerName;
            customer.ContactNo = customerData.ContactNo;
            customer.Address = customerData.Address;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { _id = customer.Id, id = customer.Id, customer.UserId, customer.CustomerName, customer.ContactNo, customer.Address, customer.CreatedAt, customer.UpdatedAt });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Customer deleted" });
        }
    }
}
