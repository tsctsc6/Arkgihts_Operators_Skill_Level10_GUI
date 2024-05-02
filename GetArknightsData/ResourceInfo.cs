namespace GetArknightsData
{
    public class SynthesisItem
    {
        public string Name { get; set; } = "";
        public int Count { get; set; } = 0;
        public SynthesisItem() {; }
        public SynthesisItem(string name, int count)
        {
            this.Name = name;
            this.Count = count;
        }
        public SynthesisItem(string name, string count)
        {
            this.Name = name;
            this.Count = int.Parse(count);
        }
    }
    public class ResourceInfo
    {
        public string name { get; set; } = "";
        public int rarity { get; set; } = 0;
        public SynthesisItem[] synthesisItem { get; set; } = new SynthesisItem[0];
        public ResourceInfo() {; }
        public ResourceInfo(string name)
        {
            this.name = name;
        }
        public ResourceInfo(string name, int rarity)
        {
            this.name = name;
            this.rarity = rarity;
        }
    }
    public class ResourceInfoCollection
    {
        public ResourceInfo[] resources { get; set; } = new ResourceInfo[0];
    }
}