using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using NotesApi.Data;
using NotesApi.Models.Settings;
using NotesApi.Services;
using NotesApi.Utils;
using Npgsql;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var appSettingConfiguration = builder.Configuration.GetSection(AppSettings.SETTING_NAME).Get<AppSettings>();

// Add services to the container.
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.UseNodaTime();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(dataSource,
        builder =>
        {
            builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

            builder.UseNodaTime();

        });
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddNewtonsoftJson(x =>
{
    x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});
builder.Services.AddResponseCaching();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt => {
    opt.AddPolicy("all",e => {
        e.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});


builder.Services.AddAuthorization();
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = appSettingConfiguration.ValidIssuer,
            ValidAudience = appSettingConfiguration.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettingConfiguration.Secret))
        };
    });

AddHealthCheckK8s(builder.Services, builder.Configuration);
builder.Services.AddTransient<IJwtUtils, JwtUtils>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseForwardedHeaders();
app.UseHttpsRedirection();


app.UseRouting();
app.UseHttpMetrics();
app.UseSerilogRequestLogging();
app.UseCors("all");
app.UseResponseCaching();
app.UseAuthorization();


#pragma warning disable ASP0014 // Suggest using top level route registrations
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();

    endpoints.MapMetrics();

    MapEndPointHealthCheckK8s(endpoints);

    endpoints.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
     string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));
});
#pragma warning restore ASP0014 // Suggest using top level route registrations

app.Run();

void AddHealthCheckK8s(IServiceCollection services, IConfiguration configuration)
{
    services.AddHostedService<StartupHostedService>();
    //services.AddHostedService<StartupFeatureFlagHostedService>();
    services.AddSingleton<StartupHostedServiceHealthCheck>();

    services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>(tags: new[] { "ready" }, customTestQuery: QueryTestDb)
        //.AddCheck<CustomDbContextCheck>("custom_db_context_check", failureStatus: HealthStatus.Unhealthy,
        //    tags: new[] { "ready" })
        .ForwardToPrometheus()
        .AddCheck<StartupHostedServiceHealthCheck>(
            "hosted_service_startup",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready" });

    services.Configure<HealthCheckPublisherOptions>(options =>
    {
        options.Delay = TimeSpan.FromSeconds(2);
        options.Predicate = (check) => check.Tags.Contains("ready");
    });

    services.AddSingleton<IHealthCheckPublisher, ReadinessPublisher>();

}

async Task<bool> QueryTestDb(ApplicationDbContext dbContext, CancellationToken cancellationToken)
{
    await dbContext.DataProtectionKeys.AnyAsync(cancellationToken);
    return true;
}


void MapEndPointHealthCheckK8s(IEndpointRouteBuilder endpoints)
{
    endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
    {
        Predicate = (check) => check.Tags.Contains("ready"),
    });

    endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
    {
        Predicate = (_) => false
    });
}