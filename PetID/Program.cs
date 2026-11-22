using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetID.Data;
using PetID.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PetIdDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PetID")
        ?? throw new InvalidOperationException("No se configuró la conexión SQL PetID.")));
builder.Services.AddScoped<IPasswordHasher<PetUser>, PasswordHasher<PetUser>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/Login";
    });

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PetIdDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    if (app.Environment.IsDevelopment() && !await dbContext.Users.AnyAsync())
    {
        var user = new PetUser
        {
            Email = "admin@petid.com",
            NormalizedEmail = "ADMIN@PETID.COM"
        };
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<PetUser>>();
        user.PasswordHash = passwordHasher.HashPassword(user, "admin123");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// Serve static files from wwwroot (images, css, js)
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
