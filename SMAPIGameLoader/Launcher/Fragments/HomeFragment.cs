#nullable enable
using _Microsoft.Android.Resource.Designer;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Button;
using SMAPIGameLoader.Tool;
using System;
using System.Text;
using Xamarin.Essentials;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace SMAPIGameLoader.Launcher.Fragments;

public class HomeFragment : Fragment
{
    private TextView? launcherInfoTextView;
    private TextView? smapiInstallInfoTextView;
    private MaterialButton? startGameBtn;
    private MaterialButton? selectApkBtn;
    private MaterialButton? rescanGameBtn;

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        return inflater.Inflate(ResourceConstant.Layout.FragmentHome, container, false);
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        launcherInfoTextView = view.FindViewById<TextView>(ResourceConstant.Id.launcherInfoTextView);
        smapiInstallInfoTextView = view.FindViewById<TextView>(ResourceConstant.Id.SMAPIInstallInfoTextView);
        startGameBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.StartGame);
        selectApkBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.SelectApkBtn);
        rescanGameBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.RescanGameBtn);

        if (startGameBtn != null)
            startGameBtn.Click += OnClickStartGame;

        if (selectApkBtn != null)
            selectApkBtn.Click += OnClickSelectApk;

        if (rescanGameBtn != null)
            rescanGameBtn.Click += OnClickRescanGame;

        if (!SMAPIInstaller.IsInstalled)
        {
            SMAPIInstaller.EnsureSMAPIInstalled();
        }

        SMAPIInstaller.OnInstalledSMAPI += RefreshInfo;

        RefreshInfo();
    }

    public override void OnDestroyView()
    {
        SMAPIInstaller.OnInstalledSMAPI -= RefreshInfo;
        base.OnDestroyView();
    }

    public void RefreshInfo()
    {
        if (Activity == null || !IsAdded)
            return;

        Activity.RunOnUiThread(() =>
        {
            UpdateLauncherInfoText();
            UpdateSMAPIInfoText();
        });
    }

    private void UpdateLauncherInfoText()
    {
        try
        {
            if (launcherInfoTextView == null)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("Launcher Version: " + AppInfo.VersionString);

            var buildDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(int.Parse(AppInfo.BuildString));
            var localDateTimeString = buildDateTimeOffset.ToLocalTime().ToString("HH:mm:ss dd/MM/yyyy");
            sb.AppendLine($"Build: {localDateTimeString} (d/m/y)");
            sb.AppendLine("Support: Stardew Valley 1.6+ (CH Play / Galaxy / Mod APK)");

            if (StardewApkTool.IsInstalled)
            {
                sb.AppendLine($"Trạng thái game: Đã phát hiện ({StardewApkTool.DetectionStatus})");
                sb.AppendLine($"Phiên bản: {StardewApkTool.CurrentGameVersion}");
                if (StardewApkTool.CurrentPackageInfo != null)
                {
                    sb.AppendLine("Package: " + StardewApkTool.CurrentPackageInfo.PackageName);
                }

                if (StardewApkTool.CanReadApk)
                {
                    sb.AppendLine("Đọc file APK: OK (Sẵn sàng khởi chạy)");
                }
                else
                {
                    sb.AppendLine("⚠️ Cảnh báo: Quyền đọc file APK bị chặn. Vui lòng bấm 'Chọn file APK' bên dưới để nạp file trực tiếp!");
                }
            }
            else
            {
                sb.AppendLine("⚠️ Trạng thái game: Chưa phát hiện Stardew Valley!");
                sb.AppendLine("• Hãy cài đặt game hoặc bấm 'Chọn file APK' để nạp file .apk.");
            }

            sb.AppendLine("Discord: Stardew SMAPI Thailand");
            sb.AppendLine("Developer: NRTnarathip, Eky-Team");

            launcherInfoTextView.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine("UpdateLauncherInfoText error: " + ex);
        }
    }

    private void UpdateSMAPIInfoText()
    {
        try
        {
            if (smapiInstallInfoTextView == null)
                return;

            if (SMAPIInstaller.IsInstalled)
            {
                var sb = new StringBuilder();
                sb.AppendLine("SMAPI Android: Đã cài đặt ✅");
                sb.AppendLine("Phiên bản SMAPI: " + SMAPIInstaller.GetCurrentVersion());
                sb.AppendLine("Build Code: " + SMAPIInstaller.GetBuildCode());
                smapiInstallInfoTextView.Text = sb.ToString();
            }
            else
            {
                smapiInstallInfoTextView.Text = "⚠️ Chưa cài đặt SMAPI. Vui lòng chuyển sang tab 'Công cụ' để cài đặt SMAPI từ file zip!";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("UpdateSMAPIInfoText error: " + ex);
        }
    }

    private async void OnClickSelectApk(object? sender, EventArgs e)
    {
        try
        {
            var pick = await FilePickerTool.PickApkFile("Chọn file Stardew Valley (.apk)");
            if (pick == null)
                return;

            ToastNotifyTool.Notify("Đang nạp file APK: " + pick.FileName);
            string? copiedPath = await FilePickerTool.CopyFileToInternalStorage(pick, "custom_stardew.apk");
            if (copiedPath != null && StardewApkTool.SetCustomApk(copiedPath))
            {
                ToastNotifyTool.Notify("Đã nhận diện file APK thành công!");
                RefreshInfo();
            }
            else
            {
                ToastNotifyTool.Notify("Không thể đọc thông tin từ file APK đã chọn.");
            }
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Lỗi chọn APK: " + ex.Message);
            ErrorDialogTool.Show(ex);
        }
    }

    private void OnClickRescanGame(object? sender, EventArgs e)
    {
        StardewApkTool.DetectGame();
        RefreshInfo();
        if (StardewApkTool.IsInstalled)
        {
            ToastNotifyTool.Notify("Đã tìm thấy game: " + StardewApkTool.DetectionStatus);
        }
        else
        {
            ToastNotifyTool.Notify("Vẫn chưa tìm thấy game. Vui lòng bấm 'Chọn file APK'!");
        }
    }

    private void OnClickStartGame(object? sender, EventArgs e)
    {
        if (Activity == null)
            return;

        if (!StardewApkTool.IsInstalled)
        {
            ToastNotifyTool.Notify("Chưa tìm thấy game Stardew Valley. Vui lòng cài đặt game hoặc bấm 'Chọn file APK'.");
            return;
        }

        if (!StardewApkTool.CanReadApk)
        {
            ToastNotifyTool.Notify("Không thể đọc trực tiếp APK của game. Vui lòng bấm 'Chọn file APK' để nạp file .apk!");
            return;
        }

        Console.WriteLine("On click start game from HomeFragment");
        EntryGame.LaunchGameActivity(Activity);
    }
}
