using AppaRently.Infrastructure;
using AppaRently.Infrastructure.Seeders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAppaRentlyInfrastructureServices(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "AppaRently.Client.Auth";
    options.Cookie.Path = "/";
});

var app = builder.Build();

await app.SeedAppaRentlyAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
