using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Arkgihts_Operators_Skill_Level10_GUI.Models;
using Arkgihts_Operators_Skill_Level10_GUI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Arkgihts_Operators_Skill_Level10_GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private FrozenDictionary<string, Material> _materialList;
    private readonly HttpClient _httpClient;
    private readonly HtmlParser _htmlParser;

    [ObservableProperty]
    private ObservableCollection<string> _operatorList = [];
    
    [ObservableProperty]
    private string _selectedOperator = string.Empty;
    
    [ObservableProperty]
    private int _selectedSkillIndex;
    
    public MainViewModel(HttpClient httpClient, HtmlParser htmlParser)
    {
        _httpClient = httpClient;
        _htmlParser = htmlParser;
        LoadResourceInfo();
    }
    
    [MemberNotNull(nameof(_materialList))]
    private void LoadResourceInfo()
    {
        OperatorList = [];
        _materialList = Array.Empty<Material>().ToFrozenDictionary(m => m.Name);
        
        if (!File.Exists(App.ResourceInfoPath)) return;
        var resourceInfo = JsonSerializer.Deserialize<ResourceInfo>(File.ReadAllText("ResourceInfo.json"),
            App.Current.ServiceProvider.GetRequiredService<JsonSerializerOptions>());
        if (resourceInfo == null) return;

        Array.ForEach(resourceInfo.OperatorList, s => OperatorList.Add(s));
        _materialList = resourceInfo.MaterialList.ToFrozenDictionary(m => m.Name);
    }

    [RelayCommand]
    private async Task LoadDepotFromClipboardAsync()
    {
        var clipboard = App.Current.ServiceProvider.GetRequiredService<MainWindow>().Clipboard;
        if (clipboard == null) return;
        var content = await clipboard.GetTextAsync();
        if (string.IsNullOrEmpty(content)) return;
        var depot = JsonSerializer.Deserialize<Depot>(content,
            App.Current.ServiceProvider.GetRequiredService<JsonSerializerOptions>());
        if (depot == null) return;
        await File.WriteAllTextAsync(App.DepotPath, JsonSerializer.Serialize(depot,
            App.Current.ServiceProvider.GetRequiredService<JsonSerializerOptions>()));
    }
    
    [RelayCommand]
    private async Task CalculateSkillMaterialAsync()
    {
        var depot = JsonSerializer.Deserialize<Depot>(await File.ReadAllTextAsync(App.DepotPath),
            App.Current.ServiceProvider.GetRequiredService<JsonSerializerOptions>());
        if (depot == null) return;
        var skillInfo = await GetOperatorSkillInfoAsync();
        var depotDic = depot.Items.Where(m => _materialList.ContainsKey(m.Name))
            .ToDictionary(m => m.Name);
        foreach (var item in _materialList)
        {
            depotDic.TryAdd(item.Key, new()
            {
                Name = item.Key,
                Have = 0
            });
        }
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 2; j++)
            {
                depotDic[skillInfo[SelectedSkillIndex, i, j].Key].Have -= skillInfo[SelectedSkillIndex, i, j].Value;
            }
        }

        var lackDirectly = depotDic.Where(d => d.Value.Have < 0)
            .OrderByDescending(d => _materialList[d.Key].Rarity);
        foreach (var item in lackDirectly)
        {
            foreach (var item2 in _materialList[item.Key].Composition)
            {
                // 已知 item.Value.Have < 0
                depotDic[item2.Key].Have += item2.Value * item.Value.Have;
            }
        }
        
        var needComposition = depotDic
            .Where(d => _materialList[d.Key].Rarity > 2 && d.Value.Have < 0)
            .Select(d => new KeyValuePair<string, int>(d.Key, -d.Value.Have))
            .OrderBy(d => _materialList[d.Key].Rarity);

        var lackRarity2 = depotDic
            .Where(d => _materialList[d.Key].Rarity == 2 && d.Value.Have < 0)
            .Select(d => new KeyValuePair<string, int>(d.Key, -d.Value.Have));
    }
    
    private async Task<KeyValuePair<string, int>[,,]> GetOperatorSkillInfoAsync()
    {
        var skillInfo = new KeyValuePair<string, int>[3, 3, 2];
        var resp = await _httpClient.GetAsync($"https://prts.wiki/w/{SelectedOperator}");
        resp.EnsureSuccessStatusCode();
        using var document = _htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var table = document.QuerySelector("span#技能升级材料")?.ParentElement?
            .NextElementSibling?.NextElementSibling?
            .QuerySelector("tbody")?.Children.Skip(9);
        if (table == null) return skillInfo;
        foreach (var (i, tr) in table.Index())
        {
            var d = Math.DivRem(i, 4, out var r);
            if (r == 0) continue;
            var td = tr.QuerySelector("td");
            if (td == null) continue;
            foreach (var (j, div) in td.Children.Index())
            {
                if (j == 0) continue;
                var name = div.QuerySelector("a")?.Attributes["title"]?.Value;
                if (string.IsNullOrEmpty(name)) continue;
                if (!int.TryParse(div.QuerySelector("span")?.TextContent, out var count)) continue;
                skillInfo[d, r - 1, j - 1] = new KeyValuePair<string, int>(name, count);
            }
        }
        return skillInfo;
    }
    
    [RelayCommand]
    private async Task GetResourceInfoAsync()
    {
        await Task.WhenAll([GetOperatorListAsync(), GetMaterialListAsync()]);
        await File.WriteAllTextAsync("ResourceInfo.json", JsonSerializer.Serialize(new ResourceInfo()
        {
            OperatorList = OperatorList.ToArray(),
            MaterialList = _materialList.Select(kv => kv.Value).ToArray(),
        }, App.Current.ServiceProvider.GetRequiredService<JsonSerializerOptions>()));
    }
    
    private async Task GetOperatorListAsync()
    {
        var resp = await _httpClient.GetAsync("https://prts.wiki/w/%E5%B9%B2%E5%91%98%E4%B8%80%E8%A7%88");
        resp.EnsureSuccessStatusCode();
        using var document = _htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
        var operatorData = document.QuerySelector("div#filter-data")?.Children;
        var operatorList = operatorData?.Select(e => e.Attributes["data-zh"]?.Value).Where(n => n is not null)
                   .Select(n => n!) ?? [];
        OperatorList.Clear();
        foreach (var s in operatorList)
        {
            OperatorList.Add(s);
        }
    }
    
    private async Task GetMaterialListAsync()
    {
        var resp = await _httpClient.GetAsync("https://prts.wiki/w/%E9%81%93%E5%85%B7%E4%B8%80%E8%A7%88");
        resp.EnsureSuccessStatusCode();
        using var document = _htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
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
        _materialList = materialData.ToFrozenDictionary(m => m.Name);
    }

    private async Task GetCompositionAsync(Material material)
    {
        var resp = await _httpClient.GetAsync($"https://prts.wiki/w/{material.Name}");
        resp.EnsureSuccessStatusCode();
        using var document = _htmlParser.ParseDocument(await resp.Content.ReadAsStringAsync());
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