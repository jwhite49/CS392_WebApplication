using CS392_WebApplication.Constants;
using CS392_WebApplication.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CS392_WebApplication.Services;
using CS392_WebApplication.API;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext (use the same key as appsettings.json)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDbContext")
        ?? throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.")));

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

// Add framework services.
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'UserDbContext' not found.")));
builder.Services.AddDbContext<ProductsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'ProductsDbContext' not found.")));
builder.Services.AddDbContext<School_UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'School_UserDbContext' not found.")));
builder.Services.AddDbContext<Product_listDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'Product_listDbContext' not found.")));
builder.Services.AddDbContext<SystemLogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'SystemLogDbContext' not found.")));
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<apiConfig>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<SignInManager<IdentityUser>, LoggingSignInManager>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddSingleton<MongoDBService>();

//If APIController needs ProductService
//→ create one and pass it in

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Privacy");
    options.Conventions.AllowAnonymousToFolder("/ProductPages/Catalog");
    options.Conventions.AllowAnonymousToFolder("/ProductPages/Listing");
    options.Conventions.AllowAnonymousToPage("/Lists/List");
    // Allowing anonymous access to public pages
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Error";
});

var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<GlobalExceptionMiddleware>(); //Log global exceptions to database and redirect to error page


app.UseRouting();

app.UseAuthentication(); // required before Authorization
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

app.Run();
