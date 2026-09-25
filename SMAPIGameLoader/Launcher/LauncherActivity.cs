#nullable enable
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using AndroidX.ViewPager2.Adapter;
using AndroidX.ViewPager2.Widget;
using SMAPIGameLoader.Launcher.Fragments;
using SMAPIGameLoader.Tool;
using System;
using Xamarin.Essentials;
using Fragment = AndroidX.Fragment.App.Fragment;

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
    public static LauncherActivity Instance { get; private set; } = null!;

    private static bool IsDeviceSupport => IntPtr.Size == 8;

    private ViewPager2? mainViewPager;
    private LinearLayout? tabHome;
    private LinearLayout? tabMods;
    private LinearLayout? tabTools;

    private LinearLayout? tabHomeIndicator;
    private LinearLayout? tabModsIndicator;
    private LinearLayout? tabToolsIndicator;

    private ImageView? tabHomeIcon;
    private ImageView? tabModsIcon;
    private ImageView? tabToolsIcon;

    private TextView? tabHomeText;
    private TextView? tabModsText;
    private TextView? tabToolsText;

    private int colorActive;
    private int colorInactive;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Instance = this;
        base.OnCreate(savedInstanceState);

        // Edge-to-Edge window
        WindowCompat.SetDecorFitsSystemWindows(Window!, false);

        SetContentView(ResourceConstant.Layout.LauncherLayout);

        Platform.Init(this, savedInstanceState);
        ActivityTool.Init(this);

        // Assert device architecture
        AssertRequirement();

        SetDarkMode();
        SetupViews();
        SetupViewPager();

        // Run utils scripts
        ProcessAdbExtras();
    }

    private void SetDarkMode()
    {
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
    }

    private void AssertRequirement()
    {
        if (IsDeviceSupport is false)
        {
            ToastNotifyTool.Notify("Không hỗ trợ thiết bị 32-bit (chỉ hỗ trợ 64-bit)");
            Finish();
        }
    }

    private void ProcessAdbExtras()
    {
        if (AdbExtraTool.IsClickStartGame(this))
        {
            if (StardewApkTool.IsInstalled && StardewApkTool.CanReadApk)
            {
                EntryGame.LaunchGameActivity(this);
            }
        }
    }

    private void SetupViews()
    {
        mainViewPager = FindViewById<ViewPager2>(ResourceConstant.Id.mainViewPager);

        tabHome = FindViewById<LinearLayout>(ResourceConstant.Id.tabHome);
        tabMods = FindViewById<LinearLayout>(ResourceConstant.Id.tabMods);
        tabTools = FindViewById<LinearLayout>(ResourceConstant.Id.tabTools);

        tabHomeIndicator = FindViewById<LinearLayout>(ResourceConstant.Id.tabHomeIndicator);
        tabModsIndicator = FindViewById<LinearLayout>(ResourceConstant.Id.tabModsIndicator);
        tabToolsIndicator = FindViewById<LinearLayout>(ResourceConstant.Id.tabToolsIndicator);

        tabHomeIcon = FindViewById<ImageView>(ResourceConstant.Id.tabHomeIcon);
        tabModsIcon = FindViewById<ImageView>(ResourceConstant.Id.tabModsIcon);
        tabToolsIcon = FindViewById<ImageView>(ResourceConstant.Id.tabToolsIcon);

        tabHomeText = FindViewById<TextView>(ResourceConstant.Id.tabHomeText);
        tabModsText = FindViewById<TextView>(ResourceConstant.Id.tabModsText);
        tabToolsText = FindViewById<TextView>(ResourceConstant.Id.tabToolsText);

        colorActive = ContextCompat.GetColor(this, ResourceConstant.Color.md_theme_onSecondaryContainer);
        colorInactive = ContextCompat.GetColor(this, ResourceConstant.Color.md_theme_onSurfaceVariant);

        if (tabHome != null)
        {
            tabHome.Click += (s, e) =>
            {
                tabHome.PerformHapticFeedback(FeedbackConstants.ContextClick);
                mainViewPager?.SetCurrentItem(0, true);
            };
        }

        if (tabMods != null)
        {
            tabMods.Click += (s, e) =>
            {
                tabMods.PerformHapticFeedback(FeedbackConstants.ContextClick);
                mainViewPager?.SetCurrentItem(1, true);
            };
        }

        if (tabTools != null)
        {
            tabTools.Click += (s, e) =>
            {
                tabTools.PerformHapticFeedback(FeedbackConstants.ContextClick);
                mainViewPager?.SetCurrentItem(2, true);
            };
        }
    }

    private void SetupViewPager()
    {
        if (mainViewPager == null)
            return;

        var adapter = new LauncherPagerAdapter(this);
        mainViewPager.Adapter = adapter;
        mainViewPager.OffscreenPageLimit = 2;

        mainViewPager.RegisterOnPageChangeCallback(new PageChangeCallback(this));
        UpdateTabSelection(0);
    }

    public void UpdateTabSelection(int position)
    {
        // Tab 0: Home
        SetTabState(tabHomeIndicator, tabHomeIcon, tabHomeText, position == 0);

        // Tab 1: Mods
        SetTabState(tabModsIndicator, tabModsIcon, tabModsText, position == 1);

        // Tab 2: Tools
        SetTabState(tabToolsIndicator, tabToolsIcon, tabToolsText, position == 2);
    }

    private void SetTabState(LinearLayout? indicator, ImageView? icon, TextView? text, bool isActive)
    {
        if (indicator != null)
        {
            if (isActive)
            {
                indicator.SetBackgroundResource(ResourceConstant.Drawable.pill_tab_background);
            }
            else
            {
                indicator.Background = null;
            }
        }

        int color = isActive ? colorActive : colorInactive;

        if (icon != null)
        {
            icon.SetColorFilter(new Color(color), PorterDuff.Mode.SrcIn);
        }

        if (text != null)
        {
            text.SetTextColor(new Color(color));
            text.Typeface = isActive ? Typeface.DefaultBold : Typeface.Default;
        }
    }

    private class PageChangeCallback : ViewPager2.OnPageChangeCallback
    {
        private readonly LauncherActivity activity;

        public PageChangeCallback(LauncherActivity activity)
        {
            this.activity = activity;
        }

        public override void OnPageSelected(int position)
        {
            base.OnPageSelected(position);
            activity.UpdateTabSelection(position);
        }
    }

    private class LauncherPagerAdapter : FragmentStateAdapter
    {
        public LauncherPagerAdapter(FragmentActivity activity) : base(activity) { }

        public override int ItemCount => 3;

        public override Fragment CreateFragment(int position)
        {
            return position switch
            {
                0 => new HomeFragment(),
                1 => new ModsFragment(),
                2 => new ToolsFragment(),
                _ => new HomeFragment()
            };
        }
    }
}