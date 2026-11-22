using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProofTrail.IdentityStores;
using ProofTrail.Repositories;
using ProofTrail.Data;
using ProofTrail.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<SqlDatabase>();
builder.Services.AddScoped<IInstitutionRepository, InstitutionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICredentialRepository, CredentialRepository>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o => {
    o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 8;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}).AddUserStore<DapperUserStore>().AddRoleStore<DapperRoleStore>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o => {
    o.LoginPath = "/Identity/Account/Login";
    o.AccessDeniedPath = "/Identity/Account/AccessDenied";
    o.Cookie.HttpOnly = true;
    o.ExpireTimeSpan = TimeSpan.FromHours(2);
    o.SlidingExpiration = true;
});
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddRazorPages();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<SqlDatabase>();
    await DatabaseSetup.InitializeAsync(db);
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}
app.Run();
public partial class Program { }
