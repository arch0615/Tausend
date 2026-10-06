using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using Tausend.Core.Models;

namespace Tausend.Core.Business
{
    public class EmailBusiness
    {
        EmailSettings settings;
        public EmailBusiness()
        {
            settings = GetEmailSettings();
        }

        public bool SendRecoverPasswordEmail(string accountEmail, string accountName, string password)
        {
            var email = new Email()
            {
                FromEmail = settings.SenderEmail,
                FromName = settings.SenderName,
                ToEmail = accountEmail,
                ToName = accountName,
                Subject = "Recuperación de contraseña",
                Body = "Su nueva contraseña es " + password
            };
            return SendEMail(email);
        }

        public bool SendPasswordResetEmail(string accountEmail, string accountName, string resetLink)
        {
            var email = new Email()
            {
                FromEmail = settings.SenderEmail,
                FromName = settings.SenderName,
                ToEmail = accountEmail,
                ToName = accountName,
                Subject = "Recuperación de contraseña",
                Body = "Para restablecer su contraseña, ingrese al siguiente enlace (válido por 1 hora): " +
                       "<a href=\"" + resetLink + "\">" + resetLink + "</a>"
            };
            return SendEMail(email);
        }

        private bool SendEMail(Email email)
        {
            var result = false;
            try
            {
                using (var Msg = new MailMessage())
                {
                    Msg.From = new MailAddress(email.FromEmail, email.FromName);
                    Msg.Sender = new MailAddress(email.FromEmail, email.FromName);
                    Msg.To.Add(email.ToEmail);
                    Msg.Subject = email.Subject;
                    Msg.Body = email.Body;
                    Msg.IsBodyHtml = true;

                    // FIX: Bypass SSL Certificate Validation (for dev/test environments or self-signed certs)
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;

                    using (var Smtp = new SmtpClient(settings.SMTPHostName, settings.SMTPPort))
                    {
                        Smtp.UseDefaultCredentials = false;
                        Smtp.Credentials = new NetworkCredential(settings.SenderEmail, settings.SenderPassword, "");
                        Smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        Smtp.DeliveryFormat = SmtpDeliveryFormat.International;
                        Smtp.EnableSsl = settings.UseSSL;

                        Smtp.Send(Msg);
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // FIX: Log the error to debug output
                System.Diagnostics.Trace.WriteLine($"[EmailBusiness] Error sending email: {ex.Message}");
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
            }
            return result;
        }

        private EmailSettings GetEmailSettings()
        {
            return new EmailSettings()
            {
                SenderEmail = "recupero@alarmastausend.com",
                SenderPassword = ConfigurationManager.AppSettings["SmtpSenderPassword"],
                SMTPHostName = "smtp.mydomain.com",
                SMTPPort = 587
            };
        }
    }
}