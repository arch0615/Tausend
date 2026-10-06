using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tausend.Backend.Models;
using Tausend.Core.Business;
using Tausend.Core.Dao;
using Tausend.Core.Enums;
using Tausend.Core.Responses;

namespace UnitTesting
{
    // Day 1 (roles model, token expiry enforcement, refresh tokens) + Day 2 (bcrypt password
    // hashing, token-based password reset) integration coverage. Hits the real configured
    // database via AccountBusiness/AccountDao, the same way AccountTesting.cs does -- see
    // ConnectionStrings.config.example before pointing this at anything but a disposable test
    // database. Not run as part of this pass; see backend/DAY5_SUMMARY.md.
    [TestClass]
    public class AccountRoleAndTokenTesting
    {
        private const string TestEmail = "qa.day5.tokens@wearelomo.com";
        private const string TestPassword = "Day5Testing!1";

        private static AccountBusiness accountBz;
        private static AccountLoginResponse loginAtClassStart;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            accountBz = new AccountBusiness();
            // Tolerate a leftover account from a previous interrupted run.
            var existing = accountBz.Login(TestEmail, TestPassword);
            if (existing.Account != null)
            {
                accountBz.DeleteAccount(existing.Account.AccessToken);
            }

            var created = accountBz.CreateAccount(new Account
            {
                Email = TestEmail,
                Password = TestPassword,
                FirstName = "Day5",
                LastName = "Tokens"
            });
            Assert.AreEqual(0, created.Code, "Setup: account creation failed -- " + created.Message);

            loginAtClassStart = accountBz.Login(TestEmail, TestPassword);
            Assert.IsNotNull(loginAtClassStart.Account, "Setup: could not log into the freshly created test account");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            var login = accountBz.Login(TestEmail, TestPassword);
            if (login.Account != null)
            {
                accountBz.DeleteAccount(login.Account.AccessToken);
            }
        }

        [TestMethod]
        public void NewAccountDefaultsToEndUserRole()
        {
            // Day 1: roles model -- a freshly created account must not be Admin/Installer by default.
            Assert.AreEqual(AccountRole.EndUser, loginAtClassStart.Account.Role);
        }

        [TestMethod]
        public void LoginIssuesWorkingAccessAndRefreshTokens()
        {
            Assert.IsFalse(string.IsNullOrEmpty(loginAtClassStart.Account.AccessToken));
            Assert.IsFalse(string.IsNullOrEmpty(loginAtClassStart.Account.RefreshToken));
            Assert.IsTrue(accountBz.ValidateAccessToken(loginAtClassStart.Account.AccessToken));
        }

        [TestMethod]
        public void GarbageAccessTokenDoesNotValidate()
        {
            Assert.IsFalse(accountBz.ValidateAccessToken("not-a-real-token"));
            Assert.IsFalse(accountBz.ValidateAccessToken(null));
            Assert.IsFalse(accountBz.ValidateAccessToken(""));
        }

        [TestMethod]
        public void RefreshTokenIssuesANewWorkingAccessToken()
        {
            // Uses its own login rather than the shared loginAtClassStart -- if the refresh
            // stored procedure rotates/invalidates the old access token, reusing the
            // class-level one here would make other tests fail depending on run order.
            var ownLogin = accountBz.Login(TestEmail, TestPassword);
            var refreshed = accountBz.RefreshAccessToken(ownLogin.Account.RefreshToken);
            Assert.AreEqual(ResponseStates.OK, refreshed.State, refreshed.Message);
            Assert.IsNotNull(refreshed.Account);
            Assert.IsFalse(string.IsNullOrEmpty(refreshed.Account.AccessToken));
            Assert.IsTrue(accountBz.ValidateAccessToken(refreshed.Account.AccessToken));
        }

        [TestMethod]
        public void GarbageRefreshTokenIsRejected()
        {
            var refreshed = accountBz.RefreshAccessToken("not-a-real-refresh-token");
            Assert.AreEqual(ResponseStates.UNAUTHORIZED, refreshed.State);
            Assert.IsNull(refreshed.Account);
        }

        [TestMethod]
        public void ChangePasswordRejectsWrongOldPassword()
        {
            var login = accountBz.Login(TestEmail, TestPassword);
            var res = accountBz.UpdateAccount(login.Account.AccessToken, "definitely-the-wrong-password", "NewPass!2");
            Assert.AreEqual(3001, res.Code, res.Message);
        }

        [TestMethod]
        public void PasswordResetRoundTripLogsInWithNewPassword()
        {
            // Bypasses AccountBusiness.UpdateAccountPassword (which only e-mails the link) to
            // pull the token directly from the DAO, so this test doesn't send a real e-mail.
            var dao = new AccountDao();
            var tokenInfo = dao.CreatePasswordResetToken(TestEmail);
            Assert.IsNotNull(tokenInfo, "Setup: no reset token was issued for " + TestEmail);

            const string newPassword = "AfterReset!3";
            var reset = accountBz.ResetPassword(tokenInfo.ResetToken, newPassword);
            Assert.AreEqual(ResponseStates.OK, reset.State, reset.Message);

            var loginWithNewPassword = accountBz.Login(TestEmail, newPassword);
            Assert.IsNotNull(loginWithNewPassword.Account, "Could not log in with the password that was just reset");

            // Leave the account in a known state for any later test in this class.
            accountBz.UpdateAccount(loginWithNewPassword.Account.AccessToken, newPassword, TestPassword);
        }

        [TestMethod]
        public void GarbageResetTokenIsRejected()
        {
            var reset = accountBz.ResetPassword("not-a-real-reset-token", "WhateverPassword!4");
            Assert.AreEqual(3002, reset.Code, reset.Message);
        }

        [TestMethod]
        public void PasswordHashingUsesBcryptNotLegacySha256()
        {
            // Day 2: bcrypt replacing unsalted SHA-256. No DB involved -- pure round-trip check.
            var hash = Tausend.Core.Security.PasswordHasher.HashPassword("SamplePassword!5");
            StringAssert.StartsWith(hash, "$2", "Hash doesn't look like a bcrypt hash");
            Assert.IsTrue(Tausend.Core.Security.PasswordHasher.VerifyBcrypt("SamplePassword!5", hash));
            Assert.IsFalse(Tausend.Core.Security.PasswordHasher.VerifyBcrypt("WrongPassword", hash));
        }
    }
}
