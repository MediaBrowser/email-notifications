using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.SmtpNotifications.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

/* NB:
 * 
 * We don't recommend that you use the SmtpClient class for new development because SmtpClient doesn't support many 
 * modern protocols. Use MailKit or other libraries instead. For more information, see SmtpClient shouldn't be 
 * used on GitHub.
 * 
 * Rewritten to utilise MailKit v2.7 & MimeKit v2.8
 * 
 * Anthony Musgrove, 2nd June 2020
 */
using MailKit;
using MimeKit;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Data.SqlTypes;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Plugins.SmtpNotifications
{
    public class Notifier : INotificationService
    {
        private readonly IEncryptionManager _encryption;
        private readonly ILogger _logger;
        public static Notifier Instance { get; private set; }

        public Notifier(ILogger logger, IEncryptionManager encryption)
        {
            _encryption = encryption;
            _logger = logger;

            Instance = this;
        }

        public bool IsEnabledForUser(User user)
        {
            var options = GetOptions(user);

            return options != null && IsValid(options) && options.Enabled;
        }

        private SMTPOptions GetOptions(User user)
        {
            try
            {
                return Plugin.Instance.Configuration.Options
                    .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, user.Id.ToString("N"), StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting SMTP options for userId '{0}': {1}", user.Id, ex.Message);
                return (null);
            }
        }

        public string Name
        {
            get { return Plugin.Instance.Name; }
        }

        public async Task SendNotification(UserNotification request, CancellationToken cancellationToken)
        {
            var options = GetOptions(request.User);

            var mail = new MimeMessage(); var bodyBuilder = new BodyBuilder();

            try
            {
                mail.From.Add(new MailboxAddress(options.EmailFromName, options.EmailFrom));
                mail.To.Add(new MailboxAddress("", options.EmailTo));
            }
            catch (Exception ex)
            {
                _logger.Error("Error creating Mailbox Address objects for from:{0} or to:{1} (error: {2})", options.EmailFrom, options.EmailTo, ex.Message);

                /* If testing SMTP, push exception to API caller */
                if (request.Name == "Test Notification")
                    throw ex;
            }

            mail.Subject = "Emby: " + request.Name;

            bodyBuilder.HtmlBody = string.Format("{0}<br/><br/>{1}", request.Name, request.Description);
            bodyBuilder.TextBody = string.Format("{0}\n\n{1}", request.Name, request.Description);

            mail.Body = bodyBuilder.ToMessageBody();

            var client = new MailKit.Net.Smtp.SmtpClient();

            if (options.IgnoreCertificateErrors.GetValueOrDefault(false))
                client.ServerCertificateValidationCallback = this.sslCertificateValidationCallback;

            client.Timeout = 20000;

            try
            {
                _logger.Info("Connecting to smtpserver at {0}:{1}", options.Server, options.Port);

                await client.ConnectAsync(options.Server, options.Port, options.SSL).ConfigureAwait(false);

                if (options.UseCredentials)
                {
                    _logger.Info("Authenticating to smtpserver using {0}/<hidden>", options.Username);

                    var pw = string.IsNullOrWhiteSpace(options.Password) ? _encryption.DecryptString(options.PwData) : options.Password;

                    try
                    {
                        await client.AuthenticateAsync(new NetworkCredential(options.Username, pw)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Info("Failed to authenticate to SMTP server: {0}", ex.Message);

                        /* If testing SMTP, push exception to API caller */
                        if (request.Name == "Test Notification")
                            throw ex;
                    }
                }

                _logger.Info("Sending email {0} with subject {1}", options.EmailTo, mail.Subject);

                await client.SendAsync(mail, default, null).ConfigureAwait(false);

                _logger.Info("Completed sending email {0} with subject {1}", options.EmailTo, mail.Subject);
            }
            catch (Exception ex)
            {
                _logger.Error("Error sending email: {0} ", ex);

                /* If testing SMTP, push exception to API caller */
                if (request.Name == "Test Notification")
                    throw ex;
            }
        }


        private bool sslCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return (true);
        }

        private bool IsValid(SMTPOptions options)
        {
            return !string.IsNullOrEmpty(options.EmailFrom) &&
                   !string.IsNullOrEmpty(options.EmailTo) &&
                   !string.IsNullOrEmpty(options.Server);
        }
    }
}
