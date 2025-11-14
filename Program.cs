//using censudex.Services;

using censudex.src.data;
using censudex.src.models;
using censudex.src.Services;
using Censudex.Services;
using dotenv.net;
using dotenv.net.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
DotEnv.Load();
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseNpgsql(
        EnvReader.GetStringValue("PostgreSQL_Connection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
    );
});
/// <summary>
/// Configures the password hasher service with BCrypt implementation.
/// </summary>
/// <returns></returns>
builder.Services.AddScoped<IPasswordHasher<AppUser>, BCryptPasswordHasher<AppUser>>();
/// <summary>
/// Configures the Kestrel web server to listen on a specified port with HTTP/2 protocol.
/// </summary>
/// <value></value>
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    options.ListenAnyIP(int.Parse(port), listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});
/// <summary>
/// Configures Identity services with password and user options.
/// </summary>
/// <value></value>
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<DataContext>()
  .AddDefaultTokenProviders();
var app = builder.Build();
/// <summary>
/// Creates a scope to apply migrations and seed initial data.
/// </summary>
/// <param name="scope"></param>
/// <returns></returns>
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();
    context.Database.Migrate();
    _ = DataSeeder.Initialize(services);

}
app.UseGrpcWeb();

// Configure the HTTP request pipeline.
app.MapGrpcService<UserService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
