using Emby.Notifications;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SmtpNotifications
{
    public class Notifier : INotifier
    {
        private IServerConfigurationManager _config;
        private ILogger _logger;

        public static string TestNotificationId = "system.emailnotificationtest";
        public Notifier(IServerConfigurationManager config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public string Name
        {
            get { return Plugin.StaticName; }
        }

        public async Task SendNotification(InternalNotificationRequest request, CancellationToken cancellationToken)
        {
            var options = request.Configuration as EmailNotificationInfo;

            using (var mail = new MailMessage(options.EmailFrom, options.EmailTo)
            {
                Subject = "Emby: " + request.Title,
                Body = string.Format("{0}\n\n{1}", request.Title, request.Description)
            })
            {
                using (var client = new SmtpClient
                {
                    Host = options.Server,
                    Port = options.Port,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 20000
                })
                {
                    if (options.EnableSSL) client.EnableSsl = true;

                    _logger.Info("Sending email {0} with subject {1}", options.EmailTo, mail.Subject);

                    if (!string.IsNullOrEmpty(options.Username))
                    {
                        client.Credentials = new NetworkCredential(options.Username, options.Password);
                    }

                    try
                    {
                        await client.SendMailAsync(mail).ConfigureAwait(false);

                        _logger.Info("Completed sending email {0} with subject {1}", options.EmailTo, mail.Subject);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
        }

        public NotificationInfo[] GetConfiguredNotifications()
        {
            return _config.GetConfiguredNotifications();
        }
    }
}
