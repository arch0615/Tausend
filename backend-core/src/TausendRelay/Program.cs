using TausendRelay.Business;
using TausendRelay.Cache;
using TausendRelay.Endpoints;
using TausendRelay.Relay;

var builder = WebApplication.CreateBuilder(args);

// Matches TausendBackend.Api's Program.cs -- PascalCase JSON, no naming-policy translation.
// This matters here specifically because backend-core's CommandBusiness.ExecuteWrappedAsync
// deserializes the SendCommand response with JsonSerializer.Deserialize's *default* options
// (case-sensitive, no camelCase policy) -- Minimal API's own default of camelCase output would
// silently break that deserialization even though the raw-substring-matching call sites
// (GetStatus et al.) wouldn't visibly fail. See Endpoints/PrivateServiceEndpoints.cs.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

// Timeout must be configured here, once, at client-creation time -- IHttpClientFactory reuses
// the underlying HttpClient across calls, and HttpClient.Timeout throws InvalidOperationException
// if set again after the first request has already gone out (see BackendClient.ExecuteAsync).
builder.Services.AddHttpClient<BackendClient>(client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddSingleton<DeviceCache>();
builder.Services.AddSingleton<UdpRelayService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<UdpRelayService>());

// Bound to localhost, not 0.0.0.0: PrivateService is called only by backend-core, which is
// expected to run on the same box (unlike the UDP socket above, which must stay open to the
// internet so real panels can reach it). Override PrivateServiceBindAddress if the two ever run
// on separate hosts. Port kept distinct from TausendBackend.Api's own default (5000) so the two
// can share a box without colliding; matches the original relay's PrivateService port (10001) so
// backend-core's existing "PrivateService" config value needs no change.
var privateServiceBindAddress = builder.Configuration["PrivateServiceBindAddress"] ?? "127.0.0.1";
var privateServicePort = builder.Configuration["PrivateServicePort"] ?? "10001";
builder.WebHost.UseUrls($"http://{privateServiceBindAddress}:{privateServicePort}");

var app = builder.Build();

app.MapPrivateService();

app.Run();
