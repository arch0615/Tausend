namespace TausendBackend.Api.Dao
{
    /// <summary>Per-(phone, panel) push mute, checked before every send in NotificationBusiness --
    /// see MutedPanelNotifications.sql for why muting is scoped to the device token, not the
    /// account, so one phone's mute never affects another phone on the same account/panel.</summary>
    public class NotificationMuteDao
    {
        private readonly SqlDbContext _db;

        public NotificationMuteDao(SqlDbContext db)
        {
            _db = db;
        }

        public void SetMuted(string deviceToken, string identifier, bool muted)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.SetNotificationMute");
                _db.AddStringParameter(sqlCommand, "DeviceToken", deviceToken);
                _db.AddStringParameter(sqlCommand, "Identifier", identifier);
                _db.AddBoolParameter(sqlCommand, "Muted", muted);
                sqlCommand.ExecuteScalar();
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
        }

        public bool IsMuted(string deviceToken, string identifier)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.IsNotificationMuted");
                _db.AddStringParameter(sqlCommand, "DeviceToken", deviceToken);
                _db.AddStringParameter(sqlCommand, "Identifier", identifier);
                var res = (bool)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
        }
    }
}
