using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tausend.RelayServer.Settings
{
    public class ServiceSettings
    {
        public const string COMMAND_SERVICE = "CommandService";
        public const string DEVICE_SERVICE = "DeviceService";
        public const string ACCOUNT_SERVICE = "AccountService";
        public const string NOTIFICATION_SERVICE = "NotificationService";

        public string CommandService { get; private set; }
        public string DeviceService { get; private set; }
        public string AccountService { get; private set; }
        public string NotificationService { get; private set; }

        public ServiceSettings(string commandService, string deviceService, string accountService, string notificationService)
        {
            CommandService = commandService;
            DeviceService = deviceService;
            AccountService = accountService;
            NotificationService = notificationService;
        }
    }
}
