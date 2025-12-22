using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Quartz;
using FCMDemo.API.Data;
using FCMDemo.API.Services;
using FCMDemo.API.Middleware;
using FCMDemo.API.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FCM Demo API",
        Version = "v1",
        Description = "Firebase Cloud Messaging Demo API with ASP.NET Core 9",
        Contact = new OpenApiContact
        {
            Name = "Firebase Cloud Messaging Demo",
            Url = new Uri("https://github.com/firebase/firebase-admin-dotnet")
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure Entity Framework Core with In-Memory Database
builder.Services.AddDbContext<FCMDbContext>(options =>
{
    options.UseInMemoryDatabase("FCMDemoDB");
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// Register application services
builder.Services.AddSingleton<IFirebaseService, FirebaseService>();
builder.Services.AddScoped<IDeviceTokenService, DeviceTokenService>();
builder.Services.AddScoped<INotificationHistoryService, NotificationHistoryService>();
builder.Services.AddScoped<INotificationSchedulingService, NotificationSchedulingService>();

// Configure Quartz.NET for scheduled notifications
builder.Services.AddQuartz(q =>
{
    // Define the job
    var jobKey = new JobKey("ProcessScheduledNotifications");
    q.AddJob<ProcessScheduledNotificationsJob>(opts => opts.WithIdentity(jobKey));

    // Create a trigger to run every minute
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ProcessScheduledNotifications-trigger")
        .WithCronSchedule("0 * * ? * *") // Every minute at second 0
        .WithDescription("Processes scheduled notifications every minute"));
});

// Add Quartz hosted service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Configure CORS for web client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:8080",
                "http://localhost:5500",
                "http://127.0.0.1:8080",
                "http://127.0.0.1:5500",
                "https://localhost:7001")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FCM Demo API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
        c.DocumentTitle = "FCM Demo API";
    });
}

// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowWebClient");

app.UseAuthorization();

app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FCMDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Logger.LogInformation("FCM Demo API started successfully");
app.Logger.LogInformation("Swagger UI available at: https://localhost:7001");
app.Logger.LogInformation("API Endpoints: /api/devicetokens, /api/notifications, /api/scheduling, /api/history");
app.Logger.LogInformation("Background job: ProcessScheduledNotifications running every minute");

app.Run();
