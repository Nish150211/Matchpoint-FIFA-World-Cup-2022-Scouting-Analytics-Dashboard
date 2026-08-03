using Microsoft.EntityFrameworkCore;
using WorldCupAnalytics.Data;

var builder = WebApplication.CreateBuilder(args);

var fixedCulture = new System.Globalization.CultureInfo("en-US");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = fixedCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = fixedCulture;

var connectionString = builder.Configuration.GetConnectionString("WorldCupDb");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
