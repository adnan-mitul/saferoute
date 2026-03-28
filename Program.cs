using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeRoute.Data;
using SafeRoute.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Add Identity
builder.Services.AddIdentity<Users, IdentityRole>(options=>  
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
}
    
    
    )
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var _context = scope.ServiceProvider.GetRequiredService<SafeRoute.Data.AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Apply any pending database migrations automatically (Crucial for Azure deployment)
    try
    {
        _context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }

    // Seed Roles
    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed Admin User
    var adminEmail = "admin@saferoute.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new Users
        {
            FullName = "System Admin",
            Email = adminEmail,
            UserName = adminEmail,
            EmailConfirmed = true,
            JoinedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    // Seed Incident Categories
    if (!_context.IncidentCategories.Any())
    {
        _context.IncidentCategories.AddRange(
            new SafeRoute.Models.IncidentCategory { Name = "Harassment", SeverityWeight = 5 },
            new SafeRoute.Models.IncidentCategory { Name = "Theft / Robbery", SeverityWeight = 4 },
            new SafeRoute.Models.IncidentCategory { Name = "Dark Zone / Poor Lighting", SeverityWeight = 2 },
            new SafeRoute.Models.IncidentCategory { Name = "Suspicious Activity", SeverityWeight = 3 },
            new SafeRoute.Models.IncidentCategory { Name = "Accident", SeverityWeight = 4 },
            new SafeRoute.Models.IncidentCategory { Name = "Eve Teasing", SeverityWeight = 5 },
            new SafeRoute.Models.IncidentCategory { Name = "Drug Activity", SeverityWeight = 4 }
        );
        _context.SaveChanges();
    }

    // Seed Emergency Services
    if (!_context.EmergencyServices.Any())
    {
        _context.EmergencyServices.AddRange(
            new SafeRoute.Models.EmergencyService { Type = "Police", Name = "Dhanmondi Police Station", Latitude = 23.7461, Longitude = 90.3742, ContactNumber = "999" },
            new SafeRoute.Models.EmergencyService { Type = "Police", Name = "Mohammadpur Police Station", Latitude = 23.7661, Longitude = 90.3587, ContactNumber = "999" },
            new SafeRoute.Models.EmergencyService { Type = "Police", Name = "Gulshan Police Station", Latitude = 23.7925, Longitude = 90.4078, ContactNumber = "999" },
            new SafeRoute.Models.EmergencyService { Type = "Hospital", Name = "Dhaka Medical College", Latitude = 23.7256, Longitude = 90.3967, ContactNumber = "02-55165001" },
            new SafeRoute.Models.EmergencyService { Type = "Hospital", Name = "Square Hospital", Latitude = 23.7527, Longitude = 90.3873, ContactNumber = "02-8159457" },
            new SafeRoute.Models.EmergencyService { Type = "Hospital", Name = "United Hospital", Latitude = 23.7913, Longitude = 90.4133, ContactNumber = "09666710710" },
            new SafeRoute.Models.EmergencyService { Type = "Fire", Name = "Dhaka Fire Station (HQ)", Latitude = 23.7330, Longitude = 90.4070, ContactNumber = "199" }
        );
        _context.SaveChanges();
    }
}

app.Run();
