using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XPSLauncher
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string currentVersion = "2.0.0";

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.pictureBox1.SendToBack();
            checkVersion();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void load22(object sender, EventArgs e)
        {
            launchGDPS("2.2");
        }

        private void load21(object sender, EventArgs e)
        {
            launchGDPS("2.1");
        }

        private void load20(object sender, EventArgs e)
        {
            launchGDPS("2.0");
        }

        private void load19(object sender, EventArgs e)
        {
            launchGDPS("1.9");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void launchGDPS(string version)
        {
            string executionPath = GetExecutionPath();
            string path = "";

            switch (version)
            {
                case "2.2":
                    path = Path.Combine(executionPath, "gdps", "2.2", "XPS.exe");
                    break;
                case "2.1":
                    path = Path.Combine(executionPath, "gdps", "2.1", "XPS.exe");
                    break;
                case "2.0":
                    path = Path.Combine(executionPath, "gdps", "2.0", "XPS.exe");
                    break;
                case "1.9":
                    path = Path.Combine(executionPath, "gdps", "1.9", "XPS.exe");
                    break;
                default:
                    return;
            }

            if (File.Exists(path))
            {
                StartProcess(path);
            }
            else
            {
                MessageBox.Show($"Error loading version {version}. Create a support ticket in the Discord and we will try to help you.", $"Error loading {version}", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void discordButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/discord");
        }

        private void twitterButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/twitter");
        }

        private void youtubeButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/youtube");
        }

        private void twitchButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/twitch");
        }

        private async void checkVersion()
        {
            var versionCheckResult = await CheckVersionAsync();
            this.button1.UseWaitCursor = false;
            this.button2.UseWaitCursor = false;
            this.button3.UseWaitCursor = false;
            this.button4.UseWaitCursor = false;
            if (versionCheckResult.HasError)
            {
                MessageBox.Show("Error checking version. Check your internet connection or try again later", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (versionCheckResult.IsNewVersionAvailable)
            {
                MessageBox.Show("New version released! Click \"OK\" to download the update", "Update Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenUrl("https://xps.xytriza.com/download/windows");
            }
            else
            {
                this.button1.Cursor = Cursors.Hand;
                this.button2.Cursor = Cursors.Hand;
                this.button3.Cursor = Cursors.Hand;
                this.button4.Cursor = Cursors.Hand;
                this.button1.Click += new System.EventHandler(this.load22);
                this.button2.Click += new System.EventHandler(this.load21);
                this.button3.Click += new System.EventHandler(this.load20);
                this.button4.Click += new System.EventHandler(this.load19);
            }
        }

        public static async Task<VersionCheckResult> CheckVersionAsync()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync("https://xps.xytriza.com/getLatestWindowsVersion.php");
                if (response.IsSuccessStatusCode)
                {
                    string latestVersion = await response.Content.ReadAsStringAsync();
                    return new VersionCheckResult
                    {
                        IsNewVersionAvailable = IsNewerVersion(currentVersion, latestVersion.Trim()),
                        HasError = false
                    };
                }
                else
                {
                    return new VersionCheckResult { HasError = true };
                }
            }
            catch
            {
                return new VersionCheckResult { HasError = true };
            }
        }

        private static bool IsNewerVersion(string currentVersion, string latestVersion)
        {
            Version currentVer, latestVer;
            if (Version.TryParse(currentVersion, out currentVer) && Version.TryParse(latestVersion, out latestVer))
            {
                return latestVer > currentVer;
            }
            return false;
        }
        static string GetExecutionPath() => AppDomain.CurrentDomain.BaseDirectory;

        static void StartProcess(string path)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = Path.GetDirectoryName(path),
                    UseShellExecute = false
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching that GDPS version. Create a support ticket in the Discord and we will try to help you.\n\nError message: {ex.Message}", $"Error laucnhing GDPS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

public class VersionCheckResult
{
   public bool IsNewVersionAvailable { get; set; }
   public bool HasError { get; set; }
}