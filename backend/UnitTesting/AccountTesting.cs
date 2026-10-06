using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tausend.Backend.Models;
using Tausend.Core.Business;

namespace UnitTesting
{
    [TestClass]
    public class AccountTesting
    {
        [TestMethod]
        public void Test000CreateAccount()
        {
            try
            {
                AccountBusiness bz = new AccountBusiness();
                Account account = new Account()
                {
                    Email = "federico@wearelomo.com",
                    Password = "123456",
                    FirstName = "Federico",
                    LastName = "Casabona"
                };

                var response = bz.CreateAccount(account);
                if (response.Code == 1000)
                {
                    Assert.Fail("La cuenta ya existe");
                }
                else if (response.Code == 11111)
                {
                    Assert.Fail("No se logró conectar a la base de datos.");
                }
            }
            catch (Exception e)
            {
                Assert.Fail("Ocurrio el siguiente error: " + e.Message);
            }
        }

        [TestMethod]
        public void Test001ValidateExistingMail()
        {
            try
            {
                AccountBusiness bz = new AccountBusiness();
                Account account = new Account()
                {
                    Email = "federico@wearelomo.com",
                    Password = "123456",
                    FirstName = "Federico",
                    LastName = "Casabona"
                };

                var response = bz.CreateAccount(account);
                Assert.IsTrue(response.Code == 1000);
            }
            catch (Exception e)
            {
                Assert.Fail("Ocurrio el siguiente error: " + e.Message);
            }
        }

        [TestMethod]
        public void Test002DeleteAccount()
        {
            try
            {
                AccountBusiness bz = new AccountBusiness();
                var login = bz.Login("federico@wearelomo.com", "123456");
                var response = bz.DeleteAccount(login.Account.AccessToken);
                Assert.IsTrue(response.Code == 0);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}
