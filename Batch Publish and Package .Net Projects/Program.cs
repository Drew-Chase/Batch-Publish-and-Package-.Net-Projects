using cclip;
using System.Diagnostics;
using System.IO.Compression;

namespace Batch_Publish_and_Package_.Net_Projects;

internal class Program
{
    static void Main(string[] args)
    {
        OptionsManager manager = new("Batch Publish and Package .Net Projects");
        manager.Add(new("v", "version", false, false, "Displays the version of the application"));
        manager.Add(new("p", "path", false, true, "Path to the project folder"));
        manager.Add(new("c", "package", false, false, "Automatically packages the binaries into archive files"));
        manager.Add(new("o", "output", false, true, "The output directory"));
        manager.Add(new("d", "package-debug", false, false, "Packages the pdb debug files"));
        OptionsParser parser = manager.Parse(args);
        if (parser.IsPresent("v"))
        {
            Console.WriteLine("Batch Publish and Package .Net Projects v0.0.1");
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


        bool compress = parser.IsPresent("c");

        string[] pubfiles = GetPubXML(path);
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
                Process process = new()
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
                process.Start();
                process.WaitForExit();
                if (process.ExitCode == 0)
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

    public static string[] GetPubXML(string path) => Directory.GetFiles(path, "*.pubxml", SearchOption.AllDirectories);
}