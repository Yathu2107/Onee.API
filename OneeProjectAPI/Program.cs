using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OneeProject.Database.Context;
using OneeProject.Services.FeServices;
using OneeProject.Services.Services;
using OneeProject.Services.Services.Push;
using OneeProject.Services.Services.Realtime;
using OneeProjectAPI.BackgroundServices;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure MySQL with Pomelo
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Services
builder.Services.AddScoped<SaveFiles>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<WorkerCategoryService>();
builder.Services.AddScoped<JobMatchService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ComplaintService>();
builder.Services.AddScoped<JobRatingService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<DeviceTokenService>();
builder.Services.AddScoped<FcmPushService>();
builder.Services.AddScoped<IPushNotificationSender>(sp => sp.GetRequiredService<FcmPushService>());
builder.Services.AddScoped<NullJobRealtimeNotifier>();
builder.Services.AddScoped<IJobRealtimeNotifier>(sp =>
    new PersistingJobRealtimeNotifier(
        sp.GetRequiredService<NullJobRealtimeNotifier>(),
        sp.GetRequiredService<NotificationService>()));
builder.Services.AddScoped<JobService>();
builder.Services.AddHostedService<OfferTimeoutService>();

// Oneee AI HTTP client
builder.Services.AddHttpClient("OneeeAi", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["OneeeAi:BaseUrl"]?.TrimEnd('/') ?? "http://127.0.0.1:8000";
    var apiKey = config["OneeeAi:ApiKey"] ?? string.Empty;

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// ✅ Configure Identity with custom AppUser
builder.Services.AddIdentity<AppUser, IdentityRole>(config =>
{
    config.Password.RequireDigit = false;
    config.Password.RequiredLength = 4;
    config.Password.RequireLowercase = false;
    config.Password.RequireNonAlphanumeric = false;
    config.Password.RequireUppercase = false;

    config.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/  ";
    config.SignIn.RequireConfirmedAccount = true;
    config.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT Auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        )
    };
});

// Load Serilog from config + enrichers
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add memory caching for token blacklisting and other scenarios
builder.Services.AddMemoryCache();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://192.168.8.104:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Use CORS before routing
app.UseCors("AllowReactApp");

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Dev cert is CN=localhost only — HTTPS to a LAN IP (e.g. 192.168.x.x) causes
// ERR_CERT_COMMON_NAME_INVALID in browsers. Prefer HTTP on :5037 for LAN/admin portal.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
