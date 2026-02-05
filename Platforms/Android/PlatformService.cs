using Microsoft.Maui.Platform;

namespace MauiExtensions.Services
{
    public partial class PlatformService
    {
        internal static partial double GetNavBarHeight() => Android.App.Application.Context.FromPixels(Android.App.Application.Context.GetActionBarHeight());
    }
}
