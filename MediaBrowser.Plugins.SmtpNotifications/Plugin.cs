using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.SmtpNotifications.Configuration;
using MediaBrowser.Model.Drawing;
using System.IO;
using System.Reflection;

namespace MediaBrowser.Plugins.SmtpNotifications
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        private readonly IEncryptionManager _encryption;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IEncryptionManager encryption)
            : base(applicationPaths, xmlSerializer)
        {
            _encryption = encryption;
            Instance = this;

            /* For dependency injection as MailKit and MimeKit are rather large codebases */
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /* Pointer to both injected dependencies; required to avoid multiple instance injection */
        private Assembly _mimeKitAssembly;
        private Assembly _mailKitAssembly;

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            switch (args.Name)
            {

                case "MailKit, Version=2.7.0.0, Culture=neutral, PublicKeyToken=4e064fe7c44a8f1b":

                    if (_mailKitAssembly != null)
                        return (_mailKitAssembly);

                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType().Namespace + ".Dependencies.MailKit.dll"))
                    {
                        var assemblyData = new Byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        _mailKitAssembly = Assembly.Load(assemblyData);
                        return (_mailKitAssembly);
                    }
                    break;

                case "MimeKit, Version=2.8.0.0, Culture=neutral, PublicKeyToken=bede1c8a46c66814":

                    if (_mimeKitAssembly != null)
                        return (_mimeKitAssembly);

                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType().Namespace + ".Dependencies.MimeKit.dll"))
                    {

                        var assemblyData = new Byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        _mimeKitAssembly = Assembly.Load(assemblyData);
                        return (_mimeKitAssembly);
                    }
                    break;
            }

            return (null);
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
                }
            };
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.jpg");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Jpg;
            }
        }

        private Guid _id = new Guid("b9f0c474-e9a8-4292-ae41-eb3c1542f4cd");
        public override Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Email Notifications"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Sends notifications via email.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }
    }
}
