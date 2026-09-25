#nullable enable
using Android;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class FilePickerTool
{
    public static FilePickerFileType FileTypeZip = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.Android, new[] { "application/zip" } },
    });

    public static FilePickerFileType FileTypeApk = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.Android, new[] { "application/vnd.android.package-archive", "application/octet-stream", "*/*" } },
    });

    public static async Task<FileResult?> PickZipFile(string? title = null)
    {
        if (title == null)
            title = "Please select zip file";

        var activity = SMAPIGameLoader.Launcher.LauncherActivity.Instance;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M && Build.VERSION.SdkInt <= BuildVersionCodes.Q)
        {
            if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.ReadExternalStorage) != Permission.Granted
                || ContextCompat.CheckSelfPermission(activity, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                ToastNotifyTool.Notify("Please Click Allow File Access Permission");
                ActivityCompat.RequestPermissions(activity,
                    new[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage },
                    1000);
                return null;
            }
        }

        var options = new PickOptions
        {
            PickerTitle = title,
            FileTypes = FileTypeZip,
        };
        return await FilePicker.PickAsync(options);
    }

    public static async Task<FileResult?> PickApkFile(string? title = null)
    {
        if (title == null)
            title = "Chọn file APK Stardew Valley";

        var activity = SMAPIGameLoader.Launcher.LauncherActivity.Instance;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M && Build.VERSION.SdkInt <= BuildVersionCodes.Q)
        {
            if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.ReadExternalStorage) != Permission.Granted)
            {
                ToastNotifyTool.Notify("Vui lòng cấp quyền đọc bộ nhớ");
                ActivityCompat.RequestPermissions(activity,
                    new[] { Manifest.Permission.ReadExternalStorage },
                    1001);
                return null;
            }
        }

        var options = new PickOptions
        {
            PickerTitle = title,
            FileTypes = FileTypeApk,
        };
        return await FilePicker.PickAsync(options);
    }

    public static async Task<string?> CopyFileToInternalStorage(FileResult pick, string targetFileName)
    {
        try
        {
            string targetPath = Path.Combine(FileTool.ExternalFilesDir, targetFileName);
            using var srcStream = await pick.OpenReadAsync();
            using var dstStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
            await srcStream.CopyToAsync(dstStream);
            return targetPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[FilePickerTool] Error copying file: " + ex);
            return pick.FullPath;
        }
    }
}
