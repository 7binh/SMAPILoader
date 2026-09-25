#nullable enable
using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Button;
using Google.Android.Material.FloatingActionButton;
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
    private MaterialButton? openFolderModsBtn;
    private FloatingActionButton? fabAddMod;
    private MaterialButtonToggleGroup? sortToggleGroup;
    private MaterialButton? btnSortDefault;
    private MaterialButton? btnSortDate;
    private bool sortByNewest = false;

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        return inflater.Inflate(ResourceConstant.Layout.FragmentMods, container, false);
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        modsListView = view.FindViewById<ListView>(ResourceConstant.Id.modsListViews);
        foundModsText = view.FindViewById<TextView>(ResourceConstant.Id.foundModsText);
        openFolderModsBtn = view.FindViewById<MaterialButton>(ResourceConstant.Id.OpenFolderModsBtn);
        fabAddMod = view.FindViewById<FloatingActionButton>(ResourceConstant.Id.fabAddMod);
        sortToggleGroup = view.FindViewById<MaterialButtonToggleGroup>(ResourceConstant.Id.sortToggleGroup);
        btnSortDefault = view.FindViewById<MaterialButton>(ResourceConstant.Id.btnSortDefault);
        btnSortDate = view.FindViewById<MaterialButton>(ResourceConstant.Id.btnSortDate);

        var prefs = Activity?.GetSharedPreferences("smapi_prefs", FileCreationMode.Private);
        sortByNewest = prefs?.GetBoolean("mods_sort_by_newest", false) ?? false;

        if (sortToggleGroup != null)
        {
            if (sortByNewest)
            {
                sortToggleGroup.Check(ResourceConstant.Id.btnSortDate);
            }
            else
            {
                sortToggleGroup.Check(ResourceConstant.Id.btnSortDefault);
            }
        }

        if (btnSortDefault != null)
        {
            btnSortDefault.Click += (s, e) =>
            {
                try { btnSortDefault.PerformHapticFeedback(FeedbackConstants.ContextClick); } catch { }
                if (sortByNewest)
                {
                    sortByNewest = false;
                    sortToggleGroup?.Check(ResourceConstant.Id.btnSortDefault);
                    prefs?.Edit()?.PutBoolean("mods_sort_by_newest", false)?.Apply();
                    ApplySort();
                }
            };
        }

        if (btnSortDate != null)
        {
            btnSortDate.Click += (s, e) =>
            {
                try { btnSortDate.PerformHapticFeedback(FeedbackConstants.ContextClick); } catch { }
                if (!sortByNewest)
                {
                    sortByNewest = true;
                    sortToggleGroup?.Check(ResourceConstant.Id.btnSortDate);
                    prefs?.Edit()?.PutBoolean("mods_sort_by_newest", true)?.Apply();
                    ApplySort();
                }
            };
        }

        if (Activity != null && modsListView != null)
        {
            modAdapter = new ModAdapter(Activity, mods);
            modsListView.Adapter = modAdapter;

            modsListView.ItemClick += (sender, e) =>
            {
                OnClickModItemView(e);
            };
        }

        if (fabAddMod != null)
        {
            fabAddMod.Click += (sender, e) =>
            {
                try
                {
                    fabAddMod.PerformHapticFeedback(FeedbackConstants.ContextClick);
                }
                catch { }

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

    private void ApplySort()
    {
        if (sortByNewest)
        {
            // Sort newest to oldest
            mods.Sort((a, b) => b.InstalledDate.CompareTo(a.InstalledDate));
        }
        else
        {
            // Sort alphabetical by name
            mods.Sort((a, b) => string.Compare(a.modName, b.modName, StringComparison.OrdinalIgnoreCase));
        }

        for (int i = 0; i < mods.Count; i++)
        {
            mods[i].UpdateIndex(i);
        }

        modAdapter?.RefreshMods();
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

                ApplySort();

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
        if (mod == null)
            return;

        DialogTool.ConfirmDelete(
            mod.modName,
            $"Phiên bản: {mod.modVersion}\n{mod.FolderPathText}",
            onConfirmDelete: () =>
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
