using Newtonsoft.Json;
using System.Diagnostics;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly string currentVersion = "1.0.300";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Loading...");

        var versions = LoadVersionConfig();

        var versionCheckResult = await CheckVersionAsync();
        if (versionCheckResult.HasError || versionCheckResult.IsNewVersionAvailable)
        {
            Console.Clear();
            if (versionCheckResult.HasError)
            {
                Console.WriteLine("Error checking version. Check your internet connection or try again later");
            }
            else
            {
                Console.WriteLine("New version available! Download it here: https://xps.xytriza.com/download/windows");
            }
            Console.ReadLine();
            return;
        }

        Console.Clear();
        Console.WriteLine("Welcome to Xytriza's GDPS!\n");

        if (versions.Versions.Count == 0)
        {
            Console.WriteLine("No versions to load.");
            Console.ReadLine();
            return;
        }

        for (int i = 0; i < versions.Versions.Count; i++)
        {
            Console.WriteLine($"{i + 1}: Load {versions.Versions[i].name}");
        }
        Console.WriteLine();

        if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= versions.Versions.Count)
        {
            var selectedVersion = versions.Versions[choice - 1];
            string executionPath = GetExecutionPath();
            string path = Path.Combine(executionPath, selectedVersion.path);
            if (FileExists(path) && path.StartsWith(Path.Combine(executionPath, "gdps"), StringComparison.OrdinalIgnoreCase))
            {
                StartProcess(path);
            }
            else
            {
                Console.WriteLine("File does not exist.");
                Console.ReadLine();
            }
        }
        else
        {
            Console.WriteLine("Invalid choice.");
            Console.ReadLine();
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

    static bool FileExists(string path) => File.Exists(path);

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

            Console.Clear();
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.ReadLine();
        }
    }

    static VersionConfig LoadVersionConfig()
    {
        string filePath = Path.Combine(GetExecutionPath(), "versions.json");
        if (!File.Exists(filePath) || !TryReadConfig(filePath, out VersionConfig? config))
        {
            config = ResetConfig(filePath);
        }

        config.Versions = config.Versions.Where(v => FileExists(Path.Combine(GetExecutionPath(), v.path))).ToList();

        return config!;
    }

    static bool TryReadConfig(string filePath, out VersionConfig? config)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            config = JsonConvert.DeserializeObject<VersionConfig>(json);
            return config != null;
        }
        catch
        {
            config = null;
            return false;
        }
    }

    static VersionConfig ResetConfig(string filePath)
    {
        var config = new VersionConfig
        {
            Versions = new List<VersionInfo>
            {
                new VersionInfo { name = "GD 2.2", path = @"gdps/2.2/XPS.exe" },
                new VersionInfo { name = "GD 2.1", path = @"gdps/2.1/XPS.exe" },
                new VersionInfo { name = "GD 2.0", path = @"gdps/2.0/XPS.exe" },
                new VersionInfo { name = "GD 1.9", path = @"gdps/1.9/XPS.exe" }
            }
        };

        File.WriteAllText(filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        return config;
    }
}

public class VersionConfig
{
    public List<VersionInfo> Versions { get; set; } = new List<VersionInfo>();
}

public class VersionInfo
{
    public string? name { get; set; }
    public string? path { get; set; }
}
public class VersionCheckResult
{
    public bool IsNewVersionAvailable { get; set; }
    public bool HasError { get; set; }
}