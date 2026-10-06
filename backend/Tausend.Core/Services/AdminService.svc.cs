using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using Tausend.Core.Business;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Entities.Responses.AdminService;
using Tausend.Core.Responses;

namespace Tausend.Core.Services
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(Namespace = "http://Tausend.Wearelomo.com", InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class AdminService : IAdminService
    {
        public ListOfAdminAccountsResponse EnumAllAccounts(AccessTokenRequest request)
        {
            var bz = new AdminBusiness();
            return bz.EnumAllAccounts(request.AccessToken);
        }

        public ListOfAdminDevicesResponse EnumAllDevices(AccessTokenRequest request)
        {
            var bz = new AdminBusiness();
            return bz.EnumAllDevices(request.AccessToken);
        }

        public SimpleResponse SetAccountRole(SetAccountRoleRequest request)
        {
            var bz = new AdminBusiness();
            return bz.SetAccountRole(request.AccessToken, request.AccountId, request.Role);
        }

        public SimpleResponse BlockDevice(Tausend.Core.Models.CommandRequest request)
        {
            var bz = new AdminBusiness();
            return bz.BlockDevice(request.AccessToken, request.DeviceId);
        }

        public SimpleResponse ResetDevice(Tausend.Core.Models.CommandRequest request)
        {
            var bz = new AdminBusiness();
            return bz.ResetDevice(request.AccessToken, request.DeviceId);
        }

        public SimpleResponse DisassociateDevice(Tausend.Core.Models.CommandRequest request)
        {
            var bz = new AdminBusiness();
            return bz.DisassociateDevice(request.AccessToken, request.DeviceId);
        }

        public ListOfAuditLogResponse EnumAuditLog(AccessTokenRequest request)
        {
            var bz = new AdminBusiness();
            return bz.EnumAuditLog(request.AccessToken);
        }
    }
}
