using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tausend.Backend.Models;
using Tausend.Core.Business;
using Tausend.Core.Dao;
using Tausend.Core.Enums;
using Tausend.Core.Models;
using Tausend.Core.Services;

namespace UnitTesting
{
    // Day 5 security fix: DeviceService endpoints must check that the CALLER'S account owns
    // the target device, not just that the caller is logged in as someone. Account A creates a
    // device; Account B (a real, separate, logged-in account with no relationship to it) must
    // not be able to read/modify/delete/block/disassociate it. This is the ownership-boundary
    // coverage DeviceServiceAuthTesting.cs deliberately doesn't have (that file only tests
    // garbage tokens). Bypasses DeviceBusiness.CreateDevice (which contacts a live panel via
    // ValidatePinOnDevice) and creates the device directly via DeviceDao -- matching this
    // project's established pattern for auth-gated setup. Not run as part of this pass; see
    // backend/DAY5_SUMMARY.md.
    [TestClass]
    public class DeviceOwnershipTesting
    {
        private const string OwnerEmail = "qa.day5.deviceowner@wearelomo.com";
        private const string OtherEmail = "qa.day5.deviceother@wearelomo.com";
        private const string TestPassword = "Day5Testing!1";
        private const string DeviceIdentifier = "QA5OWN0001";

        private static AccountBusiness accountBz;
        private static DeviceService deviceService;
        private static string ownerToken;
        private static string otherToken;
        private static long ownedDeviceId;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            accountBz = new AccountBusiness();
            deviceService = new DeviceService();

            ownerToken = EnsureTestAccount(OwnerEmail, "Day5", "Owner");
            otherToken = EnsureTestAccount(OtherEmail, "Day5", "Other");

            var deviceDao = new DeviceDao();
            ownedDeviceId = deviceDao.CreateDevice(ownerToken, "Day5 ownership test device", DeviceIdentifier, "1234", 0);
            Assert.IsTrue(ownedDeviceId > 0, "Setup: could not create the test device");
        }

        private static string EnsureTestAccount(string email, string firstName, string lastName)
        {
            var existing = accountBz.Login(email, TestPassword);
            if (existing.Account != null)
            {
                accountBz.DeleteAccount(existing.Account.AccessToken);
            }
            var created = accountBz.CreateAccount(new Account
            {
                Email = email,
                Password = TestPassword,
                FirstName = firstName,
                LastName = lastName
            });
            Assert.AreEqual(0, created.Code, "Setup: account creation failed for " + email + " -- " + created.Message);
            var login = accountBz.Login(email, TestPassword);
            Assert.IsNotNull(login.Account, "Setup: could not log into " + email);
            return login.Account.AccessToken;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            foreach (var email in new[] { OwnerEmail, OtherEmail })
            {
                var login = accountBz.Login(email, TestPassword);
                if (login.Account != null)
                {
                    accountBz.DeleteAccount(login.Account.AccessToken);
                }
            }
        }

        [TestMethod]
        public void Owner_CanReadTheirOwnDevice()
        {
            var res = deviceService.GetDeviceByID(new Device { AccessToken = ownerToken, DeviceId = ownedDeviceId });
            Assert.AreEqual(ResponseStates.OK, res.State, res.Message);
            Assert.IsNotNull(res.Device);
        }

        [TestMethod]
        public void OtherAccount_CannotReadSomeoneElsesDeviceByID()
        {
            var res = deviceService.GetDeviceByID(new Device { AccessToken = otherToken, DeviceId = ownedDeviceId });
            Assert.AreEqual(ResponseStates.NOT_FOUND, res.State);
            Assert.IsNull(res.Device);
        }

        [TestMethod]
        public void OtherAccount_CannotReadSomeoneElsesDeviceByIdentifier()
        {
            var res = deviceService.GetDeviceByIdentifier(new Device { AccessToken = otherToken, Identifier = DeviceIdentifier });
            Assert.AreEqual(ResponseStates.NOT_FOUND, res.State);
        }

        [TestMethod]
        public void OtherAccount_CannotUpdateSomeoneElsesDevice()
        {
            var res = deviceService.UpdateDevice(new UpdateDeviceRequest
            {
                AccessToken = otherToken,
                DeviceId = ownedDeviceId,
                Identifier = DeviceIdentifier,
                Pin = "1234",
                Description = "Hijacked description"
            });
            Assert.AreEqual(ResponseStates.NOT_FOUND, res.State);
        }

        [TestMethod]
        public void OtherAccount_CannotDeleteSomeoneElsesDevice()
        {
            var res = deviceService.DeleteDevice(new Device { AccessToken = otherToken, DeviceId = ownedDeviceId });
            Assert.AreEqual(ResponseStates.NOT_FOUND, res.State);
        }

        [TestMethod]
        public void OtherAccount_CannotBlockSomeoneElsesPanel()
        {
            // BlockPIN's not-found/not-yours path returns this business-error message
            // (DeviceBusiness.BlockDevice) rather than InformUnauthorized -- distinct from
            // DeviceServiceAuthTesting's garbage-token case, which never gets this far.
            var message = deviceService.BlockPIN(new PinRequest
            {
                AccessToken = otherToken,
                Identifier = DeviceIdentifier,
                Action = "Block"
            });
            Assert.AreEqual("No se ha encontrado la central indicada", message);
        }

        [TestMethod]
        public void OtherAccount_CannotDisassociateSomeoneElsesDevice()
        {
            var res = deviceService.DisassociateCentral(new DeviceDisassociate
            {
                AccessToken = otherToken,
                Identifier = DeviceIdentifier
            });
            Assert.AreEqual(ResponseStates.NOT_FOUND, res.State);

            // And the owner's link must still be intact.
            var stillThere = deviceService.GetDeviceByID(new Device { AccessToken = ownerToken, DeviceId = ownedDeviceId });
            Assert.AreEqual(ResponseStates.OK, stillThere.State);
        }

        [TestMethod]
        public void Owner_CanDisassociateTheirOwnDevice()
        {
            // Uses its own disposable device rather than the shared ownedDeviceId other tests
            // in this class depend on.
            var deviceDao = new DeviceDao();
            var disposableId = deviceDao.CreateDevice(ownerToken, "Day5 disposable disassociate device", DeviceIdentifier + "B", "1234", 0);
            Assert.IsTrue(disposableId > 0, "Setup: could not create the disposable test device");

            var res = deviceService.DisassociateCentral(new DeviceDisassociate
            {
                AccessToken = ownerToken,
                Identifier = DeviceIdentifier + "B"
            });
            Assert.AreEqual(ResponseStates.OK, res.State, res.Message);
        }
    }
}
