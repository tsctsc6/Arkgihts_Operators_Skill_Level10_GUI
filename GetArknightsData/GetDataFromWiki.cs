using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace GetArknightsData
{
    public class GetDataFromWiki
    {
        static readonly string host = "https://prts.wiki/w/";
        static readonly HttpClient client = new();
        public const string ResourceDataPath = @".\Data\材料名单.json";
        public const string OperatorListPath = @".\Data\干员名单.json";
        static public void Init()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36 Edg/116.0.1938.76");
        }
        static async public Task<ResourceInfoCollection> GetResourceDataAsync()
        {
            string htmlText = await GetHtmlText("道具一览");
            string[][] resourceNames = ProcHTML_GetResourceData(htmlText);
            //Console.WriteLine();
            int i = 2;
            List<ResourceInfo> resources = new();
            do
            {
                int j = 0;
                string[] resourceName = resourceNames[i - 2];
                string? html = null;
                do
                {
                    Task<string>? t = null;
                    if (j == 0) html = await GetHtmlText(resourceName[j]);
                    if (j < resourceName.Length - 1) t = GetHtmlText(resourceName[j + 1]);
                    var res = ProcHTML_GetResourceData2(html, i);
                    if (t != null) html = await t;
                    resources.Add(res);
                    j++;
                } while (j < resourceName.Length);
                i++;
            } while (i < 5);
            /*
            foreach (var rns in resourceNames)
            {
                var htmls = rns.Select(rn => GetHtmlText(rn)).ToArray();
                await Task.WhenAll(htmls);
                var res = htmls.Select(html => ProcHTML_GetResourceData2(html.Result, i)).ToList();
                resources = resources.Concat(res).ToList();          

                i++;
            }
            */
            ResourceInfoCollection rc = new ResourceInfoCollection();
            rc.resources = resources.ToArray();
            if (!Directory.Exists(@".\Data")) Directory.CreateDirectory(@".\Data");
            string jsonString = JsonSerializer.Serialize(rc, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            });
            await File.WriteAllTextAsync(ResourceDataPath, jsonString);
            return rc;
        }
        /// <summary>
        /// 获取网页html
        /// </summary>
        /// <param name="subUrl">prts.wiki网站的后半url</param>
        /// <returns>网页html</returns>
        static async Task<string> GetHtmlText(string subUrl)
        {
            var request = new HttpRequestMessage();
            request.Method = new HttpMethod("GET");
            request.RequestUri = new Uri($"{host}{subUrl}");
            var response = await client.SendAsync(request);
            var contentStream = await response.Content.ReadAsStreamAsync();
            StreamReader reader = new StreamReader(contentStream);
            string htmlText = reader.ReadToEnd();
            //Console.WriteLine(htmlText);
            //File.WriteAllText(@$".\{subUrl}.html", htmlText);
            return htmlText;
        }
        /// <summary>
        /// 处理“道具一览”
        /// </summary>
        /// <param name="htmlText">网页html</param>
        /// <returns>全游戏的干员专精材料名称</returns>
        static string[][] ProcHTML_GetResourceData(in string htmlText)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlText);
            //data-category属性以'分类:道具, 分类:材料'开头，并且
            //data-obtain_approach属性是'常规关卡掉落'，并且
            //data-rarity属性是'2'
            //的节点
            HtmlNodeCollection nodes_2 = doc.DocumentNode.SelectNodes(
                "//div[starts-with(@data-category,'分类:道具, 分类:材料') and " +
                "@data-obtain_approach='常规关卡掉落' and " +
                "@data-rarity='2']");
            HtmlNodeCollection nodes_3 = doc.DocumentNode.SelectNodes(
                "//div[@data-category='分类:道具, 分类:材料, 分类:加工站产物' and " +
                "@data-rarity='3']");
            HtmlNodeCollection nodes_4 = doc.DocumentNode.SelectNodes(
                "//div[@data-category='分类:道具, 分类:材料, 分类:加工站产物' and " +
                "@data-rarity='4']");
            /*
            var nodes = nodes_2.Union(nodes_3).Union(nodes_4);
            foreach (var node in nodes)
            {
                Console.WriteLine(node.Attributes["data-name"].Value);
            }
            Console.WriteLine($"·总数：{nodes_2.Count + nodes_3.Count + nodes_4.Count}");
            */
            return new string[3][]
            {
                nodes_2.Select(n => n.Attributes["data-name"].Value).ToArray(),
                nodes_3.Select(n => n.Attributes["data-name"].Value).ToArray(),
                nodes_4.Select(n => n.Attributes["data-name"].Value).ToArray()
            };
        }
        /// <summary>
        /// 获取道具合成公式
        /// </summary>
        /// <param name="name">道具名称</param>
        /// <param name="htmlText"></param>
        static ResourceInfo ProcHTML_GetResourceData2(in string htmlText, int rarity)
        {
            ResourceInfo resource = new ResourceInfo();
            resource.rarity = rarity;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlText);
            HtmlNode metaNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
            resource.name = metaNode.Attributes["content"].Value;
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//span[@id='加工站']/../following-sibling::table[1]/tbody/tr[2]");
            if (node == null) return resource;
            HtmlNodeCollection nodes = node.SelectNodes("./td");
            List<SynthesisItem> sl = new();
            foreach (var n in nodes)
            {
                HtmlNode n2 = n.SelectSingleNode("./div");
                if (n2 == null) break;
                //Console.WriteLine($"{n2.SelectSingleNode("./a").Attributes["title"].Value}: {n2.InnerText}");
                sl.Add(new SynthesisItem(
                    n2.SelectSingleNode("./a").Attributes["title"].Value,
                    n2.InnerText
                ));
            }
            resource.synthesisItem = sl.ToArray();
            /*
            resource.synthesisItem = nodes.Select(n =>
            {
                HtmlNode n2 = n.SelectSingleNode("./div");
                if (n2 == null) return new SynthesisItem();
                return new SynthesisItem(
                    n2.SelectSingleNode("./a").Attributes["title"].Value,
                    n2.InnerText
                );
            }).ToArray();
            */
            return resource;
        }
        static async public Task<OperatorCollection> GetOperatorListAsync()
        {
            return await ProcHTML_GetOperatorList(await GetHtmlText("干员一览"));
        }
        /// <summary>
        /// 获取干员名单
        /// </summary>
        /// <param name="htmlText"></param>
        static async Task<OperatorCollection> ProcHTML_GetOperatorList(string htmlText)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlText);
            HtmlNode nodes = doc.DocumentNode.SelectSingleNode("//div[@id='filter-data']");
            /*
            int i = 0;
            foreach (var node in nodes.ChildNodes)
            {
                Console.WriteLine(node.Attributes["data-zh"].Value);
                i++;
            }
            Console.WriteLine($"总数：{i}");
            */
            OperatorCollection oc = new OperatorCollection();
            oc.Names = nodes.ChildNodes.Select(n => n.Attributes["data-zh"].Value).ToArray();
            if (!Directory.Exists(@".\Data")) Directory.CreateDirectory(@".\Data");
            string jsonString = JsonSerializer.Serialize(oc, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            });
            await File.WriteAllTextAsync(OperatorListPath, jsonString);
            return oc;
        }
        static async public Task<OperatorSkill> GetSpecializationDataAsync(string name)
        {
            return ProcHtml_GetSpecializationData(await GetHtmlText(name), name);
        }
        static OperatorSkill ProcHtml_GetSpecializationData(in string htmlText, string name)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlText);
            var a = doc.DocumentNode.SelectNodes("//*[@id=\"技能升级材料\"]")[0];
            var table = a.SelectNodes("../following-sibling::table[1]/tbody")[0];
            // ./tr[7]/td[1]/div[1]/a
            // 等级1    技能1     材料1
            OperatorSkill operatorSkill = new(name);
            List<SkillLevel> skills = new();
            for (int i = 1; i < 4; i++)
            {
                if (table.SelectSingleNode($"./tr[7]/td[{i}]/div[1]/a") == null) break;
                SkillLevel skillLevel = new();
                List<SkillResource> SkillResources = new();
                for (int j = 7; j < 10; j++)
                {
                    SkillResource skill = new();
                    List<Resource> resources = new();
                    for (int k = 2; k < 4; k++)
                    {
                        var res = table.SelectNodes($"./tr[{j}]/td[{i}]/div[{k}]/a")[0].Attributes["title"].Value;
                        var num = table.SelectNodes($"./tr[{j}]/td[{i}]/div[{k}]/span")[0].InnerText;
                        resources.Add(new Resource(res, num));
                    }
                    skill.resources = resources.ToArray();
                    SkillResources.Add(skill);
                }
                skillLevel.skillResources = SkillResources.ToArray();
                skills.Add(skillLevel);
            }
            operatorSkill.skills = skills.ToArray();
            return operatorSkill;
        }
    }
}
