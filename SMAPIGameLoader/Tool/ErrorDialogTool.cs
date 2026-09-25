#nullable enable
using Android.App;
using Google.Android.Material.Dialog;
using SMAPIGameLoader.Tool;
using System;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class ErrorDialogTool
{
    public static void Show(Exception? exception, string title = "Đã xảy ra lỗi")
    {
        if (exception == null)
            return;

        Console.WriteLine("try show error dialog: " + exception);

        TaskTool.RunMainThread(() =>
        {
            var activity = ActivityTool.CurrentActivity;
            if (activity == null || activity.IsFinishing || activity.IsDestroyed)
                return;

            string errorDetails = exception.ToString();
            Clipboard.SetTextAsync(errorDetails);

            new MaterialAlertDialogBuilder(activity)
                .SetTitle(title)
                .SetMessage($"{exception.Message}\n\n(Chi tiết lỗi đã được tự động lưu vào bộ nhớ tạm clipboard)")
                .SetPositiveButton("Đóng", (s, e) => { })
                .SetNeutralButton("Sao chép lại", (s, e) =>
                {
                    Clipboard.SetTextAsync(errorDetails);
                    ToastNotifyTool.Notify("Đã sao chép chi tiết lỗi vào clipboard");
                })
                .Show();
        });
    }
}
