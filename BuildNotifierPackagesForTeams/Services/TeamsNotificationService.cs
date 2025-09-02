using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using BuildNotifierPackagesForTeams.Models;
using Newtonsoft.Json;

namespace BuildNotifierPackagesForTeams.Services
{
    public class TeamsNotificationService
    {
        private const string SETTINGS_COLLECTION = "BuildNotifierForTeams";
        private const string WEBHOOK_URL_KEY = "WebhookUrl";
        private const string ENABLED_KEY = "Enabled";
        private const string MENTION_USER_KEY = "MentionUser";

        private readonly HttpClient httpClient;
        private WritableSettingsStore settingsStore;

        public TeamsNotificationService()
        {
            httpClient = new HttpClient();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(SETTINGS_COLLECTION))
            {
                settingsStore.CreateCollection(SETTINGS_COLLECTION);
            }
        }

        public string GetWebhookUrl()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return settingsStore.PropertyExists(SETTINGS_COLLECTION, WEBHOOK_URL_KEY) 
                ? settingsStore.GetString(SETTINGS_COLLECTION, WEBHOOK_URL_KEY) 
                : string.Empty;
        }

        public void SetWebhookUrl(string url)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            settingsStore.SetString(SETTINGS_COLLECTION, WEBHOOK_URL_KEY, url ?? string.Empty);
        }

        public bool IsEnabled()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return settingsStore.PropertyExists(SETTINGS_COLLECTION, ENABLED_KEY) 
                ? settingsStore.GetBoolean(SETTINGS_COLLECTION, ENABLED_KEY) 
                : false;
        }

        public void SetEnabled(bool enabled)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            settingsStore.SetBoolean(SETTINGS_COLLECTION, ENABLED_KEY, enabled);
        }

        public string GetMentionUser()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return settingsStore.PropertyExists(SETTINGS_COLLECTION, MENTION_USER_KEY) 
                ? settingsStore.GetString(SETTINGS_COLLECTION, MENTION_USER_KEY) 
                : string.Empty;
        }

        public void SetMentionUser(string userName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            settingsStore.SetString(SETTINGS_COLLECTION, MENTION_USER_KEY, userName ?? string.Empty);
        }

        public async Task SendBuildNotificationAsync(BuildResult result)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (!IsEnabled())
                    return;

                var webhookUrl = GetWebhookUrl();
                if (string.IsNullOrEmpty(webhookUrl))
                    return;

                var json = CreateTeamsMessageJson(result);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await httpClient.PostAsync(webhookUrl, content);
            }
            catch (Exception ex)
            {
                // Log error (in real implementation, you might want to show this to user)
                System.Diagnostics.Debug.WriteLine($"Failed to send Teams notification: {ex.Message}");
            }
        }

        private string CreateTeamsMessageJson(BuildResult result)
        {
            var statusText = result.Success ? "Success" : "Failed";
            var buildTimeText = $"{result.BuildTime.TotalSeconds:F1}s";
            var mentionUser = GetMentionUser();
            
            // 멘션 텍스트 생성
            var mentionText = string.IsNullOrWhiteSpace(mentionUser) 
                ? "" 
                : $"<at>{mentionUser}</at> ";

            var titleText = $"Visual Studio Build Completed";
            
            var message = new
            {
                type = "message",
                attachments = new object[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        content = new
                        {
                            type = "AdaptiveCard",
                            version = "1.2",
                            body = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    text = titleText,
                                    weight = "Bolder",
                                    size = "Medium"
                                },
                                new
                                {
                                    type = "FactSet",
                                    facts = new object[]
                                    {
                                        new { title = "User", value = mentionText },
                                        new { title = "Project", value = result.ProjectName },
                                        new { title = "Status", value = statusText },
                                        new { title = "Build Time", value = buildTimeText },
                                        new { title = "Completed", value = result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") }
                                    }
                                }
                            },
                            // 멘션이 있을 경우 msteams 정보 추가
                            msteams = string.IsNullOrWhiteSpace(mentionUser) ? null : new
                            {
                                entities = new object[]
                                {
                                    new
                                    {
                                        type = "mention",
                                        text = $"<at>{mentionUser}</at>",
                                        mentioned = new
                                        {
                                            id = mentionUser,
                                            name = mentionUser
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return JsonConvert.SerializeObject(message, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}