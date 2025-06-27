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
        private ConcurrentDictionary<string, SmtpClientInfo> Clients = new ConcurrentDictionary<string, SmtpClientInfo>(StringComparer.OrdinalIgnoreCase);

        private SmtpClientInfo GetSmtpClient(InternalNotificationRequest request)
        {
            var keys = new List<string>();

            var options = request.Configuration.Options;

            options.TryGetValue("Server", out string server);
            options.TryGetValue("Port", out string portString);

            if (!int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
            {
                port = DefaultPort;
            }

            keys.Add(server);
            keys.Add(port.ToString(CultureInfo.InvariantCulture));

            options.TryGetValue("Username", out string username);
            options.TryGetValue("Password", out string password);

            keys.Add(username ?? string.Empty);
            keys.Add(password ?? string.Empty);

            options.TryGetValue("EnableSSL", out string enableSSLString);
            var enableSSL = string.Equals(enableSSLString, "true", StringComparison.OrdinalIgnoreCase);

            keys.Add(enableSSL.ToString());

            var key = string.Join("-", keys.ToArray());

            return Clients.GetOrAdd(key, (k) =>
            {
                options.TryGetValue("EmailFrom", out string emailFrom);
                options.TryGetValue("EmailTo", out string emailTo);

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

                return new SmtpClientInfo
                {
                    SmtpClient = client,
                };
            });
        }

        public async Task SendNotification(InternalNotificationRequest request, CancellationToken cancellationToken)
        {
            var options = request.Configuration.Options;

            options.TryGetValue("EmailFrom", out string emailFrom);
            options.TryGetValue("EmailTo", out string emailTo);

            var body = string.Format("{0}\n\n{1}", request.Title, request.Description);

            using (var mail = new MailMessage(emailFrom, emailTo)
            {
                Subject = request.Title,
                Body = body
            })
            {
                var clientInfo = GetSmtpClient(request);

                // https://emby.media/community/index.php?/topic/134277-email-notifications-3190/#comment-1405314
                await clientInfo.ResourceLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    await clientInfo.SmtpClient.SendMailAsync(mail).ConfigureAwait(false);
                }
                finally
                {
                    clientInfo.ResourceLock.Release();
                }
            }
        }

        public Dictionary<string, string> GetDefaultOptions()
        {
            var options = new Dictionary<string, string>();

            options["Port"] = DefaultPort.ToString(CultureInfo.InvariantCulture);

            return options;
        }
    }

    internal class SmtpClientInfo
    {
        public SmtpClient SmtpClient;
        public SemaphoreSlim ResourceLock = new SemaphoreSlim(1, 1);
    }
}