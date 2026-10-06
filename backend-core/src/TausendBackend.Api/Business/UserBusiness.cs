using TausendBackend.Api.Dao;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Business
{
    /// <summary>
    /// PIN-holder labels/tags on a panel (e.g. "Mom", "Housekeeper" for user number 3). Depended
    /// on by NotificationBuilder (GetUserTag, to name whoever's PIN triggered an event) and
    /// NotificationBusiness (EnumUsers) -- signatures below are kept identical to the original
    /// Tausend.Core.Business.UserBusiness for that reason.
    /// </summary>
    public class UserBusiness
    {
        private readonly UserDao _dao;

        public UserBusiness(UserDao dao)
        {
            _dao = dao;
        }

        public List<User> EnumUsers(long deviceId)
        {
            return _dao.EnumUsers(deviceId);
        }

        public void CreateUsers(List<User> users)
        {
            _dao.CreateUsers(users);
        }

        public string GetUserTag(long accountId, string alarmIdentifier, int userNumber)
        {
            return _dao.GetUserTag(accountId, alarmIdentifier, userNumber);
        }
    }
}
