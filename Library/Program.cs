using Library.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnSignedIn = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.GetUserAsync(context.Principal);
        if (user != null && !await userManager.IsInRoleAsync(user, "User"))
        {
            await userManager.AddToRoleAsync(user, "User");
        }
    };
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Ensure that the User role exists and create it if not
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Utwórz rolę "Admin", jeśli nie istnieje
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Utwórz rolę "User", jeśli nie istnieje
    if (!await roleManager.RoleExistsAsync("User"))
    {
        await roleManager.CreateAsync(new IdentityRole("User"));
    }

    // Utwórz konto admina
    var adminUser = new IdentityUser { UserName = "admin@example.com", Email = "admin@example.com" };
    var result = await userManager.CreateAsync(adminUser, "Password123!"); 

    // Jeśli utworzenie konta się powiodło, dodaj rolę "Admin"
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Utwórz zwykłe konto użytkownika
    var regularUser = new IdentityUser { UserName = "user@example.com", Email = "user@example.com" };
    result = await userManager.CreateAsync(regularUser, "Password1!"); 

    // Jeśli utworzenie konta się powiodło, dodaj rolę "User"
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(regularUser, "User");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Catalog}/{id?}");
app.MapRazorPages();

app.Run();
