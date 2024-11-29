using Emby.Notifications;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using MediaBrowser.Controller;
using System.Collections.Concurrent;

namespace MediaBrowser.Plugins.SmtpNotifications
{
    public class Notifier : IUserNotifier, INotifierWithDefaultOptions
    {
        private ILogger _logger;
        private IServerApplicationHost _appHost;

        public Notifier(ILogger logger, IServerApplicationHost applicationHost)
        {
            _logger = logger;
            _appHost = applicationHost;
        }

        private Plugin Plugin => _appHost.Plugins.OfType<Plugin>().First();

        public string Name => Plugin.StaticName;

        public string Key => "emailnotifications";

        public string SetupModuleUrl => Plugin.NotificationSetupModuleUrl;

        private int DefaultPort = 25;
        private ConcurrentDictionary<string, SmtpClient> Clients = new ConcurrentDictionary<string, SmtpClient>(StringComparer.OrdinalIgnoreCase);

        private SmtpClient GetSmtpClient(InternalNotificationRequest request)
        {
            var keys = new List<string>();
            var key = string.Join("-", keys.ToArray());

            return Clients.GetOrAdd(key, (k) =>
            {

                var options = request.Configuration.Options;

                options.TryGetValue("EmailFrom", out string emailFrom);
                options.TryGetValue("EmailTo", out string emailTo);

                options.TryGetValue("Username", out string username);
                options.TryGetValue("Password", out string password);

                options.TryGetValue("Server", out string server);

                options.TryGetValue("EnableSSL", out string enableSSLString);
                var enableSSL = string.Equals(enableSSLString, "true", StringComparison.OrdinalIgnoreCase);

                options.TryGetValue("Port", out string portString);
                if (!int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
                {
                    port = DefaultPort;
                }

                var client = new SmtpClient()
                {
                    Host = server,
                    Port = port,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 20000
                };

                if (enableSSL) client.EnableSsl = true;

                if (!string.IsNullOrEmpty(username))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }

                return client;
            });
        }

        public async Task SendNotification(InternalNotificationRequest request, CancellationToken cancellationToken)
        {
            var options = request.Configuration.Options;

            options.TryGetValue("EmailFrom", out string emailFrom);
            options.TryGetValue("EmailTo", out string emailTo);

            var body = string.Format("{0}\n\n{1}\n\n{2}", request.Title, request.Date.ToLocalTime().ToString("f"), request.Description);

            using (var mail = new MailMessage(emailFrom, emailTo)
            {
                Subject = request.Title,
                Body = body
            })
            {
                await GetSmtpClient(request).SendMailAsync(mail).ConfigureAwait(false);
            }
        }

        public Dictionary<string, string> GetDefaultOptions()
        {
            var options = new Dictionary<string, string>();

            options["Port"] = DefaultPort.ToString(CultureInfo.InvariantCulture);

            return options;
        }
    }
}
