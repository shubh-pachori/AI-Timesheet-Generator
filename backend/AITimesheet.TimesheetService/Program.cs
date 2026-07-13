using AITimesheet.TimesheetService.Data;
using AITimesheet.TimesheetService.RepositoryLayer.Implementations;
using AITimesheet.TimesheetService.RepositoryLayer.Interfaces;
using AITimesheet.TimesheetService.ServiceLayer.Clients;
using AITimesheet.TimesheetService.ServiceLayer.Implementations;
using AITimesheet.TimesheetService.ServiceLayer.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---- Database (PostgreSQL) ----
builder.Services.AddDbContext<TimesheetDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ---- Repositories ----
builder.Services.AddScoped<ITimesheetRepository, TimesheetRepository>();
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();

// ---- Internal Service Clients ----
builder.Services.AddHttpClient<IdentityServiceClient>();

// ---- Integration services (one HttpClient each) ----
builder.Services.AddHttpClient<IIntegrationService, GitHubIntegrationService>();
builder.Services.AddHttpClient<IIntegrationService, AzureDevOpsIntegrationService>();
builder.Services.AddHttpClient<IIntegrationService, JiraIntegrationService>();
builder.Services.AddHttpClient<IIntegrationService, GraphCalendarService>();

// ---- AI engine ----
builder.Services.AddHttpClient<IAiTimesheetService, OpenAiTimesheetService>();

// ---- JWT Bearer Authentication (Validation only) ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Timesheet Service API", Version = "v1" });
});

builder.Services.AddAuthorization();

var app = builder.Build();

// ---- Database Migrations Auto-Apply ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TimesheetDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
