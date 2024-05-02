namespace GetArknightsData
{
    public class DepotItem
    {
        public int have { get; set; } = 0;
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public DepotItem() {; }
    }
    public class Depot
    {
        public string @type { get; set; } = "";
        public DepotItem[] items { get; set; } = new DepotItem[0];
        public Depot() {; }
    }
}
