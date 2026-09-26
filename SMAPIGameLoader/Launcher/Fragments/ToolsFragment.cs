#nullable enable
using _Microsoft.Android.Resource.Designer;
using Android.OS;
using Android.Views;
using Google.Android.Material.Button;
using SMAPIGameLoader.Tool;
using System;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace SMAPIGameLoader.Launcher.Fragments;

public class ToolsFragment : Fragment
{
    private MaterialButton? restoreDefaultSMAPIBtn;
    private MaterialButton? installSMAPIZipBtn;
    private MaterialButton? uploadLogBtn;
    private MaterialButton? importSaveZipBtn;
    private MaterialButton? clearCacheBtn;

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        return inflater.Inflate(ResourceConstant.Layout.FragmentTools, container, false);
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        restoreDefaultSMAPIBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.RestoreDefaultSMAPIBtn);
        installSMAPIZipBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.InstallSMAPIZip);
        uploadLogBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.UploadLog);
        importSaveZipBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.ImportSaveZipBtn);
        clearCacheBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.ClearCacheBtn);

        if (restoreDefaultSMAPIBtn != null)
        {
            restoreDefaultSMAPIBtn.Click += (sender, e) =>
            {
                DialogTool.Show(
                    "Khôi phục SMAPI tích hợp",
                    "Bạn có muốn ghi đè và cài đặt lại phiên bản SMAPI Android mặc định tích hợp sẵn không?",
                    buttonOKName: "Khôi phục",
                    buttonCancelName: "Hủy",
                    onClickYes: () =>
                    {
                        bool success = SMAPIInstaller.InstallSMAPIFromAsset();
                        if (success)
                        {
                            ToastNotifyTool.Notify("Đã khôi phục SMAPI tích hợp sẵn thành công!");
                        }
                        else
                        {
                            ToastNotifyTool.Notify("Không thể cài đặt SMAPI từ asset tích hợp.");
                        }
                    }
                );
            };
        }

        if (installSMAPIZipBtn != null)
            installSMAPIZipBtn.Click += SMAPIInstaller.OnClickInstallSMAPIZip;

        if (uploadLogBtn != null)
            uploadLogBtn.Click += LogParser.OnClickUploadLog;

        if (importSaveZipBtn != null)
            importSaveZipBtn.Click += SaveManager.OnClickImportSaveZip;

        if (clearCacheBtn != null)
        {
            clearCacheBtn.Click += (sender, e) =>
            {
                FileTool.ClearCache();
                ToastNotifyTool.Notify("Đã dọn dẹp bộ nhớ đệm cache thành công!");
            };
        }
    }
}
