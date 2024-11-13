using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace Arkgihts_Operators_Skill_Level10_GUI.Android;

[Activity(
    Label = "Arkgihts_Operators_Skill_Level10_GUI.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}