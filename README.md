Emby.Plugins Email Notifications
====================

Email notifications is a core Emby Plugin that allows the administrator to send emails to specified email addresses with various events within Emby.  

Version 4.0.0.4 has been released to rectify the issues surrounding the dotnetcore SmtpClient library, which unfortunately does not support many of today's current secure email protocols.

This plugin was rewritten to utilise MailKit/MimeKit at its core, which rectifies these issues and provides much more indepth protocol and email provider support.

## More Information ##

[How to Build a Server Plugin](https://github.com/MediaBrowser/MediaBrowser/wiki/How-to-build-a-Server-Plugin)
