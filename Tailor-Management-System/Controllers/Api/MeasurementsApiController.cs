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
    [Route("api/measurements")]
    public class MeasurementsApiController : ControllerBase
    {
        private readonly TailorDbContext _context;

        public MeasurementsApiController(TailorDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet]
        public async Task<IActionResult> GetMeasurements([FromQuery] int? customerId)
        {
            var query = _context.Measurements
                .Include(m => m.Customer)
                .Where(m => m.UserId == CurrentUserId);

            if (customerId.HasValue)
            {
                query = query.Where(m => m.CustomerId == customerId.Value);
            }

            var measurements = await query.OrderByDescending(m => m.RecordedDate).ToListAsync();
            
            // Map to include Customer object as expected by React
            var result = measurements.Select(m => new {
                m.Id,
                m.UserId,
                m.CustomerId,
                customerName = m.Customer?.CustomerName, // React expects this for population
                m.Title,
                m.Type,
                shirt = m.ShirtData != null ? JsonSerializer.Deserialize<JsonElement>(m.ShirtData) : (object?)null,
                pant = m.PantData != null ? JsonSerializer.Deserialize<JsonElement>(m.PantData) : (object?)null,
                m.RecordedDate,
                m.CreatedAt,
                m.UpdatedAt
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMeasurement([FromBody] JsonElement body)
        {
            try {
                var measurement = new Measurement
                {
                    UserId = CurrentUserId,
                    CustomerId = body.TryGetProperty("customerId", out var cid) && cid.ValueKind == JsonValueKind.Number ? cid.GetInt32() : (cid.ValueKind == JsonValueKind.String && int.TryParse(cid.GetString(), out var parsedCid) ? parsedCid : 0),
                    Title = body.GetProperty("title").GetString()!,
                    Type = body.TryGetProperty("type", out var t) ? t.GetString() : null,
                    ShirtData = body.TryGetProperty("shirt", out var s) ? s.GetRawText() : null,
                    PantData = body.TryGetProperty("pant", out var p) ? p.GetRawText() : null,
                    RecordedDate = body.TryGetProperty("recordedDate", out var rd) ? rd.GetDateTime() : DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Measurements.Add(measurement);
                await _context.SaveChangesAsync();

                return Ok(measurement);
            } catch (Exception ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMeasurement(int id, [FromBody] JsonElement body)
        {
            var measurement = await _context.Measurements.FirstOrDefaultAsync(m => m.Id == id && m.UserId == CurrentUserId);
            if (measurement == null) return NotFound();

            if (body.TryGetProperty("title", out var t)) measurement.Title = t.GetString()!;
            if (body.TryGetProperty("type", out var ty)) measurement.Type = ty.GetString();
            if (body.TryGetProperty("shirt", out var s)) measurement.ShirtData = s.GetRawText();
            if (body.TryGetProperty("pant", out var p)) measurement.PantData = p.GetRawText();
            
            measurement.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(measurement);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeasurement(int id)
        {
            var measurement = await _context.Measurements.FirstOrDefaultAsync(m => m.Id == id && m.UserId == CurrentUserId);
            if (measurement == null) return NotFound();

            _context.Measurements.Remove(measurement);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Measurement deleted" });
        }
    }
}
