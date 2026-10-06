using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tausend.Backend.Models;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Enums;
using Tausend.Core.Models;
using Tausend.Core.Services;

namespace UnitTesting
{
    // Day 3 (auth checks added to previously-unprotected DeviceService endpoints, e.g. delete
    // device, block/reset PIN, disassociate panel, SMS device update/delete) + Day 4's
    // system-key work on the relay-called endpoints. All negative-path: calling each endpoint
    // with a garbage token (and, for relay-only endpoints, no X-System-Key -- calling the
    // service class directly outside a live WCF host means WebOperationContext.Current is
    // null, so SystemAuth.IsValidRequest() is false the same way a real unsigned request
    // would be) must come back Unauthorized, not silently proceed. Read-only against the DB
    // (ValidateAccessToken lookups only) -- no account/device is created or mutated.
    [TestClass]
    public class DeviceServiceAuthTesting
    {
        private const string GarbageToken = "not-a-real-access-token";

        [TestMethod]
        public void GetDeviceByID_RejectsGarbageToken()
        {
            var res = new DeviceService().GetDeviceByID(new Device { AccessToken = GarbageToken, DeviceId = 1 });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }

        [TestMethod]
        public void GetDeviceByIdentifier_RejectsGarbageToken()
        {
            var res = new DeviceService().GetDeviceByIdentifier(new Device { AccessToken = GarbageToken, Identifier = "0001" });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }

        [TestMethod]
        public void UpdateDevice_RejectsGarbageToken()
        {
            var res = new DeviceService().UpdateDevice(new UpdateDeviceRequest
            {
                AccessToken = GarbageToken,
                DeviceId = 1,
                Identifier = "0001",
                Pin = "1234",
                Description = "test"
            });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }

        [TestMethod]
        public void DeleteDevice_RejectsGarbageToken()
        {
            var res = new DeviceService().DeleteDevice(new Device { AccessToken = GarbageToken, DeviceId = 1 });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }

        [TestMethod]
        public void BlockPIN_RejectsGarbageToken()
        {
            // BlockPIN returns a plain string (the response Message), not a full response
            // object -- this is the actual, if awkward, contract, so pin the exact message.
            var message = new DeviceService().BlockPIN(new PinRequest
            {
                AccessToken = GarbageToken,
                Identifier = "0001",
                Action = "Block"
            });
            Assert.AreEqual("Access Token inválido", message);
        }

        [TestMethod]
        public void DisassociateCentral_RejectsGarbageTokenWithNoSystemKey()
        {
            // Accepts EITHER a user AccessToken or the relay's system key (Day 4); a garbage
            // token AND no system key (no live WCF request context here) must still be rejected.
            var res = new DeviceService().DisassociateCentral(new DeviceDisassociate
            {
                AccessToken = GarbageToken,
                Identifier = "0001"
            });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }

        [TestMethod]
        public void UpdateDeviceConnectionParameters_RejectsRequestWithNoSystemKey()
        {
            // Relay-only endpoint, no AccessToken concept -- must reject when there's no
            // X-System-Key at all (again, no live WCF context here == no header sent).
            var res = new DeviceService().UpdateDeviceConnectionParameters(new Device { DeviceId = 1, IP = "127.0.0.1", Port = "10000" });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }

        [TestMethod]
        public void UpdateDeviceLastConnection_RejectsRequestWithNoSystemKey()
        {
            var res = new DeviceService().UpdateDeviceLastConnection(new Device { DeviceId = 1 });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }

        [TestMethod]
        public void NotifyEvent_RejectsRequestWithNoSystemKey()
        {
            var res = new NotificationService().NotifyEvent(new NotificationRequest
            {
                AlarmIdentifier = "0001",
                EventType = "TEST"
            });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }
    }
}
