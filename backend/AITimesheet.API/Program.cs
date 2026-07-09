using AITimesheet.API.Interfaces;
using AITimesheet.API.AI;
using AITimesheet.API.Data;
using AITimesheet.API.Integrations;
using AITimesheet.API.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Database (PostgreSQL) ----
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ---- CORS (allow the React dev server) ----
const string CorsPolicy = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Frontend:Url"] ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---- Repositories ----
builder.Services.AddScoped<ITimesheetRepository, TimesheetRepository>();

// ---- Integration services (one HttpClient each) ----
builder.Services.AddHttpClient<IIntegrationService, GitHubIntegrationService>();
builder.Services.AddHttpClient<IIntegrationService, AzureDevOpsIntegrationService>();
builder.Services.AddHttpClient<IIntegrationService, JiraIntegrationService>();
builder.Services.AddHttpClient<IIntegrationService, GraphCalendarService>();

// ---- AI engine ----
builder.Services.AddHttpClient<IAiTimesheetService, OpenAiTimesheetService>();

// ---- Controllers / Swagger ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AI Timesheet Generator API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();