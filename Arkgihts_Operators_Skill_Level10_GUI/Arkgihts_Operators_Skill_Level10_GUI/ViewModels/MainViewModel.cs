using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arkgihts_Operators_Skill_Level10_GUI.ViewModels;

public partial class MainViewModel(HttpClient httpClient, HtmlParser htmlParser) : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Welcome to Avalonia!";

    [RelayCommand]
    private async Task<IEnumerable<string>> GetOperatorListAsync()
    {
        var resp = await httpClient.GetAsync("https://prts.wiki/w/%E5%B9%B2%E5%91%98%E4%B8%80%E8%A7%88");
        resp.EnsureSuccessStatusCode();
        var document = htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var operatorData = document.QuerySelector("div#filter-data")?.Children;
        return operatorData?.Select(e => e.Attributes["data-zh"]?.Value).Where(n => n is not null).Select(n => n!) ?? [];
    }
}