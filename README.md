# Batch Publish and Package .Net Projects

**Description:**
The "Batch Publish and Package .Net Projects" is a command-line tool designed to simplify the process of publishing and packaging .NET projects. This tool automates the publishing of project profiles and supports packaging the binaries into archive files. Additionally, it provides the option to use embedded profiles for common platforms, including Windows, macOS, Linux, ARM, and x86, as well as self-contained and framework-dependent publishing.

**Usage:**

- On Windows:
```shell
BatchPublishAndPackage.exe -p <Path> [-c] [-o <OutputDirectory>] [-d] [-e]
```

- On Unix (Linux or macOS):
```shell
./BatchPublishAndPackage -p <Path> [-c] [-o <OutputDirectory>] [-d] [-e]
```

**Options:**

- **-h, --help:** Displays the help screen.

- **-v, --version:** Displays the version of the application.

- **-p, --path <Path>:** (Required) Specifies the path to the project folder.

- **-c, --package:** Automatically packages the binaries into archive files.

- **-o, --output <OutputDirectory>:** Specifies the output directory for packaged files. If not provided, the current working directory is used.

- **-d, --debug:** Packages the pdb debug files along with the binaries.

- **-e, --embedded:** If specified, uses embedded profiles for common platforms (Windows, macOS, Linux, ARM, and x86). This includes self-contained and framework-dependent profiles.

**Examples:**

1. Display the help screen:
```shell
BatchPublishAndPackage.exe -h
```

2. Display the version of the application:
```shell
BatchPublishAndPackage.exe -v
```

3. Publish and package a .NET project using a specific path and output directory on Windows:
```shell
BatchPublishAndPackage.exe -p C:\path\to\project -c -o C:\output\directory
```

4. Publish and package a .NET project with embedded profiles for common platforms on Unix:
```shell
./BatchPublishAndPackage -p /path/to/project -c -e
```

5. Publish a .NET project with debug files and specify the output directory on Windows:
```shell
BatchPublishAndPackage.exe -p C:\path\to\project -d -o C:\output\directory
```

**Note:** Ensure that you have the .NET CLI installed and the `BatchPublishAndPackage.exe` (on Windows) or `BatchPublishAndPackage` (on Unix) available in your working directory or provide the full path to the executable for proper usage of the tool.

For more details and support, please refer to the application documentation or contact the development team.