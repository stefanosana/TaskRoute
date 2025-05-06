using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskRoute;
using TaskRoute.Data;
using TaskRoute.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configurazione di Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Non richiedere conferma dell'email
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>() // Supporto ai ruoli
.AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<GeminiService>();

// Aggiunta del servizio HttpClient (se necessario)
builder.Services.AddScoped<HttpClient>();

// Aggiunta del supporto per Razor Pages
builder.Services.AddRazorPages();

builder.Services.AddHostedService<CompletedTaskCleanupService>();

builder.Services.AddHttpClient();

// Configure Antiforgery (important for POST requests from JS)
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

var app = builder.Build();

// Applica le migrazioni e inizializza i dati di seed durante l'avvio
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate(); // Applica automaticamente le migrazioni

        // Inizializza i dati di seed
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Si è verificato un errore durante l'inizializzazione del database.");
    }
}

// Middleware per la gestione delle richieste
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Abilita l'autenticazione
app.UseAuthorization();  // Abilita l'autorizzazione

app.MapRazorPages();

app.Run();