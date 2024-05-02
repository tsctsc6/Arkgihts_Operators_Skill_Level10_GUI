namespace GetArknightsData
{
    public class SynthesisItem
    {
        public string Name { get; set; } = "";
        public int Count { get; set; } = 0;
        public SynthesisItem() {; }
        public SynthesisItem(string name, int count)
        {
            Name = name;
            Count = count;
        }
        public SynthesisItem(string name, string count)
        {
            Name = name;
            Count = int.Parse(count);
        }
    }
    public class ResourceInfo
    {
        public string Name { get; set; } = "";
        public int Rarity { get; set; } = 0;
        public SynthesisItem[] synthesisItem { get; set; } = new SynthesisItem[0];
        public ResourceInfo() {; }
        public ResourceInfo(string name)
        {
            Name = name;
        }
        public ResourceInfo(string name, int rarity)
        {
            Name = name;
            Rarity = rarity;
        }
    }
    public class ResourceInfoCollection
    {
        public ResourceInfo[] Resources { get; set; } = [];
    }
}