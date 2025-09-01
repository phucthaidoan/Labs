using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AuditLogging.Infrastructure.Data;
using AuditLogging.Infrastructure.Sinks;
using AuditLogging.Infrastructure.Services;
using AuditLogging.Services;
using AuditLogging.Core.Configuration;
using AuditLogging.Core.Interfaces;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Audit Logging API", 
        Version = "v1",
        Description = "API for GDPR compliant audit logging and compliance reporting"
    });
    
    // Add JWT authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure audit logging options
builder.Services.Configure<AuditLoggingOptions>(
    builder.Configuration.GetSection(AuditLoggingOptions.SectionName));

var auditOptions = builder.Configuration.GetSection(AuditLoggingOptions.SectionName)
    .Get<AuditLoggingOptions>() ?? new AuditLoggingOptions();

// Add Entity Framework
if (auditOptions.DatabaseSink.Enabled)
{
    if (auditOptions.DatabaseSink.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddDbContext<AuditLoggingDbContext>(options =>
            options.UseSqlServer(auditOptions.DatabaseSink.ConnectionString,
                sqlOptions => sqlOptions.CommandTimeout(auditOptions.DatabaseSink.CommandTimeout)));
    }
    else if (auditOptions.DatabaseSink.Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddDbContext<AuditLoggingDbContext>(options =>
            options.UseNpgsql(auditOptions.DatabaseSink.ConnectionString,
                npgsqlOptions => npgsqlOptions.CommandTimeout(auditOptions.DatabaseSink.CommandTimeout)));
    }
    else
    {
        // Use in-memory database for development/testing
        builder.Services.AddDbContext<AuditLoggingDbContext>(options =>
            options.UseInMemoryDatabase("AuditLoggingDb"));
    }
}

// Add Azure Blob Storage client
if (auditOptions.BlobStorageSink.Enabled)
{
    builder.Services.AddSingleton<BlobServiceClient>(provider =>
    {
        var connectionString = auditOptions.BlobStorageSink.ConnectionString;
        if (string.IsNullOrEmpty(connectionString))
        {
            // Use development storage for local development
            connectionString = "UseDevelopmentStorage=true";
        }
        return new BlobServiceClient(connectionString);
    });
}

// Add memory cache
builder.Services.AddMemoryCache();

// Add audit logging services
builder.Services.AddScoped<IDataProtectionService, DataProtectionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IExportService, ExportService>();

// Add audit sinks
if (auditOptions.DatabaseSink.Enabled)
{
    builder.Services.AddScoped<IAuditSink, DatabaseSink>();
}

if (auditOptions.BlobStorageSink.Enabled)
{
    builder.Services.AddScoped<IAuditSink, BlobStorageSink>();
}

// Add authentication and authorization
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:5001"; // Replace with your identity server
        options.RequireHttpsMetadata = false;
        options.Audience = "audit-logging-api";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ComplianceOfficer", policy =>
        policy.RequireRole("Admin", "ComplianceOfficer"));
    
    options.AddPolicy("Auditor", policy =>
        policy.RequireRole("Admin", "ComplianceOfficer", "Auditor"));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audit Logging API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Initialize database
if (auditOptions.DatabaseSink.Enabled)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AuditLoggingDbContext>();
    
    try
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed initial data
        await context.SeedDataAsync();
        
        app.Logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to initialize database");
    }
}

app.Logger.LogInformation("Audit Logging API started successfully");
app.Logger.LogInformation("Environment: {Environment}", auditOptions.Environment);
app.Logger.LogInformation("Application: {ApplicationName}", auditOptions.ApplicationName);

app.Run();
