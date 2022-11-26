using Emby.Notifications;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using System.Collections.Generic;
using System;

namespace MediaBrowser.Plugins.SmtpNotifications
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class NotificationsConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new ConfigurationStore
                {
                     ConfigurationType = typeof(EmailNotificationsOptions),
                     Key = "emailnotifications"
                }
            };
        }
    }
    public static class NotificationsConfigExtension
    {
        public static EmailNotificationsOptions GetNotificationsOptions(this IConfigurationManager config)
        {
            return config.GetConfiguration<EmailNotificationsOptions>("emailnotifications");
        }

        public static NotificationInfo[] GetConfiguredNotifications(this IConfigurationManager config)
        {
            return config.GetNotificationsOptions().Notifications;
        }

        public static void SaveNotificationsConfiguration(this IConfigurationManager config, EmailNotificationsOptions options)
        {
            config.SaveConfiguration("emailnotifications", options);
        }
    }

    public class EmailNotificationsOptions
    {
        public EmailNotificationInfo[] Notifications { get; set; } = Array.Empty<EmailNotificationInfo>();
    }

    public class EmailNotificationInfo : NotificationInfo
    {
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string Server { get; set; }
        public int Port { get; set; } = 25;
        public string Username { get; set; }
        public string Password { get; set; }
        public bool EnableSSL { get; set; } = false;
    }
}
