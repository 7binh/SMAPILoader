#nullable enable
using Android.App;
using Google.Android.Material.Dialog;
using System;

namespace SMAPIGameLoader.Tool;

internal static class DialogTool
{
    internal static void Show(
        string title,
        string msg,
        string buttonOKName = "Đồng ý",
        string? buttonCancelName = null,
        Action? onClickYes = null,
        Action? onClickCancel = null)
    {
        TaskTool.RunMainThread(() =>
        {
            var activity = ActivityTool.CurrentActivity;
            if (activity == null || activity.IsFinishing || activity.IsDestroyed)
                return;

            var builder = new MaterialAlertDialogBuilder(activity)
                .SetTitle(title)
                .SetMessage(msg)
                .SetPositiveButton(buttonOKName, (sender, e) =>
                {
                    onClickYes?.Invoke();
                });

            if (!string.IsNullOrEmpty(buttonCancelName))
            {
                builder.SetNegativeButton(buttonCancelName, (sender, e) =>
                {
                    onClickCancel?.Invoke();
                });
            }

            builder.Show();
        });
    }

    internal static void ConfirmDelete(
        string itemName,
        string details,
        Action onConfirmDelete,
        Action? onCancel = null)
    {
        TaskTool.RunMainThread(() =>
        {
            var activity = ActivityTool.CurrentActivity;
            if (activity == null || activity.IsFinishing || activity.IsDestroyed)
                return;

            var message = string.IsNullOrEmpty(details)
                ? $"Bạn có chắc chắn muốn xóa \"{itemName}\" không?\n\nHành động này không thể hoàn tác."
                : $"Bạn có chắc chắn muốn xóa \"{itemName}\" không?\n\n{details}\n\nHành động này không thể hoàn tác.";

            new MaterialAlertDialogBuilder(activity)
                .SetTitle("Xác nhận xóa mod")
                .SetMessage(message)
                .SetPositiveButton("Xóa mod", (s, e) => onConfirmDelete?.Invoke())
                .SetNegativeButton("Hủy", (s, e) => onCancel?.Invoke())
                .Show();
        });
    }
}
