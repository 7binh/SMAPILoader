#nullable enable
using System;
using System.Text;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using SMAPIGameLoader.Tool;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

[Activity(
    Label = "SMAPI Launcher",
    MainLauncher = true,
    Theme = "@style/AppTheme",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.FullSensor
)]
public class LauncherActivity : AppCompatActivity
{
    public static LauncherActivity Instance { get; private set; }

    private static bool IsDeviceSupport => IntPtr.Size == 8;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Instance = this;
        base.OnCreate(savedInstanceState);

        SetContentView(ResourceConstant.Layout.LauncherLayout);

        Platform.Init(this, savedInstanceState);
        ActivityTool.Init(this);

        // Assert device architecture
        AssertRequirement();

        // Setup layout and event bindings
        OnReadyToSetupLayoutPage();
        SetDarkMode();

        // Run utils scripts
        ProcessAdbExtras();
    }

    private void SetDarkMode()
    {
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
    }

    /// <summary>
    ///     Receive argument launch activity
    /// </summary>
    private void ProcessAdbExtras()
    {
        if (AdbExtraTool.IsClickStartGame(this))
        {
            OnClickStartGame();
        }
    }

    private void AssertRequirement()
    {
        // Check if 32bit device
        if (IsDeviceSupport is false)
        {
            ToastNotifyTool.Notify("Không hỗ trợ thiết bị 32-bit (chỉ hỗ trợ 64-bit)");
            Finish();
            return;
        }

        // We do not call Finish() here if game is not detected.
        // The user will be guided via the UI to rescan or select an APK manually.
    }

    private void OnReadyToSetupLayoutPage()
    {
        // Setup event bindings
        try
        {
            FindViewById<Button>(ResourceConstant.Id.InstallSMAPIZip).Click += SMAPIInstaller.OnClickInstallSMAPIZip;
            FindViewById<Button>(ResourceConstant.Id.UploadLog).Click += LogParser.OnClickUploadLog;

            var startGameBtn = FindViewById<Button>(ResourceConstant.Id.StartGame);
            startGameBtn.Click += (sender, e) => { OnClickStartGame(); };

            var modManagerBtn = FindViewById<Button>(ResourceConstant.Id.ModManagerBtn);
            modManagerBtn.Click += (sender, e) => { ActivityTool.SwapActivity<ModManagerActivity>(this, false); };

            var selectApkBtn = FindViewById<Button>(ResourceConstant.Id.SelectApkBtn);
            if (selectApkBtn != null)
            {
                selectApkBtn.Click += OnClickSelectApk;
            }

            var rescanBtn = FindViewById<Button>(ResourceConstant.Id.RescanGameBtn);
            if (rescanBtn != null)
            {
                rescanBtn.Click += OnClickRescanGame;
            }

            SMAPIInstaller.OnInstalledSMAPI += NotifyInstalledSMAPIInfo;
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error: Try to setup bind UI Event");
            ErrorDialogTool.Show(ex);
            return;
        }

        // Refresh UI state
        RefreshLauncherInfo();
        NotifyInstalledSMAPIInfo();
    }

    public void RefreshLauncherInfo()
    {
        try
        {
            var launcherInfoLines = new StringBuilder();
            launcherInfoLines.AppendLine("Launcher Version: " + AppInfo.VersionString);

            var buildDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(int.Parse(AppInfo.BuildString));
            var localDateTimeOffset = buildDateTimeOffset.ToLocalTime();
            var localDateTimeString = localDateTimeOffset.ToString("HH:mm:ss dd/MM/yyyy");
            launcherInfoLines.AppendLine($"Build: {localDateTimeString} (d/m/y)");
            launcherInfoLines.AppendLine("Support: Stardew Valley 1.6+ (CH Play / Galaxy / Mod APK)");

            if (StardewApkTool.IsInstalled)
            {
                launcherInfoLines.AppendLine($"Trạng thái game: Đã phát hiện ({StardewApkTool.DetectionStatus})");
                launcherInfoLines.AppendLine($"Phiên bản: {StardewApkTool.CurrentGameVersion}");
                if (StardewApkTool.CurrentPackageInfo != null)
                {
                    launcherInfoLines.AppendLine("Package: " + StardewApkTool.CurrentPackageInfo.PackageName);
                }

                if (StardewApkTool.CanReadApk)
                {
                    launcherInfoLines.AppendLine("Đọc file APK: OK (Sẵn sàng khởi chạy)");
                }
                else
                {
                    launcherInfoLines.AppendLine("⚠️ Cảnh báo: Quyền đọc file APK bị chặn bởi hệ thống. Vui lòng bấm 'Chọn file APK' bên dưới để nạp trực tiếp!");
                }
            }
            else
            {
                launcherInfoLines.AppendLine("⚠️ Trạng thái game: Chưa phát hiện Stardew Valley!");
                launcherInfoLines.AppendLine("• Hãy cài đặt game hoặc bấm 'Chọn file APK' để nạp file .apk.");
            }

            launcherInfoLines.AppendLine("Discord: Stardew SMAPI Thailand");
            launcherInfoLines.AppendLine("Developer: NRTnarathip, Eky-Team");

            FindViewById<TextView>(ResourceConstant.Id.launcherInfoTextView).Text = launcherInfoLines.ToString();
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error setup app text info: " + ex);
            ErrorDialogTool.Show(ex);
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
                RefreshLauncherInfo();
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
        RefreshLauncherInfo();
        if (StardewApkTool.IsInstalled)
        {
            ToastNotifyTool.Notify("Đã tìm thấy game: " + StardewApkTool.DetectionStatus);
        }
        else
        {
            ToastNotifyTool.Notify("Vẫn chưa tìm thấy game. Vui lòng kiểm tra lại hoặc bấm 'Chọn file APK'.");
        }
    }

    private void NotifyInstalledSMAPIInfo()
    {
        var smapiInstallInfo = FindViewById<TextView>(ResourceConstant.Id.SMAPIInstallInfoTextView);
        if (SMAPIInstaller.IsInstalled is false)
        {
            smapiInstallInfo.Text = "Please install SMAPI!!";
            return;
        }

        var lines = new StringBuilder();
        lines.AppendLine($"SMAPI Version: {SMAPIInstaller.GetCurrentVersion()}");
        lines.AppendLine($"SMAPI Build: {SMAPIInstaller.GetBuildCode()}");
        smapiInstallInfo.Text = lines.ToString();
    }

    private void OnClickStartGame()
    {
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

        Console.WriteLine("On click start game");
        EntryGame.LaunchGameActivity(this);
        Console.WriteLine("done continue UI runner");
    }
}