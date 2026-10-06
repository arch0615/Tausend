using TausendBackend.Api.Business;
using TausendBackend.Api.Dao;
using TausendBackend.Api.Requests;

namespace TausendBackend.Api.Services
{
    /// <summary>
    /// Polls ScheduledPgmActions roughly once a minute and fires any that are due -- what
    /// "Scheduled Departures" in the client spec actually needs. Registered as a singleton
    /// (AddHostedService), so it creates its own DI scope per tick to pull the scoped
    /// DAO/Business classes the rest of the app uses (same pattern as EF Core's own guidance
    /// for background services that need scoped services).
    ///
    /// PGMCommand never reads AccessToken (it resolves the device unscoped via DeviceId, see
    /// CommandBusiness.CreateBody's "0 = system caller" convention) -- an empty AccessToken here
    /// is safe and deliberate, not an oversight.
    /// </summary>
    public class ScheduledPgmDispatcher : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
        // Resolved once at startup, not per tick -- also seen crashing every single tick in
        // production (2026-09-05 logs): .NET's ICU-based id normalization mapped the IANA id
        // below to the legacy alias "America/Buenos_Aires", which this box's tzdata doesn't ship
        // as a symlink, even though the canonical "America/Argentina/Buenos_Aires" zoneinfo file
        // exists. Falls back to a fixed UTC-3 offset -- Argentina has had no DST since 2009, so
        // this is exact, not an approximation -- rather than let a tzdata/ICU quirk silently stop
        // every scheduled action from ever firing again.
        private static readonly TimeZoneInfo ArgentinaTimeZone = ResolveArgentinaTimeZone();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledPgmDispatcher> _logger;

        private static TimeZoneInfo ResolveArgentinaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Argentina-Fixed", TimeSpan.FromHours(-3), "Argentina Time", "Argentina Time");
            }
        }

        public ScheduledPgmDispatcher(IServiceScopeFactory scopeFactory, ILogger<ScheduledPgmDispatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "ScheduledPgmDispatcher tick failed");
                }
                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Shutdown requested mid-delay -- fall through and let the loop exit.
                }
            }
        }

        private async Task RunOnceAsync()
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ArgentinaTimeZone);
            var currentTime = new TimeSpan(localNow.Hour, localNow.Minute, 0);
            var currentDayMask = (byte)(1 << (int)localNow.DayOfWeek);

            using var scope = _scopeFactory.CreateScope();
            var dao = scope.ServiceProvider.GetRequiredService<ScheduledPgmActionDao>();
            var commandBusiness = scope.ServiceProvider.GetRequiredService<CommandBusiness>();

            var due = dao.FindDueScheduledPgmActions(currentTime, currentDayMask, localNow.Date);
            if (due.Count == 0) return;

            _logger.LogInformation("ScheduledPgmDispatcher: {Count} action(s) due at {Time}", due.Count, currentTime);
            foreach (var action in due)
            {
                try
                {
                    var result = await commandBusiness.PGMCommand(new PGMRequest
                    {
                        DeviceId = action.DeviceId,
                        Zone = action.ProgramControlNumber,
                        State = action.DesiredState,
                        AccessToken = "",
                    });
                    dao.MarkScheduledPgmActionFired(action.ScheduledPgmActionId, localNow.Date);
                    // PGMCommand catches its own exceptions and returns the message as a normal
                    // string rather than throwing (see CommandBusiness.PGMCommand), so a relay
                    // failure would otherwise mark the action fired while silently never having
                    // reached the panel -- log it so that failure mode is at least visible.
                    if (result != "OK")
                        _logger.LogWarning("ScheduledPgmAction {Id} fired but relay did not confirm: {Result}", action.ScheduledPgmActionId, result);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to fire ScheduledPgmAction {Id}", action.ScheduledPgmActionId);
                }
            }
        }
    }
}
