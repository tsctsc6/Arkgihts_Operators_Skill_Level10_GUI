using System;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using AngleSharp.Html.Parser;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Arkgihts_Operators_Skill_Level10_GUI.ViewModels;
using Arkgihts_Operators_Skill_Level10_GUI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Arkgihts_Operators_Skill_Level10_GUI;

public partial class App : Application
{
    public new static App Current => (Application.Current as App)!;
    
    public IServiceProvider ServiceProvider { get; }
    
    public App()
    {
        ServiceProvider = ConfigureServices();
    }

    private IServiceProvider ConfigureServices()
    {
        var serviceCollention = new ServiceCollection()
            .AddSingleton<HttpClient>()
            .AddSingleton<HtmlParser>()
            .AddSingleton<JsonSerializerOptions>(_ => new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true,
                IndentSize = 4,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            })
            .AddSingleton<MainViewModel>();
        return serviceCollention.BuildServiceProvider();
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}