#nullable enable
using Android.App;
using Mono.Cecil;
using SMAPIGameLoader.Tool;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

internal static class SMAPIInstaller
{
    public const string BundledSMAPIAssetName = "smapi_android.zip";
    public const string GithubOwner = "NRTnarathip";
    public const string GithubRepoName = "SMAPI-Android-1.6";
    public const string StardewModdingAPIFileName = "StardewModdingAPI.dll";

    public static string GetInstallFilePath => Path.Combine(GameAssemblyManager.AssembliesDirPath, StardewModdingAPIFileName);
    public static bool IsInstalled => File.Exists(GetInstallFilePath);

    public static Action? OnInstalledSMAPI;

    public static long GetBuildCode()
    {
        try
        {
            if (!IsInstalled)
                return 0;

            using var stream = File.OpenRead(GetInstallFilePath);
            var assembly = AssemblyDefinition.ReadAssembly(stream);
            var SMAPIAndroidBuild = assembly.MainModule.Types.FirstOrDefault(t => t.FullName == "StardewModdingAPI.Mobile.SMAPIAndroidBuild");
            if (SMAPIAndroidBuild != null)
            {
                var field = SMAPIAndroidBuild.Fields.FirstOrDefault(p => p.Name == "BuildCode");
                if (field?.Constant is string buildString && long.TryParse(buildString, out long code))
                    return code;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetBuildCode error: " + ex);
            return 0;
        }
    }

    public static Version GetCurrentVersion()
    {
        try
        {
            if (!IsInstalled)
                return new Version(0, 0, 0, 0);

            using var stream = File.OpenRead(GetInstallFilePath);
            var assembly = AssemblyDefinition.ReadAssembly(stream);
            var constantsType = assembly.MainModule.Types.FirstOrDefault(t => t.FullName == "StardewModdingAPI.EarlyConstants");
            if (constantsType != null)
            {
                var rawApiVersionField = constantsType.Fields.FirstOrDefault(p => p.Name == "RawApiVersionForAndroid");
                if (rawApiVersionField?.Constant is string versionStr && Version.TryParse(versionStr, out var v))
                    return v;
            }
            return new Version(0, 0, 0, 0);
        }
        catch
        {
            return new Version(0, 0, 0, 0);
        }
    }

    public static bool EnsureSMAPIInstalled()
    {
        if (!IsInstalled)
        {
            Console.WriteLine("SMAPI is not installed. Unpacking bundled SMAPI from assets...");
            return InstallSMAPIFromAsset();
        }
        return true;
    }

    public static bool InstallSMAPIFromAsset()
    {
        try
        {
            Console.WriteLine("Opening bundled SMAPI asset: " + BundledSMAPIAssetName);
            using var stream = Application.Context.Assets?.Open(BundledSMAPIAssetName);
            if (stream == null)
            {
                Console.WriteLine("Could not open asset: " + BundledSMAPIAssetName);
                return false;
            }

            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            InstallSMAPIFromZipArchive(zip);
            OnInstalledSMAPI?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error installing SMAPI from asset: " + ex);
            return false;
        }
    }

    public static void InstallSMAPIFromZipArchive(ZipArchive zip)
    {
        var stardewDir = GameAssemblyManager.AssembliesDirPath;
        Directory.CreateDirectory(stardewDir);

        // Determine root prefix based on StardewModdingAPI.dll location
        var stardewEntry = zip.Entries.FirstOrDefault(e => string.Equals(e.Name, StardewModdingAPIFileName, StringComparison.OrdinalIgnoreCase));
        string prefix = "";
        if (stardewEntry != null)
        {
            int lastSlash = stardewEntry.FullName.LastIndexOfAny(new[] { '/', '\\' });
            if (lastSlash >= 0)
                prefix = stardewEntry.FullName.Substring(0, lastSlash + 1);
        }
        else
        {
            var firstEntry = zip.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.Name));
            if (firstEntry != null)
            {
                int firstSlash = firstEntry.FullName.IndexOfAny(new[] { '/', '\\' });
                if (firstSlash >= 0)
                    prefix = firstEntry.FullName.Substring(0, firstSlash + 1);
            }
        }

        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            string relativePath = entry.FullName;
            if (!string.IsNullOrEmpty(prefix) && relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(prefix.Length);
            }

            if (string.IsNullOrWhiteSpace(relativePath))
                continue;

            var destExtractFilePath = Path.Combine(stardewDir, relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            ZipFileTool.Extract(entry, destExtractFilePath);
        }

        FileTool.ClearCache();
    }

    static bool IsSMAPIZipFromPickFile(FileResult pick)
    {
        var fileName = pick.FileName;

        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return false;

        if (fileName.Contains("SMAPI", StringComparison.OrdinalIgnoreCase))
        {
            var fileInfo = new FileInfo(pick.FullPath);
            return FileTool.ConvertBytesToMB(fileInfo.Length) <= 30;
        }

        return false;
    }

    public static async void OnClickInstallSMAPIZip(object? sender, EventArgs? eventArgs)
    {
        try
        {
            var pick = await FilePickerTool.PickZipFile(title: "Chọn file SMAPI Android (.zip)");
            if (pick == null)
                return;

            if (!IsSMAPIZipFromPickFile(pick))
            {
                DialogTool.Show("Lỗi tệp SMAPI", "Vui lòng chọn file zip cài đặt SMAPI dành cho Android (ví dụ: SMAPI-4.x.x.zip).");
                return;
            }

            InstallSMAPIFromZipFile(pick.FullPath);

            ToastNotifyTool.Notify("Đã cài đặt SMAPI từ file zip thành công!");
            OnInstalledSMAPI?.Invoke();
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Lỗi cài đặt SMAPI: " + ex.Message);
            Console.WriteLine(ex);
        }
    }

    public static void InstallSMAPIFromZipFile(string smapiZipFilePath)
    {
        using var zip = ZipFile.OpenRead(smapiZipFilePath);
        InstallSMAPIFromZipArchive(zip);
    }
}
