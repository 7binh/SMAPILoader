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

        installSMAPIZipBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.InstallSMAPIZip);
        uploadLogBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.UploadLog);
        importSaveZipBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.ImportSaveZipBtn);
        clearCacheBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.ClearCacheBtn);

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
