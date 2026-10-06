using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Backend.Business;
using Tausend.Backend.Models;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Responses;
using Tausend.Core.Responses.DeviceService;

namespace Tausend.Core.Business
{
    public class DeviceBusiness
    {
        private readonly DeviceDao dao;

        public DeviceBusiness()
        {
            dao = new DeviceDao();
        }

        public NewCreatedDeviceResponse CreateDevice(string accessToken, string description, string identifier, string pin, string email)
        {
            var response = new NewCreatedDeviceResponse();
            var notifBusiness = new NotificationBusiness();
            try
            {
                // Validate if PIN is already in use
                var pinRes = dao.ValidatePIN(pin, identifier);
                if (pinRes > 0)
                {
                    response.InformBusinessError("El PIN ingresado ya se encuentra en uso", 400);
                }
                else if (pinRes == -1)
                {
                    response.InformBusinessError("Ocurrió un error al registrar el dispositivo", 400);
                }
                else
                {
                    //var publicKey = GeneratePublicKey(identifier);
                    var result = dao.CreateDevice(accessToken, description, identifier, pin, 0);
                    //if (result > 0)
                    //    new CommandBusiness().ConfigurePublicKey(publicKey, accessToken, result);
                    ValidateCreateDeviceResult(result, response, true);

                    if (response.Code == 0)
                    {
                        response.Device = new AccountDevice()
                        {
                            DeviceId = result,
                            Description = description,
                            Mac = identifier,
                            Pin = pin,
                            IsOnline = true
                        };

                        // Send connection notification
                        var timeInfo = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
                        var date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeInfo);
                        var req = new NotificationRequest()
                        {
                            Secuence = 7,
                            EventDateTime = date.ToString("dd-MM-yy HH:mm:ss"),
                            EventType = "E",
                            NotificationType = 631,
                            Partition = 0,
                            AlarmParameter = 0,
                            AlarmIdentifier = identifier,
                            Email = email
                        };
                        notifBusiness.ProcessNotificationRequest(req);
                    }
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        private ushort GeneratePublicKey(string identifier)
        {
            //var device = dao.GetDevice(identifier);
            //if (device != null)
            //    return device.PublicKey;
            Random r = new Random();
            return (ushort)r.Next(1, 9999);
        }

        public CreatedDeviceResponse UpdateDevice(long deviceId, string description, string identifier, string pin, long accountId)
        {
            var response = new CreatedDeviceResponse();
            try
            {
                var oldPin = dao.GetDevicePIN(deviceId);
                int pinRes;
                if (oldPin == pin)
                {
                    pinRes = 0;
                }
                else
                {
                    // Validate if PIN is already in use
                    pinRes = dao.ValidatePIN(pin, identifier);
                }
                if (pinRes > 0)
                {
                    response.InformBusinessError("El PIN ingresado ya se encuentra en uso", 400);
                }
                else if (pinRes == -1)
                {
                    response.InformBusinessError("Ocurrió un error al registrar el dispositivo", 400);
                }
                else
                {
                    var result = dao.UpdateDevice(deviceId, description, identifier, pin, accountId);
                    ValidateCreateDeviceResult(result, response, false);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public DeviceResponse GetDevice(long deviceId, long accountId)
        {
            var response = new DeviceResponse();
            try
            {
                var device = dao.GetDevice(deviceId, accountId);
                ValidateGetDeviceResult(device, response, deviceId.ToString());
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public DeviceResponse GetDevice(string identifier, long accountId)
        {
            var response = new DeviceResponse();
            try
            {
                var device = dao.GetDevice(identifier, accountId);
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
                var result = dao.DeleteDevice(deviceId, accountId);
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
                var result = dao.UpdateDeviceConnectionParameters(deviceId, ip, port);
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
                var result = dao.UpdateDeviceLastConnection(deviceId);
                ValidateDaoResult(result, response);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        /// <summary>
        /// Sends command to central to check if PIN exists. Returns "ok", "invalid" or "offline"
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="pin"></param>
        /// <returns></returs>
        public string ValidatePinOnDevice(string identifier, string pin)
        {
            var cmbz = new CommandBusiness();
            string command = "USR" + pin;

            // Retry 5 times before sending "offline"
            for (int i = 0; i < 5; i++)
            {
                var pinRes = cmbz.SendCommandToIdentifier(command, identifier, pin);
                if (String.IsNullOrEmpty(pinRes) || !pinRes.StartsWith("USR"))
                {
                    if (pinRes == "ERROR") return "invalid";
                    else i++;
                }
                else
                {
                    return "ok";
                }
            }

            return "offline";
        }

        public SimpleResponse BlockDevice(string identifier, long accountId)
        {
            var response = new SimpleResponse();
            try
            {
                var res = dao.BlockDevice(identifier, accountId);
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
                var res = dao.ResetDevice(identifier, accountId);
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

        private void ValidateDaoResult(long result, BaseResponse response)
        {
            if (result <= 0)
            {
                response.InformNotFound(String.Format("Dispositivo {0} no encontrado", result));
            }
        }

        private void ValidateDaoDisassociateResult(int result, BaseResponse response)
        {
            if (result <= 0)
            {
                response.InformNotFound(String.Format("Dispositivo {0} no encontrado", result));
            }
        }

        private void ValidateGetDeviceResult(Device result, DeviceResponse response, string identifier)
        {
            if (result != null)
            {
                response.Device = result;
            }
            else
            {
                response.InformNotFound(String.Format("Dispositivo {0} no encontrado", identifier));
            }
        }

        private void ValidateCreateDeviceResult(long result, BaseResponse response, bool account)
        {
            if (result > 0)
            {
                response.InformOk();
            }
            else if (result == -1)
            {
                if (account)
                    response.InformNotFound("Cuenta no encontrada");
                else
                    response.InformNotFound("Dispositivo no encontrado");
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

        public CreatedDeviceResponse CreateDeviceSMS(string accessToken, string identifier, string description, string devicePin, string simPin, string phoneNumber, string deviceType)
        {
            var response = new CreatedDeviceResponse();
            try
            {
                var result = dao.CreateDeviceSMS(accessToken, identifier, description, devicePin, simPin, phoneNumber, deviceType);
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
                var result = dao.UpdateDeviceSMS(deviceId, identifier, description, devicePin, simPin, phoneNumber, deviceType, accountId);
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
                var result = dao.DeleteDeviceSMS(deviceId, accountId);
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
                var result = dao.DisassociateCentral(identifier, accountId);
                ValidateDaoDisassociateResult(result, response);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public string GetDevicePIN(long deviceId)
        {
            return dao.GetDevicePIN(deviceId);
        }
    }
}