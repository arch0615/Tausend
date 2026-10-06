using System;
using System.Collections.Generic;
using System.Linq;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Models;
using Tausend.Core.Entities.Responses.AdminService;
using Tausend.Core.Enums;
using Tausend.Core.Responses;

namespace Tausend.Core.Business
{
    public class AdminBusiness
    {
        private AdminDao dao;
        private AccountBusiness accountBusiness;

        public AdminBusiness()
        {
            dao = new AdminDao();
            accountBusiness = new AccountBusiness();
        }

        public ListOfAdminAccountsResponse EnumAllAccounts(string accessToken)
        {
            var response = new ListOfAdminAccountsResponse(null);
            try
            {
                if (!CheckAdmin(accessToken, response))
                    return response;
                response.Accounts = dao.EnumAllAccounts();
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
                response.Devices = dao.EnumAllDevices();
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
                var res = dao.SetAccountRole(targetAccountId, role);
                if (res <= 0)
                {
                    response.InformNotFound(String.Format("Cuenta {0} no encontrada", targetAccountId));
                }
                else
                {
                    LogAction(accessToken, "SetAccountRole", "Account", targetAccountId, String.Format("Role={0}", role));
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
                var res = dao.AdminBlockDevice(deviceId);
                if (res <= 0)
                {
                    response.InformNotFound(String.Format("Dispositivo {0} no encontrado", deviceId));
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
                var res = dao.AdminResetDevice(deviceId);
                if (res <= 0)
                {
                    response.InformNotFound(String.Format("Dispositivo {0} no encontrado", deviceId));
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
                var res = dao.AdminDisassociateDevice(deviceId);
                if (res <= 0)
                {
                    response.InformNotFound(String.Format("Dispositivo {0} no encontrado o sin cuentas vinculadas", deviceId));
                }
                else
                {
                    response.Message = "Dispositivo desvinculado correctamente.";
                    LogAction(accessToken, "DisassociateDevice", "Device", deviceId, String.Format("AccountsUnlinked={0}", res));
                }
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
                response.Entries = dao.EnumAuditLog();
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        /// <summary>
        /// Best-effort audit trail write -- an actor id that resolves (the caller just passed
        /// CheckAdmin, so it always will) is all this needs; failures here shouldn't mask the
        /// action's own success.
        /// </summary>
        private void LogAction(string accessToken, string action, string targetType, long? targetId, string details)
        {
            var actorAccountId = accountBusiness.ResolveAccountId(accessToken);
            dao.CreateAuditLogEntry(actorAccountId, action, targetType, targetId, details);
        }

        /// <summary>
        /// Distinguishes "not logged in" (401 Unauthorized) from "logged in but not an admin"
        /// (403 Forbidden) -- previously both collapsed into a single Forbidden response here,
        /// which contradicted this endpoint set's whole reason for having a FORBIDDEN state.
        /// Sets the outcome on <paramref name="response"/> and returns whether the caller is an
        /// admin (i.e. whether the endpoint should proceed).
        /// </summary>
        private bool CheckAdmin(string accessToken, BaseResponse response)
        {
            if (!accountBusiness.ValidateAccessToken(accessToken))
            {
                response.InformUnauthorized();
                return false;
            }
            if (!accountBusiness.IsAdmin(accessToken))
            {
                response.InformForbidden();
                return false;
            }
            return true;
        }
    }
}
