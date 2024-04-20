﻿using System;
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
        private static readonly string currentVersion = "2.3.9";
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
        private Dictionary<string, bool> launcheQueue = new Dictionary<string, bool>()
        {
            { "2.2", false },
            { "2.1", false },
            { "2.0", false },
            { "1.9", false }
        };
        private static bool settingsOpen = false;
        private static bool settingCloseOnLoad = false;
        private static bool settingAllowMultipleInstances = false;

        public Form1()
        {
            try
            {
                if (!IsRunningAsAdministrator())
                {
                    MessageBox.Show($"XPS requires administrator due to how the installer works. Please re-run XPS with administrator to continue", $"Administrator Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }

                InitializeComponent();
                FormBorderStyle = FormBorderStyle.FixedSingle;
                MaximizeBox = false;
                pictureBox1.SendToBack();
                ConvertOnLoad();
                LoadFontFromFile();
                CheckVersion();
                CheckDownloaded();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading XPS. Error message: {ex.Message}", $"Error loading XPS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
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
            label7.Font = new Font(privateFonts.Families[0], 15.75F);
            label8.Font = new Font(privateFonts.Families[0], 15.75F);
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

        private void launchGDPS(string version, bool forceOpen = false)
        {
            if (!button1.Visible || !button2.Visible || !button3.Visible || !button4.Visible)
            {
                return;
            }

            string executionPath = GetExecutionPath();
            string path = "";

            if (ModifierKeys == Keys.Shift)
            {
                ResetGDPS(version);
                return;
            }
            
            if (launcheQueue[version])
            {
                return;
            }
            if (downloadingVersions[version])
            {
                DialogResult dialogResult = MessageBox.Show($"Version {version} is currently being downloaded. Would you like to add it to the launch queue?", $"Downloading {version}", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dialogResult == DialogResult.Yes && downloadingVersions[version])
                {
                    launcheQueue[version] = true;
                    return;
                }
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
                if (settingAllowMultipleInstances || !IsProcessOpen(path) || forceOpen)
                {
                    StartProcess(path);
                    if (settingCloseOnLoad && !downloadingVersions["2.2"] && !downloadingVersions["2.1"] && !downloadingVersions["2.0"] && !downloadingVersions["1.9"])
                    {
                        Environment.Exit(0);
                    }
                }
                else
                {
                    DialogResult result = MessageBox.Show($"XPS is already running. Would you like to force open XPS?\n\nContinuing can result in save data loss.", "XPS already open", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (result == DialogResult.Yes)
                    {
                        launchGDPS(version, true);
                    }
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

        private void DashboardButton(object sender, EventArgs e)
        {
            OpenUrl("https://dashboard.xps.xytriza.com");
        }

        private void SettingsButton(object sender, EventArgs e)
        {
            ToggleSettings();
        }

        private void ExitButton(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void RestartButton(object sender, EventArgs e)
        {
            Application.Restart();
            Environment.Exit(0);
        }

        private void ResetConfigButton(object sender, EventArgs e)
        {
            ResetConfig();
        }

        private void DarkThemeButton(object sender, EventArgs e)
        {
            WriteConfig("theme", 0);
            LoadTheme();
        }

        private void AmoledThemeButton(object sender, EventArgs e)
        {
            WriteConfig("theme", 1);
            LoadTheme();
        }

        private void PurpleThemeButton(object sender, EventArgs e)
        {
            WriteConfig("theme", 2);
            LoadTheme();
        }

        private void RedThemeButton(object sender, EventArgs e)
        {
            WriteConfig("theme", 3);
            LoadTheme();
        }

        private void OpenAppDataButton(object sender, EventArgs e)
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

        private void OpenAppFolderButton(object sender, EventArgs e)
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

        private async void CheckVersion()
        {
            var versionCheckResult = await CheckVersionAsync();
            button1.UseWaitCursor = false;
            button2.UseWaitCursor = false;
            button3.UseWaitCursor = false;
            button4.UseWaitCursor = false;
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
                button1.Cursor = Cursors.Hand;
                button2.Cursor = Cursors.Hand;
                button3.Cursor = Cursors.Hand;
                button4.Cursor = Cursors.Hand;
                button1.Click += new EventHandler(load22);
                button2.Click += new EventHandler(load21);
                button3.Click += new EventHandler(load20);
                button4.Click += new EventHandler(this.load19);
                KeyPreview = true;
                KeyPress += MainForm_KeyPress;
            }
        }

        private void CheckDownloaded()
        {
            string executionPath = GetExecutionPath();
            string[] versions = { "2.2", "2.1", "2.0", "1.9" };
            foreach (string version in versions)
            {
                string folder = Path.Combine(executionPath, "gdps", version);
                string path = Path.Combine(executionPath, "gdps", version, "XPS.exe");
                if (!Directory.Exists(folder) || !File.Exists(path))
                {
                    downloadingVersions[version] = true;
                    Directory.CreateDirectory(folder);
                    DownloadAndExtractFiles(version);
                }
            }
        }

        private async void DownloadAndExtractFiles(string version)
        {
            string executionPath = GetExecutionPath();
            string zipPath = Path.Combine(executionPath, $"pkg-{version}.zip");
            string extractPath = Path.Combine(executionPath, "gdps", version);
            string downloadUrl = $"https://xps.xytriza.com/assets/package-win-{version}.zip";

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
                        if (Directory.Exists(extractPath))
                        {
                            DirectoryInfo di = new DirectoryInfo(extractPath);
                            foreach (FileInfo file in di.GetFiles())
                            {
                                file.Delete();
                            }
                            foreach (DirectoryInfo dir in di.GetDirectories())
                            {
                                dir.Delete(true);
                            }
                        }

                        ZipFile.ExtractToDirectory(zipPath, extractPath);
                        if (File.Exists(zipPath))
                        {
                            File.Delete(zipPath);
                        }
                        downloadingVersions[version] = false;
                        errorVersions[version] = false;
                        if (launcheQueue[version])
                        {
                            launcheQueue[version] = false;
                            launchGDPS(version);
                        }
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
                button1.Visible = true;
                button2.Visible = true;
                button3.Visible = true;
                button4.Visible = true;
                label1.Visible = true;
                pictureBox7.Visible = true;
                pictureBox6.Visible = true;
                pictureBox5.Visible = true;
                pictureBox4.Visible = true;
                pictureBox3.Visible = true;
                pictureBox2.Visible = true;

                //disable stuff
                button5.Visible = false;
                button6.Visible = false;
                button7.Visible = false;
                button8.Visible = false;
                label2.Visible = false;
                panel1.Visible = false;
                label3.Visible = false;
                panel2.Visible = false;
                label4.Visible = false;
                panel3.Visible = false;
                label5.Visible = false;
                panel4.Visible = false;
                label6.Visible = false;
                label7.Visible = false;
                label8.Visible = false;
                pictureBox10.Visible = false;
                pictureBox11.Visible = false;
                pictureBox12.Visible = false;
                pictureBox13.Visible = false;
                button1.Focus();
                settingsOpen = false;
            }
            else
            {
                //disable stuff
                button1.Visible = false;
                button2.Visible = false;
                button3.Visible = false;
                button4.Visible = false;
                label1.Visible = false;
                pictureBox7.Visible = false;
                pictureBox6.Visible = false;
                pictureBox5.Visible = false;
                pictureBox4.Visible = false;
                pictureBox3.Visible = false;
                pictureBox2.Visible = false;

                //enable stuff
                button5.Visible = true;
                button6.Visible = true;
                button7.Visible = true;
                button8.Visible = true;
                label2.Visible = true;
                panel1.Visible = true;
                label3.Visible = true;
                panel2.Visible = true;
                label4.Visible = true;
                panel3.Visible = true;
                label5.Visible = true;
                panel4.Visible = true;
                label6.Visible = true;
                label7.Visible = true;
                label8.Visible = true;
                if (!settingCloseOnLoad)
                {
                    pictureBox11.Visible = true;
                }
                else
                {
                    pictureBox13.Visible = true;
                }
                if (!settingAllowMultipleInstances)
                {
                    pictureBox10.Visible = true;
                }
                else
                {
                    pictureBox12.Visible = true;
                }
                button5.Focus();
                settingsOpen = true;
            }
        }

        private void ResetConfig()
        {
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string xpsPath = Path.Combine(userPath, "Xytriza", "XPS");
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "xytriza", "xpslauncher.json");
            if (!Directory.Exists(xpsPath))
            {
                Directory.CreateDirectory(xpsPath);
            }
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
            if (settingsOpen && settingCloseOnLoad)
            {
                pictureBox13.Visible = false;
                pictureBox11.Visible = true;
            }
            if (settingsOpen && settingAllowMultipleInstances)
            {
                pictureBox12.Visible = false;
                pictureBox10.Visible = true;
            }
            settingAllowMultipleInstances = false;
            settingAllowMultipleInstances = false;
            SetThemeColor(Color.FromArgb(50, 50, 50));
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
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "xytriza", "xpslauncher.json");
            File.WriteAllText(configPath, jsonText);
        }

        private void RemoveConfigValue(string key)
        {
            dynamic json = ReadConfig();
            json.Remove(key);
            string jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented);
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "xytriza", "xpslauncher.json");
            File.WriteAllText(configPath, jsonText);
        }

        private dynamic ReadConfig()
        {
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "xytriza", "xpslauncher.json");
            string oldConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XPS");
            string oldConfigPath = Path.Combine(oldConfigFolder, "launcher.json");
            if (!File.Exists(configPath))
            {
                ResetConfig();
            }
            if (Directory.Exists(oldConfigFolder) && File.Exists(oldConfigPath))
            {
                string oldConfigText = File.ReadAllText(oldConfigPath);
                File.WriteAllText(configPath, oldConfigText);
                File.Delete(oldConfigPath);
            }
            string jsonText = File.ReadAllText(configPath);
            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonText);
            return json ?? new { };
        }

        public static bool IsProcessOpen(string filePath)
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(filePath));
            return processes.Any(p => p.MainModule.FileName.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        }

        private void ConvertOnLoad()
        {
            try
            {
                dynamic config = ReadConfig();

                if (config.lastVersion == "2.2.0")
                {
                    RemoveConfigValue("closeOnExit");
                    WriteConfig("closeOnLoad", config.closeOnExit ?? false);
                    WriteConfig("allowMultipleInstances", false);
                    WriteConfig("theme", 0);
                    config.allowMultipleInstances = false;
                    config.closeOnLoad = config.closeOnExit ?? false;
                }

                settingCloseOnLoad = config.closeOnLoad;
                settingAllowMultipleInstances = config.allowMultipleInstances;
                if (settingsOpen && settingCloseOnLoad)
                {
                    pictureBox13.Visible = false;
                    pictureBox11.Visible = true;
                }
                if (settingsOpen && settingAllowMultipleInstances)
                {
                    pictureBox12.Visible = false;
                    pictureBox10.Visible = true;
                }
                LoadTheme();
                WriteConfig("lastVersion", currentVersion);
            }
            catch
            {
                ResetConfig();
            }
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
                        SetThemeColor(Color.FromArgb(0, 0, 0));
                        break;
                    case 2:
                        SetThemeColor(Color.FromArgb(25, 0, 50));
                        break;
                    case 3:
                        SetThemeColor(Color.FromArgb(35, 0, 0));
                        break;
                    default:
                        SetThemeColor(Color.FromArgb(50, 50, 50));
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
            pictureBox8.BackColor = color;
            pictureBox7.BackColor = color;
            pictureBox6.BackColor = color;
            pictureBox5.BackColor = color;
            pictureBox4.BackColor = color;
            pictureBox3.BackColor = color;
            pictureBox2.BackColor = color;
            pictureBox1.BackColor = color;
        }

        private void ToggleCloseOnLoad(object sender = null, EventArgs e = null)
        {
            if (settingCloseOnLoad)
            {
                settingCloseOnLoad = false;
                pictureBox13.Visible = false;
                pictureBox11.Visible = true;
            }
            else
            {
                settingCloseOnLoad = true;
                pictureBox13.Visible = true;
                pictureBox11.Visible = false;
            }
            WriteConfig("closeOnLoad", settingCloseOnLoad);
        }

        private void ToggleMultiInstance(object sender = null, EventArgs e = null)
        {
            if (settingAllowMultipleInstances)
            {
                settingAllowMultipleInstances = false;
                pictureBox12.Visible = false;
                pictureBox10.Visible = true;
            }
            else
            {
                settingAllowMultipleInstances = true;
                pictureBox12.Visible = true;
                pictureBox10.Visible = false;
            }
            WriteConfig("allowMultipleInstances", settingAllowMultipleInstances);
        }

        public List<int> GetPIDsByApplicationPath(string applicationPath)
        {
            var pids = new List<int>();
            var query = $"Get-WmiObject Win32_Process | Where-Object {{$_.ExecutablePath -eq '{applicationPath}'}} | Select-Object ProcessId";
            var startInfo = new ProcessStartInfo("powershell", $"-Command \"{query}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (int.TryParse(line, out var pid))
                        {
                            pids.Add(pid);
                        }
                    }
                }
            }

            return pids;
        }
    }
}

public class VersionCheckResult
{
   public bool IsNewVersionAvailable { get; set; }
   public bool HasError { get; set; }
}