using BedrockManager;
using System.Diagnostics;
using System.IO;
using System.Text;

if (args.Length > 0)
{
    switch (args[0])
    {
        case "run":
            Run(args[1..]);
            break;
        case "upgrade":
            break;
        case "help":
            ShowHelp();
            break;
    }
}
else
{
    ShowHelp();
}

Config? LoadConfig(string configPath)
{
    if (!File.Exists(configPath))
    {
        return null;
    }
    var config = new Config();
    config.LoadFrom(configPath);
    config.SaveTo(configPath);
    return config;
}

DirectoryInfo? FindApp(Config config)
{
    var appBasePath = config.AppBasePath;
    var appDir = new DirectoryInfo(appBasePath);
    var versionToRun = config.VersionToRun;
    
    AppVersion? highestVersion = null;
    DirectoryInfo? dirToApp = null;
    
    foreach (var dir in appDir.EnumerateDirectories())
    {
        var name = dir.Name;
        var index = name.LastIndexOf('-');
        if (index > 0)
        {
            var versionSuffix = name.Substring(index+1);
            if (versionToRun == "current")
            {
                var ver = new AppVersion(versionSuffix);
                if (highestVersion == null || highestVersion.CompareTo(ver) < 0)
                {
                    highestVersion = ver;
                    dirToApp = dir;
                }
            }
            else
            {
                if (versionSuffix == versionToRun)
                {
                    dirToApp = dir;
                    break;
                }
            }
        }
    }
    return dirToApp;
}

void ConfigServer(Config config, FileInfo serverConfigFile)
{
    var sb = new StringBuilder();
    {
        using var sr = new StreamReader(serverConfigFile.OpenRead());
        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (line == null)
            {
                break;
            }
            if (line.StartsWith("difficulty="))
            {
                sb.AppendLine($"gamemode={config.Difficulty}");
            }
            else if (line.StartsWith("gamemode="))
            {
                sb.AppendLine($"gamemode={config.GameMode}");
            }
            else
            {
                sb.AppendLine(line);
            }
        }
    }
    using var sw = new StreamWriter(serverConfigFile.OpenWrite());
    sw.WriteLine(sb.ToString());
}

void RunServer(FileInfo appFile)
{
    var psi = new ProcessStartInfo(appFile.FullName)
    {
        UseShellExecute = false
    };
    var p = Process.Start(psi);
    p.WaitForExit();

    // TODO: archive world

    Console.WriteLine("Done.");
}

void Run(string[] args)
{
    var configPath = args.Length > 0? args[0] : "BedrockManager.cfg";
    var config = LoadConfig(configPath);
    if (config == null)
    {
        Console.WriteLine("Error loading config file.");
        return;
    }
    var appDir = FindApp(config);
    if (appDir == null)
    {
        Console.WriteLine("Server app folder not found");
    }
    var appConfigFile = appDir!.EnumerateFiles("server.properties").FirstOrDefault();
    if (appConfigFile == null)
    {
        Console.WriteLine("server.properties not found in the app folder");
        return;
    }
    ConfigServer(config, appConfigFile);

    var appFile = appDir!.EnumerateFiles("bedrock_server.exe").FirstOrDefault();
    if (appFile == null)
    {
        Console.WriteLine("bedrock_server.exe not found in the app folder");
        return;
    }

    RunServer(appFile);
}

void ShowHelp()
{
    Console.WriteLine("run [<path_to_config>]");
    Console.WriteLine("  Run an bedrock server instance according to the config. BedrockManager.cfg in current directory is used if the config path is not specified.");
}

namespace BedrockManager
{
    class Config
    {
        public string AppBasePath { get; set; }
        public string WorldArchivePath { get; set; }

        public string VersionToRun { get; set; } = "latest";
        public bool ArchiveWorldOnQuit { get; set; } = true;

        public string GameMode { get; set; } // survival,creative,adventure
        public string Difficulty { get; set; } // peaceful,easy,normal,hard
        public string WorldName { get; set; }

        public void LoadFrom(string configPath)
        {
            using var sr = new StreamReader(configPath);
            AppBasePath = sr.ReadLine();
            WorldArchivePath = sr.ReadLine();
            VersionToRun = sr.ReadLine();
            var archiveWorldOnQuit = int.Parse(sr.ReadLine());
            ArchiveWorldOnQuit = archiveWorldOnQuit != 0;
            GameMode = sr.ReadLine();
            Difficulty = sr.ReadLine();
        }

        public void SaveTo(string configPath)
        {
            using var sw = new StreamWriter(configPath);
            sw.WriteLine(AppBasePath);
            sw.WriteLine(WorldArchivePath);
            sw.WriteLine(VersionToRun);
            sw.WriteLine(ArchiveWorldOnQuit? "1" : "0");
            sw.WriteLine(GameMode);
            sw.WriteLine(Difficulty);
        }
    }

    class AppVersion : IComparable<AppVersion>
    {
        public AppVersion(string str)
        {
            var split = str.Split('.');
            Components = split.Select(x=>int.Parse(x)).ToArray();
        }

        public int[] Components { get; private set; }

        public int CompareTo(AppVersion that)
        {
            for (var i = 0; i < Components.Length && i < that.Components.Length; ++i)
            {
                var thisVal = Components[i];
                var thatVal = that.Components[i];
                var c = thisVal.CompareTo(thatVal);
                if (c != 0) return c;
            }
            return Components.Length.CompareTo(that.Components.Length);
        }
    }
}
