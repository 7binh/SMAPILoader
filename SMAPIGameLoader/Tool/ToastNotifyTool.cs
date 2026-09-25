#nullable enable
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Widget;
using Google.Android.Material.Snackbar;
using SMAPIGameLoader.Tool;
using System;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class ToastNotifyTool
{
    public static void Notify(string message, ToastLength duration = ToastLength.Short)
    {
        TaskTool.RunMainThread(() =>
        {
            try
            {
                var activity = ActivityTool.CurrentActivity;
                if (activity != null && !activity.IsFinishing && !activity.IsDestroyed)
                {
                    var rootView = activity.FindViewById(Android.Resource.Id.Content);
                    if (rootView != null)
                    {
                        var snackbarLength = duration == ToastLength.Long ? Snackbar.LengthLong : Snackbar.LengthShort;
                        var snackbar = Snackbar.Make(rootView, message, snackbarLength);

                        var pillBar = activity.FindViewById(ResourceConstant.Id.floatingPillBar);
                        if (pillBar != null)
                        {
                            snackbar.SetAnchorView(pillBar);
                        }

                        snackbar.Show();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Snackbar exception fallback: " + ex.Message);
            }

            // Fallback to native system toast
            Toast.MakeText(Application.Context, message, duration)?.Show();
        });
    }
}
