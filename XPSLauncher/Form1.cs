using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;
using System.Collections.Generic;

namespace XPSLauncher
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string currentVersion = "2.1.0";
        private PrivateFontCollection privateFonts = new PrivateFontCollection();
        private Dictionary<string, bool> downloadingVersions = new Dictionary<string, bool>()
        {
            { "2.2", false },
            { "2.1", false },
            { "2.0", false },
            { "1.9", false }
        };
        private Dictionary<string, bool> errorVersions = new Dictionary<string, bool>()
        {
            { "2.2", false },
            { "2.1", false },
            { "2.0", false },
            { "1.9", false }
        };

        public Form1()
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show($"XPS requires administrator due to how the installer works. Please re-run XPS with administrator to continue", $"Administrator Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.pictureBox1.SendToBack();
            LoadFontFromFile();
            CheckVersion();
            CheckDownloaded();
        }

        private void LoadFontFromFile()
        {
            string fontPath = "Pusab.ttf";
            privateFonts.AddFontFile(fontPath);
            button1.Font = new Font(privateFonts.Families[0], 17.5F);
            button2.Font = new Font(privateFonts.Families[0], 17.5F);
            button3.Font = new Font(privateFonts.Families[0], 17.5F);
            button4.Font = new Font(privateFonts.Families[0], 17.5F);
            label1.Font = new Font(privateFonts.Families[0], 15.75F);
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

            if (Control.ModifierKeys == Keys.Shift)
            {
                ResetGDPS(version);
                return;
            }

            if (downloadingVersions[version])
            {
                MessageBox.Show($"Version {version} is currently being downloaded. Please wait until the download is complete.", $"Downloading {version}", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (errorVersions[version])
            {
                MessageBox.Show($"Version {version} had an issue while downloading and cannot be launched. Please create a support ticket in our Discord server for assistance.", $"Error downloading {version}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
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

            if (File.Exists(path) && !downloadingVersions[version] && !errorVersions[version])
            {
                StartProcess(path);
            }
            else
            {
                MessageBox.Show($"Error loading version {version}. Create a support ticket in the Discord and we will try to help you.", $"Error loading {version}", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetGDPS(string version)
        {
            if (downloadingVersions[version])
            {
                return;
            }

            DialogResult dialogResult = MessageBox.Show($"Are you sure you would like to reset {version}? This will delete all game files and replace them with fresh ones. This will not effect save files.", $"Reset keybind pressed", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                downloadingVersions[version] = true;
                string executionPath = GetExecutionPath();
                string path = Path.Combine(executionPath, "gdps", version);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                DownloadAndExtractFiles(version);
            }
        }

        private void DiscordButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/discord");
        }

        private void TwitterButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/twitter");
        }

        private void YoutubeButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/youtube");
        }

        private void TwitchButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/twitch");
        }

        private void WebsiteButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com");
        }

        private void ToolsButton(object sender, EventArgs e)
        {
            OpenUrl("https://xps.xytriza.com/tools");
        }

        private async void CheckVersion()
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
                this.button1.Click += new EventHandler(this.load22);
                this.button2.Click += new EventHandler(this.load21);
                this.button3.Click += new EventHandler(this.load20);
                this.button4.Click += new EventHandler(this.load19);
                this.KeyPreview = true;
                this.KeyPress += MainForm_KeyPress;
            }
        }

        private void CheckDownloaded()
        {
            string executionPath = GetExecutionPath();
            string[] versions = { "2.2", "2.1", "2.0", "1.9" };
            foreach (string version in versions)
            {
                string path = Path.Combine(executionPath, "gdps", version);
                if (!Directory.Exists(path))
                {
                    downloadingVersions[version] = true;
                    Directory.CreateDirectory(path);
                    DownloadAndExtractFiles(version);
                }
            }
        }

        private async void DownloadAndExtractFiles(string version)
        {
            string executionPath = GetExecutionPath();
            string zipPath = Path.Combine(executionPath, $"pkg-{version}.zip");
            string extractPath = Path.Combine(executionPath, "gdps", version);
            string downloadUrl = $"https://xps.xytriza.com/download/windows/package-{version}.zip";

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        using (var fileStream = File.Create(zipPath))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                        downloadingVersions[version] = false;
                        MessageBox.Show($"Version {version} has been downloaded successfully!", $"Downloaded {version}", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        errorVersions[version] = true;
                        downloadingVersions[version] = false;
                        if (Directory.Exists(extractPath))
                        {
                            Directory.Delete(extractPath);
                        }
                        MessageBox.Show($"Error downloading files for version {version}. Create a support ticket in the Discord and we will try to help you.", $"Error downloading {version}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                errorVersions[version] = true;
                downloadingVersions[version] = false;
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath);
                }
                MessageBox.Show($"Error downloading files for version {version}. Create a support ticket in the Discord and we will try to help you.\n\nError message: {ex.Message}", $"Error downloading {version}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                File.Delete(zipPath);
            }
            catch (Exception ex)
            {
                errorVersions[version] = true;
                downloadingVersions[version] = false;
                Directory.Delete(extractPath, true);
                MessageBox.Show($"Error extracting files for version {version}. Create a support ticket in the Discord and we will try to help you.\n\nError message: {ex.Message}", $"Error extracting {version}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
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

        private static bool IsRunningAsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                //will fix this later
                case '1':
                    button1.Focus();
                    button1.PerformClick();
                    break;
                case '!':
                    button1.Focus();
                    ResetGDPS("2.2");
                    break;
                case '2':
                    button2.Focus();
                    button2.PerformClick();
                    break;
                case '@':
                    button2.Focus();
                    ResetGDPS("2.1");
                    break;
                case '3':
                    button3.Focus();
                    button3.PerformClick();
                    break;
                case '#':
                    button3.Focus();
                    ResetGDPS("2.0");
                    break;
                case '4':
                    button4.Focus();
                    button4.PerformClick();
                    break;
                case '$':
                    button4.Focus();
                    ResetGDPS("1.9");
                    break;
                default:
                    break;
            }
        }
    }
}

public class VersionCheckResult
{
   public bool IsNewVersionAvailable { get; set; }
   public bool HasError { get; set; }
}