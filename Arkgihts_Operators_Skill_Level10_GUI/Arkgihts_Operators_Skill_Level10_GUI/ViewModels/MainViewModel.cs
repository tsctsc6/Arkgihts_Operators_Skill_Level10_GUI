using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Arkgihts_Operators_Skill_Level10_GUI.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Arkgihts_Operators_Skill_Level10_GUI.ViewModels;

public partial class MainViewModel(HttpClient httpClient, HtmlParser htmlParser) : ViewModelBase
{
    private IEnumerable<string> _operatorList = [];
    private IEnumerable<Material> _materialList = [];

    [RelayCommand]
    private async Task GetResourceInfoAsync()
    {
        await Task.WhenAll([GetOperatorListAsync(), GetMaterialListAsync()]);
        await File.WriteAllTextAsync("ResourceInfo.json", JsonSerializer.Serialize(new ResourceInfo()
        {
            OperatorList = _operatorList.ToArray(),
            MaterialList = _materialList.ToArray()
        }, App.Current.ServiceProvider.GetRequiredService<JsonSerializerOptions>()));
    }
    
    private async Task GetOperatorListAsync()
    {
        var resp = await httpClient.GetAsync("https://prts.wiki/w/%E5%B9%B2%E5%91%98%E4%B8%80%E8%A7%88");
        resp.EnsureSuccessStatusCode();
        var document = htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var operatorData = document.QuerySelector("div#filter-data")?.Children;
        _operatorList = operatorData?.Select(e => e.Attributes["data-zh"]?.Value).Where(n => n is not null)
                   .Select(n => n!) ?? [];
    }
    
    private async Task GetMaterialListAsync()
    {
        var resp = await httpClient.GetAsync("https://prts.wiki/w/%E9%81%93%E5%85%B7%E4%B8%80%E8%A7%88");
        resp.EnsureSuccessStatusCode();
        var document = htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var materialData = document.QuerySelector("div.mw-parser-output")?.Children
            .Where(e =>
            {
                if (!int.TryParse(e.Attributes["data-rarity"]?.Value, out var rarity)) return false;
                if (rarity < 2) return false;
                if (!(e.Attributes["data-category"]?.Value.Contains("分类:材料") ?? false)) return false;
                return (e.Attributes["data-category"]?.Value.Contains("分类:加工站产物") ?? false) ||
                       e.Attributes["data-obtain_approach"]?.Value == "常规关卡掉落";
            })
            .Select(e => new Material()
            {
                Name = e.Attributes["data-name"]?.Value ?? string.Empty,
                Rarity = int.Parse(e.Attributes["data-rarity"]?.Value ?? string.Empty),
            }).ToArray();
        if (materialData is null) return;
        await Task.WhenAll(materialData.Where(m => m.Rarity > 2).Select(GetCompositionAsync));
        _materialList = materialData;
    }

    private async Task GetCompositionAsync(Material material)
    {
        var resp = await httpClient.GetAsync($"https://prts.wiki/w/{material.Name}");
        resp.EnsureSuccessStatusCode();
        var document = htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var composition = document.QuerySelector("span#加工站")?.ParentElement?.NextElementSibling?
            .QuerySelector("tbody > tr:nth-child(2) > td > div")?.Children;
        if (composition is null) return;
        material.Composition = [];
        var pairs = new List<KeyValuePair<string, int>>(3);
        foreach (var compositionItem in composition)
        {
            if (!int.TryParse(compositionItem.QuerySelector("span")?.InnerHtml, out var count)) continue;
            pairs.Add(new(
                compositionItem.QuerySelector("a")?.Attributes["title"]?.Value ?? string.Empty, count
            ));
        }
        material.Composition = pairs.ToArray();
    }
}