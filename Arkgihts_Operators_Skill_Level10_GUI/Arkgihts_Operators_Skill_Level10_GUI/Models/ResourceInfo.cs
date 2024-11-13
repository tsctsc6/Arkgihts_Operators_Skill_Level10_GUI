using System.Collections.Generic;

namespace Arkgihts_Operators_Skill_Level10_GUI.Models;

public class ResourceInfo
{
    public string[] OperatorList { get; set; } = [];
    public Material[] MaterialList { get; set; } = [];
}

public class Material
{
    public string Name { get; set; } = string.Empty;
    public int Rarity {get; set;} = 0;
    public KeyValuePair<string, int>[] Composition { get; set; } = [];
}