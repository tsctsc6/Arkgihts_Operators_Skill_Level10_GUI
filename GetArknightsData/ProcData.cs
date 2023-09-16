using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GetArknightsData
{
    public class ProcData
    {
        static public Dictionary<string, ResourceInfo> CalSynthesisTable()
        {
            Dictionary<string, ResourceInfo> ResourceDictionary = new();
            string json = "";
            try
            {
                json = File.ReadAllText(@".\Data\材料名单.json");
            }
            catch(FileNotFoundException) { return ResourceDictionary; }
            var rc = JsonSerializer.Deserialize<ResourceInfoCollection>(json);
            foreach(var item in rc.resources)
            {
                ResourceDictionary.Add(item.name, item);
            }
            return ResourceDictionary;
        }
        /// <summary>
        /// 获得材料的合成所需
        /// </summary>
        /// <param name="ResourceDictionary">材料合成表</param>
        /// <param name="name">材料名称</param>
        /// <param name="num">数量</param>
        /// <param name="targetrRrity">要计算到的稀有度</param>
        /// <returns>结果</returns>
        static public Dictionary<string, int> CalSynthesis(
            in Dictionary<string, ResourceInfo> ResourceDictionary, string name, int num, int targetrRrity)
        {
            Dictionary<string, int> dic = new();
            if (CalSynthesis(ResourceDictionary, name, num, targetrRrity, dic))
                return dic;
            return new Dictionary<string, int>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ResourceDictionary">材料合成表</param>
        /// <param name="name">材料名称</param>
        /// <param name="num">数量</param>
        /// <param name="targetrRrity">要计算到的稀有度</param>
        /// <param name="dic"></param>
        /// <returns>
        /// false: 计算过程中出现找不到某种材料
        /// true: 计算过程正常
        /// </returns>
        static private bool CalSynthesis(
            in Dictionary<string, ResourceInfo> ResourceDictionary, string name, int num, int targetrRrity,
            Dictionary<string, int> dic)
        {
            ResourceInfo? resourceInfo;
            try { resourceInfo = ResourceDictionary[name]; }
            catch(KeyNotFoundException) { return false; }
            if (resourceInfo.rarity == targetrRrity + 1)
            {
                if (resourceInfo.synthesisItem.Length == 0) return false;
                foreach (var si in resourceInfo.synthesisItem)
                {
                    if (!dic.ContainsKey(si.name))
                        dic.Add(si.name, si.count * num);
                    else
                    {
                        dic[si.name] = dic[si.name] + si.count * num;
                    }
                }
                return true;
            }
            if (resourceInfo.rarity > targetrRrity + 1)
                foreach (var si in resourceInfo.synthesisItem)
                    if (CalSynthesis(ResourceDictionary, si.name, si.count, targetrRrity, dic))
                        return false;
            return true;
        }

    }
}
