using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Tailor_Management_System.Data;
using Tailor_Management_System.Models;

namespace Tailor_Management_System.Controllers
{
    public class AuthController : Controller
    {
        private readonly TailorDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(TailorDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("token") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    ViewBag.Error = "Invalid email or password";
                    return View();
                }

                var token = GenerateJwtToken(user);

                // Set Session for internal views
                HttpContext.Session.SetString("token", token);
                HttpContext.Session.SetString("userId", user.Id.ToString());
                HttpContext.Session.SetString("fullName", user.FullName);
                HttpContext.Session.SetString("shopName", user.ShopName);
                HttpContext.Session.SetString("email", user.Email);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred during login. Please try again.";
                return View();
            }
        }

        // New API version for React
        [HttpPost("/api/auth/login")]
        public async Task<IActionResult> ApiLogin([FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("email", out var emailProp) || !body.TryGetProperty("password", out var passwordProp))
                    return BadRequest(new { message = "Email and password are required" });

                string email = emailProp.GetString()!;
                string password = passwordProp.GetString()!;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                    return Unauthorized(new { message = "Invalid email or password" });

                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    token,
                    userId = user.Id,
                    fullName = user.FullName,
                    shopName = user.ShopName,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("token") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string shopName, string email, string password)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    ViewBag.Error = "Email already registered";
                    return View();
                }

                var user = new User
                {
                    FullName = fullName,
                    ShopName = shopName,
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(password)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                HttpContext.Session.SetString("token", token);
                HttpContext.Session.SetString("userId", user.Id.ToString());
                HttpContext.Session.SetString("fullName", fullName);
                HttpContext.Session.SetString("shopName", shopName);
                HttpContext.Session.SetString("email", email);

                return RedirectToAction("Index", "Home");
            }
            catch
            {
                ViewBag.Error = "An error occurred during registration.";
                return View();
            }
        }

        // New API version for React
        [HttpPost("/api/auth/register")]
        public async Task<IActionResult> ApiRegister([FromBody] UserRegistrationRequest request)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    return BadRequest(new { message = "Email already registered" });

                var user = new User
                {
                    FullName = request.FullName,
                    ShopName = request.ShopName,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    token,
                    userId = user.Id,
                    fullName = user.FullName,
                    shopName = user.ShopName,
                    email = user.Email
                });
            }
            catch
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        // ── Google One-Tap Sign-In ────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Token))
                    return Json(new { success = false, message = "No credential token received." });

                // Verify the Google ID token by calling Google's tokeninfo endpoint
                var client = _httpClientFactory.CreateClient();
                var verifyUrl = $"https://oauth2.googleapis.com/tokeninfo?id_token={request.Token}";
                var verifyResponse = await client.GetAsync(verifyUrl);

                if (!verifyResponse.IsSuccessStatusCode)
                    return Json(new { success = false, message = "Google token verification failed." });

                var json = await verifyResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Extract claims from the verified token
                var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                var name  = root.TryGetProperty("name",  out var nameProp)  ? nameProp.GetString()  : null;
                var emailVerified = root.TryGetProperty("email_verified", out var evProp) ? evProp.GetString() : "false";

                if (string.IsNullOrWhiteSpace(email) || emailVerified != "true")
                    return Json(new { success = false, message = "Email not verified by Google." });

                // Find or create the local user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    user = new User
                    {
                        FullName  = name ?? email,
                        ShopName  = "My Shop",
                        Email     = email,
                        Password  = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()) // random password
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Issue our own JWT and set session
                var token = GenerateJwtToken(user);
                HttpContext.Session.SetString("token",    token);
                HttpContext.Session.SetString("userId",   user.Id.ToString());
                HttpContext.Session.SetString("fullName", user.FullName);
                HttpContext.Session.SetString("shopName", user.ShopName);
                HttpContext.Session.SetString("email",    user.Email);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[GoogleLogin] Error: " + ex.Message);
                return Json(new { success = false, message = "Server error during Google Sign-In." });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName),
                new Claim("ShopName", user.ShopName)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(jwtSettings["ExpireMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Helper class for registration request
        public class UserRegistrationRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string ShopName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // Helper class for Google login request
        public class GoogleLoginRequest
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}