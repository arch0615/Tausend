using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Business
{
    public class UserBusiness
    {
        private UserDao _dao;
        public UserBusiness()
        {
            _dao = new UserDao();
        }

        public List<User> EnumUsers(long deviceId)
        {
            return _dao.EnumUsers(deviceId);
        }

        public void CreateUsers(List<User> users)
        {
            _dao.CreateUsers(users);
        }

        public string GetUserTag(long accountId, string alarmIdentifier, int user)
        {
            return _dao.GetUserTag(accountId, alarmIdentifier, user);
        }
    }
}