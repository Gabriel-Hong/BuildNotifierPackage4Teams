using System;
using System.Threading.Tasks;
using System.Windows;
using BuildNotifierPackagesForTeams.Models;
using BuildNotifierPackagesForTeams.Services;
using Microsoft.VisualStudio.Shell;

namespace BuildNotifierPackagesForTeams.UI
{
    /// <summary>
    /// Interaction logic for ConfigurationDialog.xaml
    /// </summary>
    public partial class ConfigurationDialog : Window
    {
        private readonly TeamsNotificationService teamsService;

        public ConfigurationDialog(TeamsNotificationService teamsService)
        {
            InitializeComponent();
            this.teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
            LoadSettings();
        }

        private void LoadSettings()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                EnabledCheckBox.IsChecked = teamsService.IsEnabled();
                WebhookUrlTextBox.Text = teamsService.GetWebhookUrl();
                MentionUserTextBox.Text = teamsService.GetMentionUser();
            });
        }

        private void SaveSettings()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                teamsService.SetEnabled(EnabledCheckBox.IsChecked == true);
                teamsService.SetWebhookUrl(WebhookUrlTextBox.Text);
                teamsService.SetMentionUser(MentionUserTextBox.Text);
            });
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WebhookUrlTextBox.Text))
            {
                MessageBox.Show("Please enter Webhook URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TestButton.IsEnabled = false;
            TestButton.Content = "Sending...";

            try
            {
                // Temporarily save settings
                var originalEnabled = teamsService.IsEnabled();
                var originalUrl = teamsService.GetWebhookUrl();
                var originalMentionUser = teamsService.GetMentionUser();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                teamsService.SetEnabled(true);
                teamsService.SetWebhookUrl(WebhookUrlTextBox.Text);
                teamsService.SetMentionUser(MentionUserTextBox.Text);

                // Send test message
                var testResult = new BuildResult
                {
                    Success = true,
                    BuildTime = TimeSpan.FromSeconds(5.2),
                    Timestamp = DateTime.Now,
                    ProjectName = "Test Project"
                };

                await teamsService.SendBuildNotificationAsync(testResult);

                // Restore original settings
                teamsService.SetEnabled(originalEnabled);
                teamsService.SetWebhookUrl(originalUrl);
                teamsService.SetMentionUser(originalMentionUser);

                var mentionInfo = string.IsNullOrWhiteSpace(MentionUserTextBox.Text) 
                    ? "" 
                    : $" (mentioning {MentionUserTextBox.Text})";
                    
                MessageBox.Show($"Test message sent successfully{mentionInfo}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send test message:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestButton.IsEnabled = true;
                TestButton.Content = "Test";
            }
        }
    }
}