using System.Diagnostics;
using System.IO.Compression;

internal class Program
{
    static string PrevDir(int backStep)
    {
        string currentDir = Directory.GetCurrentDirectory();
        for (int i = 0; i < backStep; i++)
            currentDir = Directory.GetParent(currentDir).FullName;
        return currentDir;
    }

    static string GetModOutputDirectory()
    {
        string modOutDir = Path.Combine(PrevDir(4),
            "LumaIsland_ThaiFontFix\\bin\\Release\\net46");
        return modOutDir;
    }

    static class BuildConfig
    {
        public static string MainDllFileName = "LumaIsland_ThaiFontFix.dll";
        public static string FolderName = "LumaIsland_ThaiFontFix";
        public static string GetVersion(string modOutputDir)
        {
            var dllPath = Path.Combine(modOutputDir, MainDllFileName);
            var versionInfo = FileVersionInfo.GetVersionInfo(dllPath);
            var v = new Version(versionInfo.FileVersion);
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }

        public readonly static string[] CopyFiles = {
            MainDllFileName,
            "RSU_Regular.ttf",
        };
    }


    private static void Main(string[] args)
    {
        Console.WriteLine("BuildReleaseTool running...");

        var modOutputDir = GetModOutputDirectory();
        var files = Directory.GetFiles(modOutputDir, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            Console.WriteLine("found file: " + file);
        }

        string releaseZipName = @"LumaIsland_ThaiFontFix-";
        var fileVersion = BuildConfig.GetVersion(modOutputDir);
        releaseZipName += fileVersion + ".zip";

        using (ZipArchive zip = ZipFile.Open(releaseZipName, ZipArchiveMode.Create))
        {
            foreach (var srcFileName in BuildConfig.CopyFiles)
            {
                var srcFilePath = Path.Combine(modOutputDir, srcFileName);
                var entryName = Path.Combine(BuildConfig.FolderName, srcFileName);
                zip.CreateEntryFromFile(srcFilePath, entryName);
            }
        }

        Console.WriteLine("Created zip file: " + releaseZipName);
        Console.WriteLine("BuildReleaseTool executed successfully.");
        Console.ReadKey();
    }
}