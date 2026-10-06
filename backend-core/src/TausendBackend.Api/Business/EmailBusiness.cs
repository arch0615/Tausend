using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Business
{
    // Sends via SendGrid's HTTPS API (api.sendgrid.com:443) instead of SMTP. The production
    // server's hosting provider (DigitalOcean) blocks outbound SMTP ports (25/465/587) on all
    // droplets by default as an anti-spam policy and, per their support response, does not lift
    // that restriction on request -- every SmtpClient.Send() call here used to hang until timeout,
    // which is what surfaced in the app as "No se pudo conectar con el servidor" on every
    // password-recovery attempt. HTTPS (443) is already open on this server for the API itself,
    // so routing mail through an HTTP API sidesteps the block entirely.
    public class EmailBusiness
    {
        private const string SendGridEndpoint = "https://api.sendgrid.com/v3/mail/send";

        private readonly EmailSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailBusiness(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _settings = new EmailSettings
            {
                SenderEmail = configuration["Smtp:SenderEmail"] ?? "recupero@alarmastausend.com",
                SenderName = configuration["Smtp:SenderName"] ?? "Alarmas Tausend",
                SendGridApiKey = configuration["SendGrid:ApiKey"]
            };
        }

        public bool SendRecoverPasswordEmail(string accountEmail, string accountName, string password)
        {
            var email = new Email
            {
                FromEmail = _settings.SenderEmail,
                FromName = _settings.SenderName,
                ToEmail = accountEmail,
                ToName = accountName,
                Subject = "Recuperación de contraseña",
                Body = "Su nueva contraseña es " + password
            };
            return SendEmail(email);
        }

        public bool SendPasswordResetEmail(string accountEmail, string accountName, string resetLink)
        {
            var email = new Email
            {
                FromEmail = _settings.SenderEmail,
                FromName = _settings.SenderName,
                ToEmail = accountEmail,
                ToName = accountName,
                Subject = "Recuperación de contraseña",
                Body = "Para restablecer su contraseña, ingrese al siguiente enlace (válido por 1 hora): " +
                       "<a href=\"" + resetLink + "\">" + resetLink + "</a>"
            };
            return SendEmail(email);
        }

        private bool SendEmail(Email email)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.SendGridApiKey))
                {
                    System.Diagnostics.Trace.WriteLine("[EmailBusiness] SendGrid:ApiKey is not configured.");
                    return false;
                }

                var payload = new
                {
                    personalizations = new[]
                    {
                        new { to = new[] { new { email = email.ToEmail, name = email.ToName } } }
                    },
                    from = new { email = email.FromEmail, name = email.FromName },
                    subject = email.Subject,
                    content = new[] { new { type = "text/html", value = email.Body } }
                };
                var json = JsonSerializer.Serialize(payload);

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _settings.SendGridApiKey);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                // SendGrid's own success response is "202 Accepted" with an empty body.
                var response = client.PostAsync(SendGridEndpoint, content).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    System.Diagnostics.Trace.WriteLine(
                        $"[EmailBusiness] SendGrid error {(int)response.StatusCode}: {body}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[EmailBusiness] Error sending email: {ex.Message}");
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}
