// This is the first file that runs when the app starts.
// It does two jobs:
//   1) Build a list of "services" (tools) the app can use   -> builder.Services.Add...
//   2) Build the "pipeline" every web request walks through -> app.Use...

using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;
using ShiftFlow.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// JOB 1: register services (the "tools" the app can ask for later)
// ---------------------------------------------------------------------------

// Read the database address from appsettings.json (ConnectionStrings:DefaultConnection).
// If it's missing, stop the app right now with a clear message instead of failing
// later in a confusing way.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Tell the app about the database: "it's PostgreSQL, reachable at this address".
// ApplicationDbContext is our own class that represents one conversation with the DB.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// In development, show a helpful page when the database setup is wrong
// (for example, when there are migrations that haven't been applied yet).
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Turn on the whole login system (signup, passwords, sign-in cookies, etc.).
//   <ApplicationUser>                         -> the class that represents a user
//   RequireConfirmedAccount = false           -> don't require email confirmation yet
//   AddEntityFrameworkStores<ApplicationDbContext>() -> store users in our database
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Our own business-logic services. AddScoped = one instance per HTTP request,
// which matches the DbContext lifetime it depends on.
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<DepartmentService>();

// Our own pages use the MVC style.
builder.Services.AddControllersWithViews();

// The ready-made login/register/logout screens from Identity are "Razor Pages",
builder.Services.AddRazorPages();

var app = builder.Build();

// ---------------------------------------------------------------------------
// JOB 2: the request pipeline (checkpoints every request passes through, in order)
// ---------------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    // Adds the "Apply Migrations" button to the DB error page during development.
    app.UseMigrationsEndPoint();
}
else
{
    // In production, send unhandled errors to a friendly page and force HTTPS.
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // checkpoint: "who is this visitor?" (reads the login cookie)
app.UseAuthorization();  // checkpoint: "is this visitor allowed to be here?"

app.MapStaticAssets();

// Map friendly URLs to controllers, e.g. "/Home/Index" -> HomeController.Index().
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Make the Identity login/register/logout pages reachable
// (e.g. /Identity/Account/Login).
app.MapRazorPages();

app.Run();
