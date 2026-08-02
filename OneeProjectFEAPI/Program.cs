using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Services.FeServices;
using OneeProject.Services.FeServices.User;
using OneeProject.Services.FeServices.Worker;
using OneeProject.Services.Services;
using OneeProject.Services.Services.Push;
using OneeProject.Services.Services.Realtime;
using OneeProjectFEAPI.BackgroundServices;
using OneeProjectFEAPI.Hubs;
using OneeProjectFEAPI.Realtime;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

builder.Services.AddScoped<FeSaveFiles>();
builder.Services.AddScoped<CommunicationService>();
builder.Services.AddScoped<DeviceTokenService>();
builder.Services.AddScoped<FEAccountService>();
builder.Services.AddScoped<FEWorkerAccountService>();
builder.Services.AddScoped<FEJobService>();
builder.Services.AddScoped<FEWorkerJobService>();
builder.Services.AddScoped<FEAddressService>();
builder.Services.AddScoped<FEWorkerAddressService>();
builder.Services.AddScoped<FENotificationService>();
builder.Services.AddScoped<FEWorkerNotificationService>();
builder.Services.AddScoped<FEComplaintService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ComplaintService>();
builder.Services.AddScoped<JobMatchService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<JobRatingService>();
builder.Services.AddScoped<FcmPushService>();
builder.Services.AddScoped<IPushNotificationSender>(sp => sp.GetRequiredService<FcmPushService>());
builder.Services.AddScoped<SignalRJobNotifier>();
builder.Services.AddScoped<CompositeJobNotifier>();
builder.Services.AddScoped<IJobRealtimeNotifier>(sp =>
    new PersistingJobRealtimeNotifier(
        sp.GetRequiredService<CompositeJobNotifier>(),
        sp.GetRequiredService<NotificationService>()));
builder.Services.AddHostedService<OfferTimeoutService>();

builder.Services.AddHttpClient("OneeeAi", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["OneeeAi:BaseUrl"]?.TrimEnd('/') ?? "http://127.0.0.1:8000";
    var apiKey = config["OneeeAi:ApiKey"] ?? string.Empty;
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    client.Timeout = TimeSpan.FromSeconds(60);
});

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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        )
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/job"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3012")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

CommonResources.SecretKey = builder.Configuration["Security:SecretKey"];

var app = builder.Build();

app.UseCors("AllowReactApp");
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<JobHub>("/hubs/job");

app.Run();
