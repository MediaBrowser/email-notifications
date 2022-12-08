define(['globalize', 'pluginManager', 'emby-input'], function (globalize, pluginManager) {
    'use strict';

    function EntryEditor() {
    }

    EntryEditor.setObjectValues = function (context, entry) {

        entry.Options.EmailFrom = context.querySelector('.txtEmailFrom').value;
        entry.Options.EmailTo = context.querySelector('.txtEmailTo').value;
        entry.Options.Server = context.querySelector('.txtServer').value;
        entry.Options.Port = context.querySelector('.txtPort').value || null;
        entry.Options.EnableSSL = context.querySelector('.chkEnableSSL').checked;
        entry.Options.Username = context.querySelector('.txtUsername').value;
        entry.Options.Password = context.querySelector('.txtPassword').value;
    };

    EntryEditor.setFormValues = function (context, entry) {

        context.querySelector('.txtEmailFrom').value = entry.Options.EmailFrom || '';
        context.querySelector('.txtEmailTo').value = entry.Options.EmailTo || '';
        context.querySelector('.txtServer').value = entry.Options.Server || '';
        context.querySelector('.txtPort').value = entry.Options.Port || '';
        context.querySelector('.chkEnableSSL').checked = entry.Options.EnableSSL;
        context.querySelector('.txtUsername').value = entry.Options.Username || '';
        context.querySelector('.txtPassword').value = entry.Options.Password || '';
    };

    EntryEditor.loadTemplate = function (context) {

        return require(['text!' + pluginManager.getConfigurationResourceUrl('emaileditortemplate')]).then(function (responses) {

            var template = responses[0];
            context.innerHTML = globalize.translateDocument(template);

            // setup any required event handlers here
        });
    };

    EntryEditor.destroy = function () {

    };

    return EntryEditor;
});