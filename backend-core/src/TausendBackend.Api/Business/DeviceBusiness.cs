using TausendBackend.Api.Dao;
using TausendBackend.Api.Models;
using TausendBackend.Api.Responses;

namespace TausendBackend.Api.Business
{
    public class DeviceBusiness
    {
        private readonly DeviceDao _dao;
        private readonly CommandBusiness _commandBusiness;

        public DeviceBusiness(DeviceDao dao, CommandBusiness commandBusiness)
        {
            _dao = dao;
            _commandBusiness = commandBusiness;
        }

        /// <summary>Sends the command to check if PIN exists on the panel. Returns "ok", "invalid",
        /// or "offline".</summary>
        public async Task<string> ValidatePinOnDevice(string identifier, string pin)
        {
            var command = "USR" + pin;
            // Retry up to 5 times before reporting "offline" -- the original double-increments i
            // on a non-ERROR/non-USR response (the loop's own i++ plus an explicit one below), so
            // this actually only retries ~2-3 times in practice; preserved as-is for parity rather
            // than "fixed", since that changes real timing against the relay.
            for (var i = 0; i < 5; i++)
            {
                var pinRes = await _commandBusiness.SendCommandToIdentifier(command, identifier, pin);
                if (string.IsNullOrEmpty(pinRes) || !pinRes.StartsWith("USR"))
                {
                    if (pinRes == "ERROR") return "invalid";
                    i++;
                }
                else
                {
                    return "ok";
                }
            }
            return "offline";
        }

        /// <summary>
        /// Does NOT send the "device connected" notification the original does inline (that
        /// would make DeviceBusiness depend on NotificationBusiness, which already depends on
        /// DeviceBusiness for device lookups -- a circular DI dependency the original's
        /// new-it-yourself style never had to worry about but this constructor-DI port does).
        /// Callers (DeviceController) send it via NotificationBusiness after a successful create.
        /// </summary>
        public NewCreatedDeviceResponse CreateDevice(string accessToken, string description, string identifier, string pin)
        {
            var response = new NewCreatedDeviceResponse();
            try
            {
                var pinRes = _dao.ValidatePIN(pin, identifier);
                if (pinRes > 0)
                {
                    response.InformBusinessError("El código de usuario ingresado ya se encuentra en uso", 400);
                }
                else if (pinRes == -1)
                {
                    response.InformBusinessError("Ocurrió un error al registrar el dispositivo", 400);
                }
                else
                {
                    var result = _dao.CreateDevice(accessToken, description, identifier, pin, 0);
                    ValidateCreateDeviceResult(result, response, true);

                    if (response.Code == 0)
                    {
                        response.Device = new AccountDevice
                        {
                            DeviceId = result,
                            Description = description,
                            Mac = identifier,
                            Pin = pin,
                            IsOnline = true
                        };
                    }
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public CreatedDeviceResponse UpdateDevice(long deviceId, string description, string identifier, string pin, long accountId)
        {
            var response = new CreatedDeviceResponse();
            try
            {
                var oldPin = _dao.GetDevicePIN(deviceId);
                int pinRes;
                if (oldPin == pin)
                {
                    pinRes = 0;
                }
                else
                {
                    pinRes = _dao.ValidatePIN(pin, identifier);
                }
                if (pinRes > 0)
                {
                    response.InformBusinessError("El código de usuario ingresado ya se encuentra en uso", 400);
                }
                else if (pinRes == -1)
                {
                    response.InformBusinessError("Ocurrió un error al registrar el dispositivo", 400);
                }
                else
                {
                    var result = _dao.UpdateDevice(deviceId, description, identifier, pin, accountId);
                    ValidateCreateDeviceResult(result, response, false);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        /// <summary>accountId = 0 means an internal/system caller (the relay) -- unscoped lookup;
        /// anything else must own this device.</summary>
        public DeviceResponse GetDevice(long deviceId, long accountId)
        {
            var response = new DeviceResponse();
            try
            {
                var device = _dao.GetDevice(deviceId, accountId);
                ValidateGetDeviceResult(device, response, deviceId.ToString());
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        /// <summary>accountId = 0 means an internal/system caller (the relay) -- unscoped lookup;
        /// anything else must own this device.</summary>
        public DeviceResponse GetDevice(string identifier, long accountId)
        {
            var response = new DeviceResponse();
            try
            {
                var device = _dao.GetDevice(identifier, accountId);
                ValidateGetDeviceResult(device, response, identifier);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public DeletedDeviceResponse DeleteDevice(long deviceId, long accountId)
        {
            var response = new DeletedDeviceResponse();
            try
            {
                var result = _dao.DeleteDevice(deviceId, accountId);
                ValidateDaoResult(result, response);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public UpdateDeviceResponse UpdateDeviceConnectionParameters(long deviceId, string ip, string port)
        {
            var response = new UpdateDeviceResponse();
            try
            {
                var result = _dao.UpdateDeviceConnectionParameters(deviceId, ip, port);
                ValidateDaoResult(result, response);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public UpdateDeviceResponse UpdateDeviceLastConnection(long deviceId)
        {
            var response = new UpdateDeviceResponse();
            try
            {
                var result = _dao.UpdateDeviceLastConnection(deviceId);
                ValidateDaoResult(result, response);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public SimpleResponse BlockDevice(string identifier, long accountId)
        {
            var response = new SimpleResponse();
            try
            {
                var res = _dao.BlockDevice(identifier, accountId);
                if (res == 1)
                {
                    response.InformOk();
                    response.Message = "Dispositivo bloqueado correctamente.";
                }
                else
                {
                    response.InformBusinessError("No se ha encontrado la central indicada", 400);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e);
            }
            return response;
        }

        public SimpleResponse ResetDevice(string identifier, long accountId)
        {
            var response = new SimpleResponse();
            try
            {
                var res = _dao.ResetDevice(identifier, accountId);
                if (res == 1)
                {
                    response.InformOk();
                    response.Message = "Dispositivo blanqueado correctamente.";
                }
                else
                {
                    response.InformBusinessError("No se ha encontrado la central indicada", 400);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e);
            }
            return response;
        }

        public CreatedDeviceResponse CreateDeviceSMS(string accessToken, string identifier, string description, string devicePin, string simPin, string phoneNumber, string deviceType)
        {
            var response = new CreatedDeviceResponse();
            try
            {
                var result = _dao.CreateDeviceSMS(accessToken, identifier, description, devicePin, simPin, phoneNumber, deviceType);
                ValidateCreateDeviceResult(result, response, true);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public CreatedDeviceResponse UpdateDeviceSMS(long deviceId, string identifier, string description, string devicePin, string simPin, string phoneNumber, string deviceType, long accountId)
        {
            var response = new CreatedDeviceResponse();
            try
            {
                var result = _dao.UpdateDeviceSMS(deviceId, identifier, description, devicePin, simPin, phoneNumber, deviceType, accountId);
                ValidateCreateDeviceResult(result, response, false);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public DeletedDeviceResponse DeleteDeviceSMS(long deviceId, long accountId)
        {
            var response = new DeletedDeviceResponse();
            try
            {
                var result = _dao.DeleteDeviceSMS(deviceId, accountId);
                ValidateDaoResult(result, response);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        /// <summary>accountId = 0 means an internal/system caller (the relay) unlinking every
        /// account from the device (e.g. factory reset); otherwise, only the caller's own link
        /// is removed.</summary>
        public DisassociateCentralResponse DeviceDisassociate(string identifier, long accountId)
        {
            var response = new DisassociateCentralResponse();
            try
            {
                var result = _dao.DisassociateCentral(identifier, accountId);
                if (result <= 0)
                {
                    response.InformNotFound($"Dispositivo {identifier} no encontrado");
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public string GetDevicePIN(long deviceId) => _dao.GetDevicePIN(deviceId);

        private static void ValidateDaoResult(long result, BaseResponse response)
        {
            if (result <= 0)
            {
                response.InformNotFound($"Dispositivo {result} no encontrado");
            }
        }

        private static void ValidateGetDeviceResult(Device? result, DeviceResponse response, string identifier)
        {
            if (result != null)
            {
                response.Device = result;
            }
            else
            {
                response.InformNotFound($"Dispositivo {identifier} no encontrado");
            }
        }

        private static void ValidateCreateDeviceResult(long result, BaseResponse response, bool account)
        {
            if (result > 0)
            {
                response.InformOk();
            }
            else if (result == -1)
            {
                response.InformNotFound(account ? "Cuenta no encontrada" : "Dispositivo no encontrado");
            }
            else if (result == -2)
            {
                response.InformBusinessError("Dispositivo con descripción repetida", 2000);
            }
            else
            {
                response.InformBusinessError("Error no especificado", 2222);
            }
        }
    }
}
