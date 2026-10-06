using Microsoft.AspNetCore.Mvc;
using TausendBackend.Api.Business;
using TausendBackend.Api.Requests;
using TausendBackend.Api.Responses;

namespace TausendBackend.Api.Controllers
{
    [ApiController]
    [Route("AdminService")]
    public class AdminController : ControllerBase
    {
        private readonly AdminBusiness _adminBusiness;

        public AdminController(AdminBusiness adminBusiness)
        {
            _adminBusiness = adminBusiness;
        }

        [HttpPost("EnumAllAccounts")]
        public ListOfAdminAccountsResponse EnumAllAccounts(AccessTokenRequest request) => _adminBusiness.EnumAllAccounts(request.AccessToken);

        [HttpPost("EnumAllDevices")]
        public ListOfAdminDevicesResponse EnumAllDevices(AccessTokenRequest request) => _adminBusiness.EnumAllDevices(request.AccessToken);

        [HttpPost("EnumAccountDeviceLinks")]
        public ListOfAccountDeviceLinksResponse EnumAccountDeviceLinks(AccessTokenRequest request) => _adminBusiness.EnumAccountDeviceLinks(request.AccessToken);

        [HttpPost("SetAccountRole")]
        public SimpleResponse SetAccountRole(SetAccountRoleRequest request) => _adminBusiness.SetAccountRole(request.AccessToken, request.AccountId, request.Role);

        [HttpPost("SetAccountEnabled")]
        public SimpleResponse SetAccountEnabled(SetAccountEnabledRequest request) => _adminBusiness.SetAccountEnabled(request.AccessToken, request.AccountId, request.Enabled);

        [HttpPost("RevokeAccountSessions")]
        public SimpleResponse RevokeAccountSessions(AccountIdRequest request) => _adminBusiness.RevokeAccountSessions(request.AccessToken, request.AccountId);

        [HttpPost("UnlinkAccountDevice")]
        public SimpleResponse UnlinkAccountDevice(UnlinkAccountDeviceRequest request) => _adminBusiness.UnlinkAccountDevice(request.AccessToken, request.AccountId, request.DeviceId);

        [HttpPost("SetDeviceEnabled")]
        public SimpleResponse SetDeviceEnabled(SetDeviceEnabledRequest request) => _adminBusiness.SetDeviceEnabled(request.AccessToken, request.DeviceId, request.Enabled);

        [HttpPost("BlockDevice")]
        public SimpleResponse BlockDevice(CommandRequest request) => _adminBusiness.BlockDevice(request.AccessToken, request.DeviceId);

        [HttpPost("ResetDevice")]
        public SimpleResponse ResetDevice(CommandRequest request) => _adminBusiness.ResetDevice(request.AccessToken, request.DeviceId);

        [HttpPost("DisassociateDevice")]
        public SimpleResponse DisassociateDevice(CommandRequest request) => _adminBusiness.DisassociateDevice(request.AccessToken, request.DeviceId);

        [HttpPost("EnumAllEvents")]
        public ListOfEventsResponse EnumAllEvents(AccessTokenRequest request) => _adminBusiness.EnumAllEvents(request.AccessToken);

        [HttpPost("EnumAuditLog")]
        public ListOfAuditLogResponse EnumAuditLog(AccessTokenRequest request) => _adminBusiness.EnumAuditLog(request.AccessToken);
    }
}
