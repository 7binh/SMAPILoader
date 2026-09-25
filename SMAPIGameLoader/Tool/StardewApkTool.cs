#nullable enable
using Android.App;
using Android.Content.PM;
using Android.OS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class StardewApkTool
{
    public const string GamePlayStorePackageName = "com.chucklefish.stardewvalley";
    public const string GameGalaxyStorePackageName = "com.chucklefish.stardewvalleysamsung";

    private const string PrefKeyCustomApkPath = "custom_stardew_apk_path";

    public static string? CustomApkPath { get; private set; }
    public static string DetectionStatus { get; private set; } = "Chưa phát hiện game";
    public static bool IsGameFromPlayStore { get; private set; }
    public static bool IsGameFromGalaxyStore { get; private set; }

    private static PackageInfo? _currentPackageInfo;
    public static PackageInfo? CurrentPackageInfo => _currentPackageInfo;

    static StardewApkTool()
    {
        DetectGame();
    }

    public static bool DetectGame()
    {
        Console.WriteLine("[StardewApkTool] Running DetectGame...");
        _currentPackageInfo = null;
        IsGameFromPlayStore = false;
        IsGameFromGalaxyStore = false;

        // 1. Check custom APK path saved in preferences
        try
        {
            string savedApk = Preferences.Get(PrefKeyCustomApkPath, string.Empty);
            if (!string.IsNullOrEmpty(savedApk) && File.Exists(savedApk))
            {
                if (SetCustomApk(savedApk))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[StardewApkTool] Error reading saved APK preference: " + ex.Message);
        }

        CustomApkPath = null;

        // 2. Check Galaxy Store version
        var samsung = ApkTool.GetPackageInfo(GameGalaxyStorePackageName);
        if (samsung != null)
        {
            _currentPackageInfo = samsung;
            IsGameFromGalaxyStore = true;
            DetectionStatus = $"Galaxy Store ({samsung.VersionName})";
            Console.WriteLine("[StardewApkTool] " + DetectionStatus);
            return true;
        }

        // 3. Check Google Play Store / default package name
        var playStore = ApkTool.GetPackageInfo(GamePlayStorePackageName);
        if (playStore != null)
        {
            _currentPackageInfo = playStore;
            IsGameFromPlayStore = true;
            DetectionStatus = $"Google Play / Mod ({playStore.VersionName})";
            Console.WriteLine("[StardewApkTool] " + DetectionStatus);
            return true;
        }

        // 4. Scan all installed packages on the device
        try
        {
            var pm = Application.Context?.PackageManager;
            if (pm != null)
            {
                IList<PackageInfo>? installed = null;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
                    try { installed = pm.GetInstalledPackages(PackageManager.PackageInfoFlags.Of(PackageInfoFlagsLong.None)); } catch { }
                }
                if (installed == null)
                {
                    try { installed = pm.GetInstalledPackages(0); } catch { }
                }

                if (installed != null)
                {
                    var ownPackage = Application.Context?.PackageName;

                    // 4a. Check package names containing 'stardew'
                    var mod = installed.FirstOrDefault(p =>
                        !string.IsNullOrEmpty(p.PackageName) &&
                        !p.PackageName.Equals(ownPackage, StringComparison.OrdinalIgnoreCase) &&
                        p.PackageName.Contains("stardew", StringComparison.OrdinalIgnoreCase));

                    // 4b. Check application labels containing 'stardew'
                    if (mod == null)
                    {
                        mod = installed.FirstOrDefault(p =>
                        {
                            if (string.IsNullOrEmpty(p.PackageName) || p.PackageName.Equals(ownPackage, StringComparison.OrdinalIgnoreCase))
                                return false;
                            try
                            {
                                var label = pm.GetApplicationLabel(p.ApplicationInfo);
                                return !string.IsNullOrEmpty(label) && label.Contains("stardew", StringComparison.OrdinalIgnoreCase);
                            }
                            catch { return false; }
                        });
                    }

                    if (mod != null)
                    {
                        _currentPackageInfo = mod;
                        DetectionStatus = $"Bản Mod ({mod.PackageName} - {mod.VersionName})";
                        Console.WriteLine("[StardewApkTool] " + DetectionStatus);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[StardewApkTool] Error scanning installed packages: " + ex.Message);
        }

        DetectionStatus = "Chưa phát hiện Stardew Valley trên máy";
        Console.WriteLine("[StardewApkTool] " + DetectionStatus);
        return false;
    }

    public static bool SetCustomApk(string apkPath)
    {
        try
        {
            if (string.IsNullOrEmpty(apkPath) || !File.Exists(apkPath))
                return false;

            var pm = Application.Context?.PackageManager;
            var archiveInfo = pm?.GetPackageArchiveInfo(apkPath, 0);

            _currentPackageInfo = archiveInfo ?? new PackageInfo
            {
                PackageName = "com.custom.stardewvalley",
                VersionName = "1.6.0.0"
            };

            CustomApkPath = apkPath;
            Preferences.Set(PrefKeyCustomApkPath, apkPath);
            DetectionStatus = $"APK: {Path.GetFileName(apkPath)} (v{CurrentGameVersion})";
            Console.WriteLine("[StardewApkTool] " + DetectionStatus);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[StardewApkTool] SetCustomApk error: " + ex);
            return false;
        }
    }

    public static void ClearCustomApk()
    {
        CustomApkPath = null;
        try { Preferences.Remove(PrefKeyCustomApkPath); } catch { }
        DetectGame();
    }

    public static bool HasSplitApks =>
        string.IsNullOrEmpty(CustomApkPath) &&
        CurrentPackageInfo?.ApplicationInfo?.SplitSourceDirs != null &&
        CurrentPackageInfo.ApplicationInfo.SplitSourceDirs.Count > 0;

    public static bool IsInstalled =>
        CurrentPackageInfo != null || (!string.IsNullOrEmpty(CustomApkPath) && File.Exists(CustomApkPath));

    public static string? BaseApkPath
    {
        get
        {
            if (!string.IsNullOrEmpty(CustomApkPath) && File.Exists(CustomApkPath))
                return CustomApkPath;

            var pub = CurrentPackageInfo?.ApplicationInfo?.PublicSourceDir;
            if (!string.IsNullOrEmpty(pub) && File.Exists(pub))
                return pub;

            var src = CurrentPackageInfo?.ApplicationInfo?.SourceDir;
            if (!string.IsNullOrEmpty(src) && File.Exists(src))
                return src;

            return pub ?? src;
        }
    }

    public static bool CanReadApk
    {
        get
        {
            try
            {
                var path = BaseApkPath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return false;

                using var fs = File.OpenRead(path);
                return fs.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public static string? Arm64ApkPath
    {
        get
        {
            try
            {
                if (!string.IsNullOrEmpty(CustomApkPath))
                    return CustomApkPath;

                if (CurrentPackageInfo == null)
                    return null;

                if (HasSplitApks)
                {
                    var arm64 = CurrentPackageInfo.ApplicationInfo?.SplitSourceDirs?
                        .FirstOrDefault(path => !string.IsNullOrEmpty(path) && (path.Contains("split_config.arm64") || path.Contains("arm64")));
                    if (!string.IsNullOrEmpty(arm64) && File.Exists(arm64))
                        return arm64;
                }

                return BaseApkPath;
            }
            catch (Exception ex)
            {
                ErrorDialogTool.Show(ex, "Error try to get Arm64ApkPath");
                return BaseApkPath;
            }
        }
    }

    public static string? ContentApkPath
    {
        get
        {
            try
            {
                if (!string.IsNullOrEmpty(CustomApkPath))
                    return CustomApkPath;

                if (CurrentPackageInfo == null)
                    return null;

                if (HasSplitApks)
                {
                    var content = CurrentPackageInfo.ApplicationInfo?.SplitSourceDirs?
                        .FirstOrDefault(path => !string.IsNullOrEmpty(path) && (path.Contains("split_content") || path.Contains("content")));
                    if (!string.IsNullOrEmpty(content) && File.Exists(content))
                        return content;
                }

                return BaseApkPath;
            }
            catch (Exception ex)
            {
                ErrorDialogTool.Show(ex, "Error try to get ContentApkPath");
                return BaseApkPath;
            }
        }
    }

    public static Version GameVersionSupport => new(1, 6, 0, 0);

    public static Version CurrentGameVersion
    {
        get
        {
            try
            {
                if (CurrentPackageInfo?.VersionName == null)
                    return new Version(1, 6, 0, 0);

                string rawVer = CurrentPackageInfo.VersionName.Trim();
                string cleanVer = rawVer.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
                return Version.TryParse(cleanVer, out var ver) ? ver : new Version(1, 6, 0, 0);
            }
            catch (Exception)
            {
                return new Version(1, 6, 0, 0);
            }
        }
    }

    public static bool IsGameVersionSupport =>
        !string.IsNullOrEmpty(CustomApkPath) ||
        (CurrentGameVersion.Major >= 1 && CurrentGameVersion.Minor >= 6) ||
        CurrentGameVersion >= GameVersionSupport;
}
