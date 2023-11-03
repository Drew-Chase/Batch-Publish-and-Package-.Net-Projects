// LFInteractive LLC. 2021-2024
using cclip;
using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace Batch_Publish_and_Package_.Net_Projects;

internal class Program
{
    public static string[] GetPubXML(string path) => Directory.GetFiles(path, "*.pubxml", SearchOption.AllDirectories);

    public static string[] GetEmbeddedPubXML(string working_directory)
    {
        List<string> files = new();

        string[] names = Assembly.GetExecutingAssembly()?.GetManifestResourceNames() ?? Array.Empty<string>();
        string[] publishDirectories = GetPublishProfilesDirectories(working_directory);
        foreach (string publishDirectory in publishDirectories)
        {
            foreach (string name in names)
            {
                if (name.EndsWith(".pubxml"))
                {
                    using Stream stream = Assembly.GetExecutingAssembly()?.GetManifestResourceStream(name) ?? Stream.Null;
                    using StreamReader reader = new(stream);
                    string content = reader.ReadToEnd();
                    string filename = Path.GetFileNameWithoutExtension(name).Split('.').Last() + ".pubxml";
                    string path = Path.Combine(publishDirectory, filename);
                    File.WriteAllText(path, content);
                    files.Add(path);
                }
            }
        }
        return files.ToArray();
    }

    public static string[] GetPublishProfilesDirectories(string working_dir)
    {
        List<string> files = new();
        string[] csproj = Directory.GetFiles(working_dir, "*.csproj", SearchOption.AllDirectories);
        foreach (string proj in csproj)
        {
            string directory = Directory.GetParent(proj)?.FullName ?? "";
            if (!string.IsNullOrWhiteSpace(directory))
            {
                files.Add(Directory.CreateDirectory(Path.Combine(directory, "Properties", "PublishProfiles")).FullName);
            }
        }

        return files.ToArray();
    }

    public static bool IsDotnetInstalled()
    {
        string? path = null;
        Process process = new()
        {
            StartInfo = new()
            {
                FileName = OperatingSystem.IsWindows() ? "where" : "which",
                Arguments = "dotnet",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true
        };
        process.OutputDataReceived += (s, e) =>
        {
            string? data = e.Data;
            try
            {
                if (!string.IsNullOrEmpty(data) && File.Exists(data))
                {
                    path = data;
                }
            }
            catch
            {
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        return path != null;
    }

    private static void Main(string[] args)
    {
        Process? currentProcess = null;
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            Console.ResetColor();
        };
        Console.CancelKeyPress += (s, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Canceling....");
            currentProcess?.Kill();
            Console.ResetColor();
        };

        if (!IsDotnetInstalled())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"The dotnet CLI must be installed, download it here: https://dotnet.microsoft.com/en-us/download");
            Console.ResetColor();
        }

        OptionsManager manager = new("Batch Publish and Package .Net Projects");
        manager.Add(new("v", "version", false, false, "Displays the version of the application"));
        manager.Add(new("p", "path", false, true, "Path to the project folder"));
        manager.Add(new("c", "package", false, false, "Automatically packages the binaries into archive files"));
        manager.Add(new("o", "output", false, true, "The output directory"));
        manager.Add(new("d", "debug", false, false, "Packages the pdb debug files"));
        manager.Add(new("l", "log", false, false, "Creates a log file for each profile."));
        manager.Add(new("e", "embedded", false, false, "If it should use the embedded profiles for windows, mac and linux, ARM and x86, self-contained and framework dependent"));

        OptionsParser parser = manager.Parse(args);
        if (parser.IsPresent("v"))
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new(0, 0, 0);
            Console.Write("Batch Publish and Package .Net Projects");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" v{version}");
            Console.ResetColor();
            return;
        }

        // Get Path
        parser.IsPresent("p", out string path);
        path ??= Environment.CurrentDirectory;
        path = Path.GetFullPath(path).Trim('"');

        // Get output
        parser.IsPresent("o", out string output);
        output ??= Environment.CurrentDirectory;
        output = Path.GetFullPath(output).Trim('"');
        Directory.CreateDirectory(output);

        bool packageDebug = parser.IsPresent("d");
        bool useEmbedded = parser.IsPresent("e");
        bool compress = parser.IsPresent("c");
        bool log = parser.IsPresent("l");

        string[] pubfiles = useEmbedded ? GetEmbeddedPubXML(path) : GetPubXML(path);
        if (pubfiles.Any())
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Processing {pubfiles.Length} profiles.");
            string builddir = compress ? Path.Combine(output, "tmp", Guid.NewGuid().ToString("N")) : output;
            Directory.CreateDirectory(builddir);
            foreach (string file in pubfiles)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Publishing {Path.GetFileNameWithoutExtension(file)}");
                string profileName = Path.GetFileNameWithoutExtension(file);
                string profileBuildDir = Path.Combine(builddir, Path.GetFileNameWithoutExtension(file));
                currentProcess = new()
                {
                    EnableRaisingEvents = true,
                    StartInfo = new()
                    {
                        FileName = "dotnet",
                        Arguments = $"publish -c Release /p:PublishProfile=\"{profileName}\" -o \"{profileBuildDir}\"",
                        UseShellExecute = false,
                        WorkingDirectory = path,
                        RedirectStandardOutput = true
                    }
                };
                if (log)
                {
                    currentProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            File.AppendAllText(Path.Combine(output, profileName + ".log"), e.Data + Environment.NewLine);
                        }
                    };
                }
                currentProcess.Start();

                currentProcess.BeginOutputReadLine();
                currentProcess.WaitForExit();
                if (currentProcess.ExitCode == 0)
                {
                    if (compress)
                    {
                        string archiveFile = Path.Combine(output, Path.GetFileNameWithoutExtension(file) + ".zip");
                        if (File.Exists(archiveFile))
                        {
                            File.Delete(archiveFile);
                        }
                        using (ZipArchive archive = ZipFile.Open(archiveFile, ZipArchiveMode.Create))
                        {
                            foreach (string item in Directory.GetFiles(profileBuildDir))
                            {
                                if (packageDebug || Path.GetExtension(item) != ".pdb")
                                    archive.CreateEntryFromFile(item, Path.GetFileName(item));
                            }
                        }
                        Directory.Delete(profileBuildDir, true);
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Failed to publish {Path.GetFileNameWithoutExtension(file)}");
                    Console.ResetColor();
                }
            }
            if (compress)
            {
                Directory.Delete(Path.Combine(output, "tmp"), true);
            }
            if (useEmbedded)
            {
                foreach (string profile in pubfiles)
                {
                    File.Delete(profile);
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write("No publish profiles found in ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.Write($"\"{path}\"");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write(" or any subdirectories");
            Console.ResetColor();
        }
    }
}