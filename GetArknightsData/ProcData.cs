using System.Text.Json;

namespace GetArknightsData
{
    public class ProcData
    {
        private Dictionary<string, ResourceInfo>? ResourceDictionary;
        private Dictionary<string, int>? depot;
        public ProcData(ResourceInfoCollection? rc = null)
        {
            if (rc == null)
            {
                ResourceDictionary = null;
                return;
            }
            ResourceDictionary = new();
            foreach(var item in rc.Resources)
            {
                ResourceDictionary.Add(item.Name, item);
            }
            return;
        }
        /// <summary>
        /// 对于dic2所有Key，dic1中如果存在该Key，则执行op；dic1中如果不存在该Key，则初始化该Key并执行op；返回全新的dic。
        /// </summary>
        /// <param name="dic1"></param>
        /// <param name="dic2"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        static public Dictionary<string, int> DicOperator(in Dictionary<string, int> dic1, in Dictionary<string, int> dic2,
            Func<int, int, int> op)
        {
            Dictionary<string, int> dic = new(dic1);
            foreach (var item in dic2)
            {
                if (!dic.ContainsKey(item.Key)) dic.Add(item.Key, 0);
                dic[item.Key] = op(dic[item.Key], item.Value);
            }
            return dic;
        }
        static public Dictionary<string, int> DicSelfOperator(Dictionary<string, int> dic1, in Dictionary<string, int> dic2,
            Func<int, int, int> op)
        {
            foreach (var item in dic2)
            {
                if (!dic1.ContainsKey(item.Key)) dic1.Add(item.Key, 0);
                dic1[item.Key] = op(dic1[item.Key], item.Value);
            }
            return dic1;
        }
        static public void DicSelfOperator(Dictionary<string, int> dic, string key, int value,
            Func<int, int, int> op)
        {
            if (!dic.ContainsKey(key)) dic.Add(key, 0);
            dic[key] = op(dic[key], value);
        }
        /// <summary>
        /// 获得材料的合成所需
        /// </summary>
        /// <param name="ResourceDictionary">材料字典</param>
        /// <param name="name">材料名称</param>
        /// <param name="num">数量</param>
        /// <param name="targetrRrity">要计算到的稀有度</param>
        /// <returns>结果</returns>
        public Dictionary<string, int> CalSynthesis(string name, int num, int targetrRrity)
        {
            Dictionary<string, int> dic = new();
            if (CalSynthesis(name, num, targetrRrity, dic))
                return dic;
            return new Dictionary<string, int>();
        }
        /// <summary>
        /// 用于递归
        /// </summary>
        /// <param name="ResourceDictionary">材料字典</param>
        /// <param name="name">材料名称</param>
        /// <param name="num">数量</param>
        /// <param name="targetrRrity">要计算到的稀有度</param>
        /// <param name="dic"></param>
        /// <returns>
        /// false: 计算过程中出现找不到某种材料
        /// true: 计算过程正常
        /// </returns>
        private bool CalSynthesis(string name, int num, int targetrRrity, Dictionary<string, int> dic)
        {
            ResourceInfo? resourceInfo;
            try { resourceInfo = ResourceDictionary[name]; }
            catch(KeyNotFoundException) { return false; }
            if (resourceInfo.Rarity <= targetrRrity)
            {
                if (!dic.ContainsKey(resourceInfo.Name))
                    dic.Add(resourceInfo.Name, num);
                else
                    dic[resourceInfo.Name] = dic[resourceInfo.Name] + num;
                return true;
            }
            if (resourceInfo.Rarity == targetrRrity + 1)
            {
                if (resourceInfo.synthesisItem.Length == 0) return true;
                foreach (var si in resourceInfo.synthesisItem)
                {
                    if (!dic.ContainsKey(si.Name))
                        dic.Add(si.Name, si.Count * num);
                    else
                        dic[si.Name] = dic[si.Name] + si.Count * num;
                }
                return true;
            }
            if (resourceInfo.Rarity > targetrRrity + 1)
            {
                foreach (var si in resourceInfo.synthesisItem)
                    if (!CalSynthesis(si.Name, si.Count * num, targetrRrity, dic))
                        return false;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 计算一个技能升一级的材料
        /// </summary>
        /// <param name="skillResource"></param>
        /// <param name="targetrRrity"></param>
        /// <returns></returns>
        public Dictionary<string, int> CalSkillResource(SkillResource skillResource, int targetrRrity)
        {
            Dictionary<string, int> dic = new();
            foreach (var r in skillResource.resources)
            {
                DicSelfOperator(dic, CalSynthesis(r.name, r.count, targetrRrity), (x, y) => x + y);
            }
            return dic;
        }
        /// <summary>
        /// 计算一个技能专三材料
        /// </summary>
        /// <param name="skillLevel"></param>
        /// <param name="targetrRrity"></param>
        /// <returns></returns>
        public Dictionary<string, int> CalSkillLevel(SkillLevel skillLevel, int targetrRrity)
        {
            Dictionary<string, int> dic = new();
            foreach (var r in skillLevel.skillResources)
            {
                DicSelfOperator(dic, CalSkillResource(r, targetrRrity), (x, y) => x + y);
            }
            return dic;
        }
        public Dictionary<string, int> SkillLevelToDic(SkillLevel skillLevel)
        {
            Dictionary<string, int> dic = new();
            foreach (var r in skillLevel.skillResources)
            {
                foreach (var rr in r.resources)
                {
                    DicSelfOperator(dic, rr.name, rr.count, (x, y) => x + y);
                }
            }
            return dic;
        }
        public void LoadDepot(string path)
        {
            if (ResourceDictionary == null) { throw new ArgumentNullException("ResourceDictionary is null"); }
            depot = new();
            string json = "";
            try
            {
                json = File.ReadAllText(path);
            }
            catch (FileNotFoundException) { return; }
            var d = JsonSerializer.Deserialize<Depot>(json);
            foreach (var item in d.items)
                if (ResourceDictionary.ContainsKey(item.name))
                    depot.Add(item.name, item.have);
            return;
        }
        /// <summary>
        /// 计算直接欠缺的材料
        /// </summary>
        /// <param name="skillData"></param>
        /// <returns>直接欠缺的材料</returns>
        public Dictionary<string, int> CalLackDirectDepot(Dictionary<string, int> skillData)
        {
            if (depot == null) return new();
            return DicOperator(depot, skillData, (x, y) => x - y);
        }
        /// <summary>
        /// 直接欠缺的材料排序
        /// </summary>
        /// <param name="LackDirectDepot"></param>
        /// <returns></returns>
        public List<KeyValuePair<string, int>> CalNeedSynthesis(Dictionary<string, int> LackDirectDepot)
        {
            List<KeyValuePair<string, int>> list = new();
            foreach (var item in LackDirectDepot)
                if (item.Value < 0)
                    list.Add(new(item.Key, -item.Value));
            list.Sort(new KeyValuePair_string_int_Comp(ResourceDictionary));
            return list;
        }
        /// <summary>
        /// 计算总体欠缺的2级材料
        /// </summary>
        /// <param name="skillData"></param>
        /// <returns>(总计需要合成的材料，总体欠缺的2级材料)</returns>
        public (List<KeyValuePair<string, int>>, Dictionary<string, int>)
            CalLack_Rarity2(Dictionary<string, int> skillData)
        {
            if (depot == null) return new();
            Dictionary<string, int> LackDirectDepot = DicOperator(depot, skillData, (x, y) => x - y);
            Dictionary<string, int> dicSynthesisPath = new();
            CalLack_Rarity2(LackDirectDepot, dicSynthesisPath, 4, 2);
            Dictionary<string, int> dicLack2 = new();
            foreach (var item in LackDirectDepot)
                if (item.Value < 0)
                    dicLack2.Add(item.Key, -item.Value);
            List<KeyValuePair<string, int>> list = dicSynthesisPath.ToList();
            list.Sort(new KeyValuePair_string_int_Comp(ResourceDictionary));

            if (dicLack2.Count == 0) dicLack2.Add("无", 0);
            return (list, dicLack2);
        }
        private void CalLack_Rarity2(Dictionary<string, int> LackDirectDepot,
            Dictionary<string, int> dicSynthesisPath,
            int currentRrity, int targetrRrity)
        {
            if (currentRrity == targetrRrity) return;
            var keys = new List<string>(LackDirectDepot.Keys);
            for(int i = 0; i < keys.Count; i++)
            {
                if (LackDirectDepot[keys[i]] < 0 & ResourceDictionary[keys[i]].Rarity == currentRrity)
                {
                    DicSelfOperator(LackDirectDepot, CalSynthesis(keys[i], -LackDirectDepot[keys[i]], currentRrity - 1),
                        (x, y) => x - y);
                    if (!dicSynthesisPath.ContainsKey(keys[i]))
                        dicSynthesisPath.Add(keys[i], -LackDirectDepot[keys[i]]);
                    else
                        dicSynthesisPath[keys[i]] = dicSynthesisPath[keys[i]] - LackDirectDepot[keys[i]];
                    LackDirectDepot.Remove(keys[i]);
                }
            }
            CalLack_Rarity2(LackDirectDepot, dicSynthesisPath, currentRrity - 1, targetrRrity);
        }
    }
    public class KeyValuePair_string_int_Comp : IComparer<KeyValuePair<string, int>>
    {
        private Dictionary<string, ResourceInfo>? ResourceDictionary;
        public KeyValuePair_string_int_Comp(Dictionary<string, ResourceInfo>? ResourceDictionary)
        {
            this.ResourceDictionary = ResourceDictionary;
        }
        public int Compare(KeyValuePair<string, int> x, KeyValuePair<string, int> y)
        {
            if (!ResourceDictionary.ContainsKey(x.Key) | !ResourceDictionary.ContainsKey(y.Key))
                throw new ArgumentException("There is no such a key in ResourceDictionary");
            return ResourceDictionary[x.Key].Rarity - ResourceDictionary[y.Key].Rarity;
        }
    }
}
