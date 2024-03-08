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
using System.Linq;

namespace XPSLauncher
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string currentVersion = "2.3.0";
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
        private static bool settingsOpen = false;

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
            ConvertOnLoad();
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
            button5.Font = new Font(privateFonts.Families[0], 14.5F);
            button6.Font = new Font(privateFonts.Families[0], 12.5F);
            button7.Font = new Font(privateFonts.Families[0], 13.5F);
            button8.Font = new Font(privateFonts.Families[0], 10.5F);
            label1.Font = new Font(privateFonts.Families[0], 15.75F);
            checkBox1.Font = new Font(privateFonts.Families[0], 15.75F);
            checkBox2.Font = new Font(privateFonts.Families[0], 15.75F);
            label2.Font = new Font(privateFonts.Families[0], 20.0F);
            label3.Font = new Font(privateFonts.Families[0], 13.5F);
            label4.Font = new Font(privateFonts.Families[0], 13.5F);
            label5.Font = new Font(privateFonts.Families[0], 13.5F);
            label6.Font = new Font(privateFonts.Families[0], 13.5F);
            button7.Font = new Font(privateFonts.Families[0], 13.5F);
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
            if (!this.button1.Visible || !this.button2.Visible || !this.button3.Visible || !this.button4.Visible)
            {
                return;
            }

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
                dynamic config = ReadConfig();
                if ((bool)config.allowMultipleInstances || !IsProcessOpen(path))
                {
                    StartProcess(path);
                    if ((bool)config.closeOnLoad)
                    {
                        Environment.Exit(0);
                    }
                }
                else
                {
                    MessageBox.Show(Text = $"XPS {version} is already running. If you would like to open another instance, enable the \"Allow multiple instances\" setting in the settings menu.", $"Error loading version {version}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
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

        private void SettingsButton(object sender, EventArgs e)
        {
            ToggleSettings();
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
                        ZipFile.ExtractToDirectory(zipPath, extractPath);
                        if (File.Exists(zipPath))
                        {
                            File.Delete(zipPath);
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
                        if (File.Exists(zipPath))
                        {
                            File.Delete(zipPath);
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
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                MessageBox.Show($"Error downloading files for version {version}. Create a support ticket in the Discord and we will try to help you.\n\nError message: {ex.Message}", $"Error downloading {version}", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void ToggleSettings()
        {
            if (settingsOpen)
            {
                //enable stuff
                this.button1.Visible = true;
                this.button2.Visible = true;
                this.button3.Visible = true;
                this.button4.Visible = true;
                this.label1.Visible = true;
                this.pictureBox7.Visible = true;
                this.pictureBox6.Visible = true;
                this.pictureBox5.Visible = true;
                this.pictureBox4.Visible = true;
                this.pictureBox3.Visible = true;
                this.pictureBox2.Visible = true;

                //disable stuff
                this.checkBox1.Visible = false;
                this.checkBox2.Visible = false;
                this.button5.Visible = false;
                this.button6.Visible = false;
                this.button7.Visible = false;
                this.button8.Visible = false;
                this.label2.Visible = false;
                this.panel1.Visible = false;
                this.label3.Visible = false;
                this.panel2.Visible = false;
                this.label4.Visible = false;
                this.panel3.Visible = false;
                this.label5.Visible = false;
                this.panel4.Visible = false;
                this.label6.Visible = false;
                this.button1.Focus();
                settingsOpen = false;
            }
            else
            {
                //disable stuff
                this.button1.Visible = false;
                this.button2.Visible = false;
                this.button3.Visible = false;
                this.button4.Visible = false;
                this.label1.Visible = false;
                this.pictureBox7.Visible = false;
                this.pictureBox6.Visible = false;
                this.pictureBox5.Visible = false;
                this.pictureBox4.Visible = false;
                this.pictureBox3.Visible = false;
                this.pictureBox2.Visible = false;

                //enable stuff
                this.checkBox1.Visible = true;
                this.checkBox2.Visible = true;
                this.button5.Visible = true;
                this.button6.Visible = true;
                this.button7.Visible = true;
                this.button8.Visible = true;
                this.label2.Visible = true;
                this.panel1.Visible = true;
                this.label3.Visible = true;
                this.panel2.Visible = true;
                this.label4.Visible = true;
                this.panel3.Visible = true;
                this.label5.Visible = true;
                this.panel4.Visible = true;
                this.label6.Visible = true;
                this.button5.Focus();
                settingsOpen = true;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            WriteConfig("closeOnLoad", checkBox1.Checked);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            WriteConfig("allowMultipleInstances", checkBox2.Checked);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string executionPath = GetExecutionPath();
            try
            {
                Process.Start("explorer.exe", executionPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening the XPS folder. Create a support ticket in the Discord and we will try to help you.\n\nError message: {ex.Message}", $"Error opening XPS folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string xpsPath = Path.Combine(localAppDataPath, "XPS");
            if (!Directory.Exists(xpsPath))
            {
                Directory.CreateDirectory(xpsPath);
            }
            try
            {
                Process.Start("explorer.exe", xpsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening the XPS folder. Create a support ticket in the Discord and we will try to help you.\n\nError message: {ex.Message}", $"Error opening XPS folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Application.Restart();
            Environment.Exit(0);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ResetConfig();
        }

        private void ResetConfig()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string xpsPath = Path.Combine(localAppDataPath, "XPS");
            string configPath = Path.Combine(xpsPath, "launcher.json");
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            SetThemeColor(System.Drawing.Color.FromArgb(50, 50, 50));
            dynamic json = new
            {
                lastVersion = currentVersion,
                closeOnLoad = false,
                allowMultipleInstances = false,
                theme = 0
            };
            string jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(configPath, jsonText);
        }

        private void WriteConfig<T>(string key, T value)
        {
            dynamic json = ReadConfig();
            json[key] = value;
            string jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented);
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string xpsPath = Path.Combine(localAppDataPath, "XPS");
            string configPath = Path.Combine(xpsPath, "launcher.json");
            File.WriteAllText(configPath, jsonText);
        }

        private void RemoveConfigValue(string key)
        {
            dynamic json = ReadConfig();
            json.Remove(key);
            string jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented);
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string xpsPath = Path.Combine(localAppDataPath, "XPS");
            string configPath = Path.Combine(xpsPath, "launcher.json");
            File.WriteAllText(configPath, jsonText);
        }

        private dynamic ReadConfig()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string xpsPath = Path.Combine(localAppDataPath, "XPS");
            string configPath = Path.Combine(xpsPath, "launcher.json");
            if (!File.Exists(configPath))
            {
                ResetConfig();
            }
            string jsonText = File.ReadAllText(configPath);
            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonText);
            return json;
        }

        public static bool IsProcessOpen(string filePath)
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(filePath));
            return processes.Any(p => p.MainModule.FileName.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        }

        private void ConvertOnLoad()
        {
            dynamic config = ReadConfig();

            if (config.lastVersion == "2.2.0")
            {
                RemoveConfigValue("closeOnExit");
                WriteConfig("closeOnLoad", config.closeOnExit);
                WriteConfig("allowMultipleInstances", false);
                WriteConfig("theme", 0);
                config.allowMultipleInstances = false;
                config.closeOnLoad = config.closeOnExit;
            }

            checkBox1.Checked = config.closeOnLoad;
            checkBox2.Checked = config.allowMultipleInstances;
            LoadTheme();
            WriteConfig("lastVersion", currentVersion);
        }

        private void LoadTheme()
        {
            dynamic config = ReadConfig();
            int theme = config.theme;
            try
            {
                switch (theme)
                {
                    case 1:
                        SetThemeColor(System.Drawing.Color.FromArgb(0, 0, 0));
                        break;
                    case 2:
                        SetThemeColor(System.Drawing.Color.FromArgb(25, 0, 50));
                        break;
                    case 3:
                        SetThemeColor(System.Drawing.Color.FromArgb(0, 0, 50));
                        break;
                    default:
                        SetThemeColor(System.Drawing.Color.FromArgb(50, 50, 50));
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading theme. Error message: {ex.Message}", $"Error loading theme", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SetThemeColor(Color color)
        {
            this.BackColor = color;
            this.pictureBox8.BackColor = color;
            this.pictureBox7.BackColor = color;
            this.pictureBox6.BackColor = color;
            this.pictureBox5.BackColor = color;
            this.pictureBox4.BackColor = color;
            this.pictureBox3.BackColor = color;
            this.pictureBox2.BackColor = color;
            this.pictureBox1.BackColor = color;
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            WriteConfig("theme", 0);
            LoadTheme();
        }

        private void panel2_Click(object sender, EventArgs e)
        {
            WriteConfig("theme", 1);
            LoadTheme();
        }

        private void panel3_Click(object sender, EventArgs e)
        {
            WriteConfig("theme", 2);
            LoadTheme();
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            WriteConfig("theme", 3);
            LoadTheme();
        }
    }
}

public class VersionCheckResult
{
   public bool IsNewVersionAvailable { get; set; }
   public bool HasError { get; set; }
}