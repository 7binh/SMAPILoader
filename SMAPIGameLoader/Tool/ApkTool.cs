using Android.App;
using Android.Content.PM;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class ApkTool
{
    public static int LauncherBuildCode => int.Parse(AppInfo.BuildString);
    public static Version AppVersion => AppInfo.Version;
    public static string PackageName => AppInfo.PackageName;

    public static PackageInfo GetPackageInfo(string PackageName)
    {
        try
        {
            var ctx = Application.Context;
            if (ctx?.PackageManager == null)
                return null;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                try
                {
                    return ctx.PackageManager.GetPackageInfo(PackageName, PackageManager.PackageInfoFlags.Of(PackageInfoFlagsLong.None));
                }
                catch
                {
                    return ctx.PackageManager.GetPackageInfo(PackageName, 0);
                }
            }
            else
            {
                return ctx.PackageManager.GetPackageInfo(PackageName, 0);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ApkTool] GetPackageInfo('{PackageName}') not found or error: {e.Message}");
            return null;
        }
    }
    public static bool IsInstalled(string packageName)
        => GetPackageInfo(packageName) is not null;
}
