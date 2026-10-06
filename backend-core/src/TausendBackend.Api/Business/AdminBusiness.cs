using TausendBackend.Api.Dao;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Responses;

namespace TausendBackend.Api.Business
{
    public class AdminBusiness
    {
        private readonly AdminDao _dao;
        private readonly AccountBusiness _accountBusiness;

        public AdminBusiness(AdminDao dao, AccountBusiness accountBusiness)
        {
            _dao = dao;
            _accountBusiness = accountBusiness;
        }

        public ListOfAdminAccountsResponse EnumAllAccounts(string accessToken)
        {
            var response = new ListOfAdminAccountsResponse(null);
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                response.Accounts = _dao.EnumAllAccounts();
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public ListOfAdminDevicesResponse EnumAllDevices(string accessToken)
        {
            var response = new ListOfAdminDevicesResponse(null);
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                response.Devices = _dao.EnumAllDevices();
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public SimpleResponse SetAccountRole(string accessToken, long targetAccountId, AccountRole role)
        {
            var response = new SimpleResponse();
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                var res = _dao.SetAccountRole(targetAccountId, role);
                if (res <= 0)
                {
                    response.InformNotFound($"Cuenta {targetAccountId} no encontrada");
                }
                else
                {
                    LogAction(accessToken, "SetAccountRole", "Account", targetAccountId, $"Role={role}");
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        /// <summary>Also revokes the account's current sessions when disabling -- Accounts.Enabled
        /// alone only blocks the NEXT login (CreateLoginSession re-checks it); it doesn't
        /// invalidate a token already issued, since ValidateAccessToken/RefreshAccessToken don't
        /// re-check Enabled on every call. Re-enabling doesn't restore the old sessions.</summary>
        public SimpleResponse SetAccountEnabled(string accessToken, long targetAccountId, bool enabled)
        {
            var response = new SimpleResponse();
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                // Resolved before the possible session revoke below -- disabling can invalidate
                // the caller's OWN token (an admin disabling themselves), and LogAction needs a
                // still-valid actor id regardless.
                var actorAccountId = _accountBusiness.ResolveAccountId(accessToken);
                var res = _dao.SetAccountEnabled(targetAccountId, enabled);
                if (res <= 0)
                {
                    response.InformNotFound($"Cuenta {targetAccountId} no encontrada");
                }
                else
                {
                    if (!enabled)
                        _dao.RevokeAccountSessions(targetAccountId);
                    LogAction(actorAccountId, enabled ? "EnableAccount" : "DisableAccount", "Account", targetAccountId, null);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public SimpleResponse RevokeAccountSessions(string accessToken, long targetAccountId)
        {
            var response = new SimpleResponse();
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                // Resolved before the revoke -- an admin can revoke their OWN sessions, which
                // deletes the very token this method was authorized with.
                var actorAccountId = _accountBusiness.ResolveAccountId(accessToken);
                var res = _dao.RevokeAccountSessions(targetAccountId);
                if (res <= 0)
                {
                    response.InformNotFound($"Cuenta {targetAccountId} no encontrada");
                }
                else
                {
                    response.Message = "Sesiones revocadas correctamente.";
                    LogAction(actorAccountId, "RevokeAccountSessions", "Account", targetAccountId, null);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public SimpleResponse UnlinkAccountDevice(string accessToken, long targetAccountId, long deviceId)
        {
            var response = new SimpleResponse();
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                var res = _dao.AdminUnlinkAccountDevice(targetAccountId, deviceId);
                if (res <= 0)
                {
                    response.InformNotFound($"La cuenta {targetAccountId} no tiene vinculado el dispositivo {deviceId}");
                }
                else
                {
                    response.Message = "Dispositivo desvinculado de la cuenta correctamente.";
                    LogAction(accessToken, "UnlinkAccountDevice", "Device", deviceId, $"AccountId={targetAccountId}");
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public SimpleResponse SetDeviceEnabled(string accessToken, long deviceId, bool enabled)
        {
            var response = new SimpleResponse();
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                var res = _dao.AdminSetDeviceEnabled(deviceId, enabled);
                if (res <= 0)
                {
                    response.InformNotFound($"Dispositivo {deviceId} no encontrado");
                }
                else
                {
                    LogAction(accessToken, enabled ? "EnableDevice" : "DisableDevice", "Device", deviceId, null);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public SimpleResponse BlockDevice(string accessToken, long deviceId)
        {
            var response = new SimpleResponse();
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                var res = _dao.AdminBlockDevice(deviceId);
                if (res <= 0)
                {
                    response.InformNotFound($"Dispositivo {deviceId} no encontrado");
                }
                else
                {
                    response.Message = "Dispositivo bloqueado correctamente.";
                    LogAction(accessToken, "BlockDevice", "Device", deviceId, null);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public SimpleResponse ResetDevice(string accessToken, long deviceId)
        {
            var response = new SimpleResponse();
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                var res = _dao.AdminResetDevice(deviceId);
                if (res <= 0)
                {
                    response.InformNotFound($"Dispositivo {deviceId} no encontrado");
                }
                else
                {
                    response.Message = "Dispositivo blanqueado correctamente.";
                    LogAction(accessToken, "ResetDevice", "Device", deviceId, null);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public SimpleResponse DisassociateDevice(string accessToken, long deviceId)
        {
            var response = new SimpleResponse();
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                var res = _dao.AdminDisassociateDevice(deviceId);
                if (res <= 0)
                {
                    response.InformNotFound($"Dispositivo {deviceId} no encontrado o sin cuentas vinculadas");
                }
                else
                {
                    response.Message = "Dispositivo desvinculado correctamente.";
                    LogAction(accessToken, "DisassociateDevice", "Device", deviceId, $"AccountsUnlinked={res}");
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public ListOfAccountDeviceLinksResponse EnumAccountDeviceLinks(string accessToken)
        {
            var response = new ListOfAccountDeviceLinksResponse(null);
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                response.Links = _dao.EnumAccountDeviceLinks();
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public ListOfEventsResponse EnumAllEvents(string accessToken)
        {
            var response = new ListOfEventsResponse(null);
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                return new ListOfEventsResponse(_dao.EnumAllEvents());
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public ListOfAuditLogResponse EnumAuditLog(string accessToken)
        {
            var response = new ListOfAuditLogResponse(null);
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                response.Entries = _dao.EnumAuditLog();
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        /// <summary>Best-effort audit trail write -- an actor id that resolves (the caller just
        /// passed CheckAdmin, so it always will) is all this needs; failures here shouldn't mask
        /// the action's own success.</summary>
        private void LogAction(string accessToken, string action, string targetType, long? targetId, string? details)
        {
            var actorAccountId = _accountBusiness.ResolveAccountId(accessToken);
            LogAction(actorAccountId, action, targetType, targetId, details);
        }

        /// <summary>Use this overload (resolving the actor's id BEFORE the action runs) whenever
        /// the action can invalidate the caller's own access token -- e.g. an admin revoking their
        /// own sessions. Resolving from the token AFTER that would return 0 (token already gone)
        /// and fail the audit insert's FK constraint on ActorAccountId.</summary>
        private void LogAction(long actorAccountId, string action, string targetType, long? targetId, string? details)
        {
            _dao.CreateAuditLogEntry(actorAccountId, action, targetType, targetId, details);
        }

        /// <summary>
        /// Distinguishes "not logged in" (401 Unauthorized) from "logged in but not an admin"
        /// (403 Forbidden). Sets the outcome on <paramref name="response"/> and returns whether
        /// the caller is an admin (i.e. whether the endpoint should proceed).
        /// </summary>
        private bool CheckAdmin(string accessToken, BaseResponse response)
        {
            if (!_accountBusiness.ValidateAccessToken(accessToken))
            {
                response.InformUnauthorized();
                return false;
            }
            if (!_accountBusiness.IsAdmin(accessToken))
            {
                response.InformForbidden();
                return false;
            }
            return true;
        }
    }
}
