#nullable enable
using _Microsoft.Android.Resource.Designer;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Button;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.Text;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace SMAPIGameLoader.Launcher.Fragments;

public class ModsFragment : Fragment
{
    private readonly List<ModItemView> mods = new();
    private ModAdapter? modAdapter;
    private ListView? modsListView;
    private TextView? foundModsText;
    private MaterialButton? installModBtn;
    private MaterialButton? openFolderModsBtn;

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        return inflater.Inflate(ResourceConstant.Layout.FragmentMods, container, false);
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        modsListView = view.FindViewById<ListView>(ResourceConstant.Id.modsListViews);
        foundModsText = view.FindViewById<TextView>(ResourceConstant.Id.foundModsText);
        installModBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.InstallModBtn);
        openFolderModsBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.OpenFolderModsBtn);

        if (Activity != null && modsListView != null)
        {
            modAdapter = new ModAdapter(Activity, mods);
            modsListView.Adapter = modAdapter;

            modsListView.ItemClick += (sender, e) =>
            {
                OnClickModItemView(e);
            };
        }

        if (installModBtn != null)
        {
            installModBtn.Click += async (sender, e) =>
            {
                ModInstaller.OnClickInstallMod(OnInstalledCallback: () =>
                {
                    RefreshMods();
                });
            };
        }

        if (openFolderModsBtn != null)
        {
            openFolderModsBtn.Click += OnClick_OpenFolderMods;
        }

        RefreshMods();
    }

    public override void OnResume()
    {
        base.OnResume();
        RefreshMods();
    }

    private void OnClick_OpenFolderMods(object? sender, EventArgs e)
    {
        FileTool.OpenAppFilesExternalFilesDir("Mods");
    }

    public void RefreshMods()
    {
        if (Activity == null || !IsAdded)
            return;

        Activity.RunOnUiThread(() =>
        {
            try
            {
                mods.Clear();
                Console.WriteLine("Start Refresh Mods in ModsFragment..");

                var manifestFiles = new List<string>();
                ModTool.FindManifestFile(ModTool.ModsDir, manifestFiles);

                for (int i = 0; i < manifestFiles.Count; i++)
                {
                    var mod = new ModItemView(manifestFiles[i], i);
                    mods.Add(mod);
                }

                modAdapter?.RefreshMods();

                if (foundModsText != null)
                {
                    foundModsText.Text = "Tìm thấy: " + mods.Count + " mods";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RefreshMods error: " + ex);
            }
        });
    }

    private void OnClickModItemView(AdapterView.ItemClickEventArgs e)
    {
        if (modAdapter == null)
            return;

        var mod = modAdapter.GetModOnClick(e);
        var text = new StringBuilder();
        text.AppendLine($"Mod: {mod.NameText}");
        text.AppendLine($"{mod.VersionText}");
        text.AppendLine();
        text.AppendLine("Bạn có muốn xóa mod này không?");
        DialogTool.Show(
            "❌ Xóa: " + mod.NameText,
            text.ToString(),
            buttonOKName: "Xóa mod",
            onClickYes: () =>
            {
                OnClickDeleteMod(mod);
            }
        );
    }

    private void OnClickDeleteMod(ModItemView mod)
    {
        Console.WriteLine("Try delete mod: " + mod.modName);
        if (ModInstaller.TryDeleteMod(mod.modFolderPath, true))
        {
            ToastNotifyTool.Notify("Đã xóa mod: " + mod.modName);
            RefreshMods();
        }
    }
}
