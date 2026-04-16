using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Tailor_Management_System.Data;
using Tailor_Management_System.Models;

namespace Tailor_Management_System.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/orders")]
    public class OrdersApiController : ControllerBase
    {
        private readonly TailorDbContext _context;

        public OrdersApiController(TailorDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.AssignedTailor)
                .Include(o => o.Measurement)
                .Where(o => o.UserId == CurrentUserId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var result = orders.Select(o => BuildOrderResponse(o));
            return Ok(result);
        }

        // Builds the enriched order response shape the React frontend expects
        private object BuildOrderResponse(Order o)
        {
            return new {
                id = o.Id,
                _id = o.Id,
                userId = o.UserId,
                customerId = new { _id = o.CustomerId, id = o.CustomerId, customerName = o.Customer?.CustomerName, contactNo = o.Customer?.ContactNo },
                measurementId = o.Measurement != null ? (object)new { _id = o.MeasurementId, id = o.MeasurementId, title = o.Measurement.Title } : null,
                services = o.Services != null ? JsonSerializer.Deserialize<JsonElement>(o.Services) : (object?)null,
                itemsUsed = o.ItemsUsed != null ? JsonSerializer.Deserialize<JsonElement>(o.ItemsUsed) : (object?)null,
                status = o.Status,
                paymentMethod = o.PaymentMethod,
                paymentExpectedBy = o.PaymentExpectedBy,
                assignedTailorId = o.AssignedTailorId,
                assignedTailorInt = o.AssignedTailorId,
                // Return as object with name so React can display tailor name without a separate lookup
                assignedTailor = o.AssignedTailor != null 
                    ? (object)new { _id = o.AssignedTailorId, id = o.AssignedTailorId, name = o.AssignedTailor.Name, phone = o.AssignedTailor.Phone } 
                    : (o.AssignedTailorId != null ? (object)new { _id = o.AssignedTailorId, id = o.AssignedTailorId, name = (string?)null } : null),
                tailorContractPrice = o.TailorContractPrice,
                calculatedTotal = o.CalculatedTotal,
                userDefinedTotal = o.UserDefinedTotal,
                targetDeliveryDate = o.TargetDeliveryDate,
                totalAmount = o.TotalAmount,
                createdAt = o.CreatedAt,
                updatedAt = o.UpdatedAt
            };
        }


        private DateTime? ParseDateSafe(JsonElement body, string prop)
        {
            if (body.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String)
            {
                var val = el.GetString();
                if (!string.IsNullOrWhiteSpace(val) && DateTime.TryParse(val, out var dt))
                    return dt;
            }
            return null;
        }

        private int? ParseIntSafe(JsonElement body, string prop)
        {
            // Case-insensitive search for the property first
            JsonElement el = default;
            bool found = false;
            foreach (var property in body.EnumerateObject())
            {
                if (string.Equals(property.Name, prop, StringComparison.OrdinalIgnoreCase))
                {
                    el = property.Value;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                if (el.ValueKind == JsonValueKind.Number) return el.GetInt32();
                if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var i)) return i;
                if (el.ValueKind == JsonValueKind.Object)
                {
                    // Case-insensitive search inside the object for id/_id
                    foreach (var subProp in el.EnumerateObject())
                    {
                        if (string.Equals(subProp.Name, "id", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(subProp.Name, "_id", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(subProp.Name, "tailorId", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(subProp.Name, "customerId", StringComparison.OrdinalIgnoreCase))
                        {
                            if (subProp.Value.ValueKind == JsonValueKind.Number) return subProp.Value.GetInt32();
                            if (subProp.Value.ValueKind == JsonValueKind.String && int.TryParse(subProp.Value.GetString(), out var idStr)) return idStr;
                        }
                    }
                }
            }
            return null;
        }

        private decimal ParseDecimalSafe(JsonElement body, string prop)
        {
            if (body.TryGetProperty(prop, out var el))
            {
                if (el.ValueKind == JsonValueKind.Number) return el.GetDecimal();
                if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), out var d)) return d;
            }
            return 0;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] JsonElement body)
        {
            try
            {
                Console.WriteLine($"[OrdersApi] CreateOrder received: {body.GetRawText()}");

                // Fallback parsing for common property names (Case-insensitive via ParseIntSafe)
                int? tailorId = ParseIntSafe(body, "assignedTailor") 
                             ?? ParseIntSafe(body, "assignedTailorId") 
                             ?? ParseIntSafe(body, "assignedTailorInt") 
                             ?? ParseIntSafe(body, "tailorId")
                             ?? ParseIntSafe(body, "assigned_tailor")
                             ?? ParseIntSafe(body, "tailor_id");

                int? customerId = ParseIntSafe(body, "customerId") 
                               ?? ParseIntSafe(body, "customerIdId") 
                               ?? ParseIntSafe(body, "customer") 
                               ?? ParseIntSafe(body, "customer_id");

                if (customerId == null || customerId <= 0)
                {
                    return BadRequest(new { message = "Customer is required. Please select a valid customer." });
                }

                int? measurementId = ParseIntSafe(body, "measurementId") 
                                  ?? ParseIntSafe(body, "measurement") 
                                  ?? ParseIntSafe(body, "measurementIdId")
                                  ?? ParseIntSafe(body, "measurement_id");

                Console.WriteLine($"[OrdersApi] Parsed IDs -> Customer: {customerId}, Tailor: {tailorId}, Measurement: {measurementId}");

                var order = new Order
                {
                    UserId = CurrentUserId,
                    CustomerId = customerId.Value,
                    MeasurementId = measurementId,
                    Services = body.TryGetProperty("services", out var s) ? s.GetRawText() : null,
                    ItemsUsed = body.TryGetProperty("itemsUsed", out var i) ? i.GetRawText() : null,
                    Status = body.TryGetProperty("status", out var st) ? (st.GetString() ?? "Pending") : "Pending",
                    PaymentMethod = body.TryGetProperty("paymentMethod", out var pm) ? (pm.GetString() ?? "Pending") : "Pending",
                    PaymentExpectedBy = ParseDateSafe(body, "paymentExpectedBy"),
                    AssignedTailorId = (tailorId == 0) ? null : tailorId, // Handle cases where 0 means unassigned
                    TailorContractPrice = ParseDecimalSafe(body, "tailorContractPrice"),
                    CalculatedTotal = ParseDecimalSafe(body, "calculatedTotal"),
                    UserDefinedTotal = ParseDecimalSafe(body, "userDefinedTotal"),
                    TargetDeliveryDate = ParseDateSafe(body, "targetDeliveryDate"),
                    TotalAmount = ParseDecimalSafe(body, "totalAmount"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[OrdersApi] Order created with ID: {order.Id}, TailorId: {order.AssignedTailorId}");

                // Reload with navigation properties so React gets the full enriched response
                var created = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.AssignedTailor)
                    .Include(o => o.Measurement)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                // Trigger Auto Income
                await HandleAutoIncome(order);

                return Ok(BuildOrderResponse(created!));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrdersApi] CreateOrder Error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] JsonElement body)
        {
            try
            {
                Console.WriteLine($"[OrdersApi] UpdateOrder {id} received: {body.GetRawText()}");
                var order = await _context.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id && o.UserId == CurrentUserId);
                if (order == null) return NotFound();

                // Core fields with fallback names
                var customerId = ParseIntSafe(body, "customerId") ?? ParseIntSafe(body, "customerIdId") ?? ParseIntSafe(body, "customer") ?? ParseIntSafe(body, "customer_id");
                if (customerId != null && customerId > 0) order.CustomerId = customerId.Value;
                else if (customerId == 0) return BadRequest(new { message = "Invalid Customer selection." });

                var measurementId = ParseIntSafe(body, "measurementId") ?? ParseIntSafe(body, "measurement") ?? ParseIntSafe(body, "measurement_id");
                if (measurementId != null) order.MeasurementId = measurementId;
                
                // Content fields
                if (body.TryGetProperty("services", out var s)) order.Services = s.ValueKind == JsonValueKind.Null ? null : s.GetRawText();
                if (body.TryGetProperty("itemsUsed", out var i)) order.ItemsUsed = i.ValueKind == JsonValueKind.Null ? null : i.GetRawText();
                
                // Status and Metadata
                if (body.TryGetProperty("status", out var st)) order.Status = st.GetString() ?? "Pending";
                if (body.TryGetProperty("paymentMethod", out var pm)) order.PaymentMethod = pm.GetString() ?? "Pending";
                if (body.TryGetProperty("paymentExpectedBy", out _)) order.PaymentExpectedBy = ParseDateSafe(body, "paymentExpectedBy");
                
                // Tailor Assignment (Extended fallback check)
                int? tailorId = ParseIntSafe(body, "assignedTailor") 
                             ?? ParseIntSafe(body, "assignedTailorId") 
                             ?? ParseIntSafe(body, "assignedTailorInt") 
                             ?? ParseIntSafe(body, "tailorId")
                             ?? ParseIntSafe(body, "assigned_tailor")
                             ?? ParseIntSafe(body, "tailor_id");
                
                if (tailorId != null || body.GetRawText().Contains("assignedTailor", StringComparison.OrdinalIgnoreCase))
                {
                    order.AssignedTailorId = (tailorId == 0) ? null : tailorId;
                    Console.WriteLine($"[OrdersApi] Updated TailorId to: {order.AssignedTailorId}");
                }

                if (body.TryGetProperty("tailorContractPrice", out _)) order.TailorContractPrice = ParseDecimalSafe(body, "tailorContractPrice");

                // Financials
                if (body.TryGetProperty("calculatedTotal", out _)) order.CalculatedTotal = ParseDecimalSafe(body, "calculatedTotal");
                if (body.TryGetProperty("userDefinedTotal", out _)) order.UserDefinedTotal = ParseDecimalSafe(body, "userDefinedTotal");
                if (body.TryGetProperty("totalAmount", out _)) order.TotalAmount = ParseDecimalSafe(body, "totalAmount");
                
                // Dates
                if (body.TryGetProperty("targetDeliveryDate", out _)) order.TargetDeliveryDate = ParseDateSafe(body, "targetDeliveryDate");

                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                Console.WriteLine($"[OrdersApi] Order {id} updated. TailorId: {order.AssignedTailorId}");

                // Reload with all navigation properties so React gets enriched response
                var updated = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.AssignedTailor)
                    .Include(o => o.Measurement)
                    .FirstOrDefaultAsync(o => o.Id == id);

                // Trigger Auto Income
                await HandleAutoIncome(order);

                return Ok(BuildOrderResponse(updated!));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrdersApi] UpdateOrder Error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task HandleAutoIncome(Order order)
        {
            decimal amountPaid = order.UserDefinedTotal > 0 ? order.UserDefinedTotal : (order.CalculatedTotal > 0 ? order.CalculatedTotal : order.TotalAmount);
            
            if (amountPaid > 0 && (order.PaymentMethod == "Cash" || order.PaymentMethod == "Online"))
            {
                var existingIncome = await _context.Incomes.FirstOrDefaultAsync(i => i.OrderId == order.Id && i.UserId == order.UserId);
                
                if (existingIncome == null)
                {
                    var customer = await _context.Customers.FindAsync(order.CustomerId);
                    var newIncome = new IncomeItem
                    {
                        UserId = order.UserId,
                        CustomerName = customer?.CustomerName ?? "Unknown Order Customer",
                        OrderId = order.Id,
                        PaymentMethod = order.PaymentMethod,
                        Amount = amountPaid,
                        Date = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Incomes.Add(newIncome);
                }
                else
                {
                    existingIncome.Amount = amountPaid;
                    existingIncome.PaymentMethod = order.PaymentMethod;
                    existingIncome.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == CurrentUserId);
            if (order == null) return NotFound();

            // Manually clean up any related Income record to avoid FK errors in SQLite
            var incomes = await _context.Incomes.Where(i => i.OrderId == id).ToListAsync();
            if (incomes.Any()) {
                _context.Incomes.RemoveRange(incomes);
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return Ok(new { _id = id, id = id, message = "Order deleted" });
        }
    }
}
