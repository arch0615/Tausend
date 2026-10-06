using TausendBackend.Api.Dao;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Business
{
    // Device-ownership checks happen in DeviceController (AuthorizeDeviceAccess) before any of
    // these run, same as every other panel-scoped feature in that controller (Zones, Users) --
    // this class is pure CRUD over the schedule rows themselves.
    public class ScheduledPgmActionBusiness
    {
        private readonly ScheduledPgmActionDao _dao;

        public ScheduledPgmActionBusiness(ScheduledPgmActionDao dao)
        {
            _dao = dao;
        }

        public long CreateScheduledPgmAction(long deviceId, int programControlNumber, TimeSpan timeOfDay, byte daysOfWeekMask, bool desiredState)
            => _dao.CreateScheduledPgmAction(deviceId, programControlNumber, timeOfDay, daysOfWeekMask, desiredState);

        public List<ScheduledPgmAction> EnumScheduledPgmActions(long deviceId) => _dao.EnumScheduledPgmActions(deviceId);

        public long DeleteScheduledPgmAction(long scheduledPgmActionId, long deviceId) => _dao.DeleteScheduledPgmAction(scheduledPgmActionId, deviceId);

        public long SetScheduledPgmActionEnabled(long scheduledPgmActionId, long deviceId, bool enabled)
            => _dao.SetScheduledPgmActionEnabled(scheduledPgmActionId, deviceId, enabled);
    }
}
