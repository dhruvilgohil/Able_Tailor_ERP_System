using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Tailor_Management_System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
var dbPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "TailorDb.db");
builder.Services.AddDbContext<TailorDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));


// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Add session support (for internal views)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});


var app = builder.Build();

// Automatically apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TailorDbContext>();
        context.Database.Migrate();
        Console.WriteLine("[Database] Auto-migration successful.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database] Error applying migrations: {ex.Message}");
    }
}

// Configure the app to listen on port 5000
app.Urls.Add("http://localhost:5000");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection(); // Removed to prevent SSL protocol errors on local HTTP port
app.UseStaticFiles(); // Serve React static files from wwwroot
app.UseRouting();
app.UseCors("AllowAll");


app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

// Enable MVC Routing for standard views
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map fallback for React SPA routing - Disabled to use MVC views
// app.MapFallbackToFile("index.html");


app.Run();
