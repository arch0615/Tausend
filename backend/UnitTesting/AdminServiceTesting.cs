using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tausend.Backend.Models;
using Tausend.Core.Business;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Enums;
using Tausend.Core.Services;

namespace UnitTesting
{
    // Day 4 PM: AdminService (fleet-wide account/device listing, role assignment), gated by
    // AccountBusiness.IsAdmin. Bootstraps an Admin account directly via AdminDao.SetAccountRole
    // (bypassing the auth-gated business method on purpose -- that's what's under test) rather
    // than needing a pre-existing admin in the DB. Not run as part of this pass; see
    // backend/DAY5_SUMMARY.md.
    [TestClass]
    public class AdminServiceTesting
    {
        private const string AdminEmail = "qa.day5.admin@wearelomo.com";
        private const string NonAdminEmail = "qa.day5.nonadmin@wearelomo.com";
        private const string TestPassword = "Day5Testing!1";

        private static AccountBusiness accountBz;
        private static AdminService adminService;
        private static string adminToken;
        private static string nonAdminToken;
        private static long nonAdminAccountId;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            accountBz = new AccountBusiness();
            adminService = new AdminService();

            adminToken = EnsureTestAccount(AdminEmail, "Day5", "Admin");
            var adminDao = new AdminDao();
            var adminAccount = new AccountDao().GetAccount(adminToken);
            adminDao.SetAccountRole(adminAccount.AccountId, AccountRole.Admin);
            // Token was already issued before the role change; re-login to pick up the new role
            // the same way a real client would after being promoted.
            var adminLogin = accountBz.Login(AdminEmail, TestPassword);
            Assert.AreEqual(AccountRole.Admin, adminLogin.Account.Role, "Setup: admin promotion didn't take");
            adminToken = adminLogin.Account.AccessToken;

            nonAdminToken = EnsureTestAccount(NonAdminEmail, "Day5", "NonAdmin");
            nonAdminAccountId = new AccountDao().GetAccount(nonAdminToken).AccountId;
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
            foreach (var email in new[] { AdminEmail, NonAdminEmail })
            {
                var login = accountBz.Login(email, TestPassword);
                if (login.Account != null)
                {
                    accountBz.DeleteAccount(login.Account.AccessToken);
                }
            }
        }

        [TestMethod]
        public void EnumAllAccounts_AsAdmin_Succeeds()
        {
            var res = adminService.EnumAllAccounts(new AccessTokenRequest { AccessToken = adminToken });
            Assert.AreEqual(ResponseStates.OK, res.State, res.Message);
            Assert.IsNotNull(res.Accounts);
        }

        [TestMethod]
        public void EnumAllAccounts_AsNonAdmin_IsForbidden()
        {
            var res = adminService.EnumAllAccounts(new AccessTokenRequest { AccessToken = nonAdminToken });
            Assert.AreEqual(ResponseStates.FORBIDDEN, res.State);
        }

        [TestMethod]
        public void EnumAllAccounts_WithGarbageToken_IsUnauthorizedNotForbidden()
        {
            // The bug this pins: AdminBusiness used to call IsAdmin() directly, which collapses
            // "not logged in" and "logged in but not admin" into the same Forbidden response --
            // contradicting the Day 4 commit's own stated intent (and the dashboard's
            // ResponseState type, which already distinguishes UNAUTHORIZED from FORBIDDEN).
            var res = adminService.EnumAllAccounts(new AccessTokenRequest { AccessToken = "not-a-real-token" });
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, res.State);
        }

        [TestMethod]
        public void EnumAllDevices_AsNonAdmin_IsForbidden()
        {
            var res = adminService.EnumAllDevices(new AccessTokenRequest { AccessToken = nonAdminToken });
            Assert.AreEqual(ResponseStates.FORBIDDEN, res.State);
        }

        [TestMethod]
        public void SetAccountRole_AsNonAdmin_IsForbiddenAndDoesNotChangeRole()
        {
            var res = adminService.SetAccountRole(new SetAccountRoleRequest
            {
                AccessToken = nonAdminToken,
                AccountId = nonAdminAccountId,
                Role = AccountRole.Admin
            });
            Assert.AreEqual(ResponseStates.FORBIDDEN, res.State);

            var stillNonAdmin = accountBz.Login(NonAdminEmail, TestPassword);
            Assert.AreEqual(AccountRole.EndUser, stillNonAdmin.Account.Role, "A forbidden request must not have changed the role");
        }

        [TestMethod]
        public void SetAccountRole_AsAdmin_PromotesTarget()
        {
            var res = adminService.SetAccountRole(new SetAccountRoleRequest
            {
                AccessToken = adminToken,
                AccountId = nonAdminAccountId,
                Role = AccountRole.Installer
            });
            Assert.AreEqual(ResponseStates.OK, res.State, res.Message);

            var promoted = accountBz.Login(NonAdminEmail, TestPassword);
            Assert.AreEqual(AccountRole.Installer, promoted.Account.Role);

            // Restore for any other test in this class that assumes EndUser.
            new AdminDao().SetAccountRole(nonAdminAccountId, AccountRole.EndUser);
        }

        [TestMethod]
        public void SetAccountRole_OnNonexistentAccount_ReportsNotFound()
        {
            var res = adminService.SetAccountRole(new SetAccountRoleRequest
            {
                AccessToken = adminToken,
                AccountId = -1,
                Role = AccountRole.EndUser
            });
            Assert.AreEqual(ResponseStates.NOT_FOUND, res.State);
        }
    }
}
