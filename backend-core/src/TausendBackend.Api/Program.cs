using TausendBackend.Api.Business;
using TausendBackend.Api.Dao;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // The old WCF backend serialized properties as exact PascalCase (Code, Message,
    // AccessToken, ...). Keeping that here means the dashboard and any future mobile
    // client need zero changes to point at this backend instead.
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// Dev-only permissive CORS so the dashboard/mobile dev servers can call this directly
// without a proxy. Production should replace "AllowedOrigins" with the real deployed
// dashboard/app origins -- see appsettings.json.
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddScoped<SqlDbContext>();
builder.Services.AddScoped<AccountDao>();
builder.Services.AddScoped<AdminDao>();
builder.Services.AddScoped<EmailBusiness>();
builder.Services.AddScoped<AccountBusiness>();
builder.Services.AddScoped<AdminBusiness>();
builder.Services.AddScoped<NotificationBuilder>();
// Zone/Exclusion/ProgramControl/User/Event domains -- ported from Tausend.Core's
// ZoneDao/ZoneBusiness, ExclusionDao/ExclusionBusiness, ProgramControlDao/ProgramControlBusiness,
// UserDao/UserBusiness and EventDao. No EventBusiness exists (event creation/enumeration is
// called directly from NotificationBusiness); no controllers here either -- these are consumed
// by DeviceBusiness/CommandBusiness/NotificationBusiness built elsewhere.
builder.Services.AddScoped<ZoneDao>();
builder.Services.AddScoped<ZoneBusiness>();
builder.Services.AddScoped<ExclusionDao>();
builder.Services.AddScoped<ExclusionBusiness>();
builder.Services.AddScoped<ProgramControlDao>();
builder.Services.AddScoped<ProgramControlBusiness>();
builder.Services.AddScoped<ScheduledPgmActionDao>();
builder.Services.AddScoped<ScheduledPgmActionBusiness>();
builder.Services.AddScoped<UserDao>();
builder.Services.AddScoped<UserBusiness>();
builder.Services.AddScoped<EventDao>();
// Singleton: holds the long-lived HttpClient and the FCM OAuth token cache for the app's
// lifetime -- see PushNotificationBusinessFactory / FirebasePushNotificationBusiness.
builder.Services.AddSingleton<PushNotificationBusinessFactory>();
builder.Services.AddScoped<NotificationBusiness>();

// Device/Command domains -- ported from Tausend.Core's DeviceDao/DeviceBusiness and
// CommandBusiness. DeviceDao/DeviceBusiness carry the ownership-check fix from
// ../backend/DAY5_SUMMARY.md baked in from the start (not ported-then-patched). CommandBusiness
// is the client for the relay's PrivateService (see ../backend/RELAY_DECISION.md) -- needs
// IHttpClientFactory for that.
builder.Services.AddHttpClient();
builder.Services.AddScoped<DeviceDao>();
builder.Services.AddScoped<DeviceBusiness>();
builder.Services.AddScoped<CommandBusiness>();

builder.Services.AddHostedService<TausendBackend.Api.Services.ScheduledPgmDispatcher>();

var app = builder.Build();

await TausendBackend.Api.Migrations.StartupMigrations.ApplyAsync(app.Configuration, app.Logger);

app.UseCors();
app.MapControllers();

app.Run();
