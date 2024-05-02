namespace GetArknightsData
{
    public class Resource
    {
        public string name { get; set; } = "";
        public int count { get; set; } = 0;
        public Resource() {; }
        public Resource(string name, string count)
        {
            this.name = name;
            this.count = int.Parse(count);
        }
    }
    public class SkillResource
    {
        public Resource[] resources { get; set; } = new Resource[0];
        public SkillResource() {; }
    }
    public class SkillLevel
    {
        public SkillResource[] skillResources { get; set; } = new SkillResource[0];
        public SkillLevel() {; }
    }
    public class OperatorSkill
    {
        public string name { get; set; } = "";
        public SkillLevel[] skills { get; set; } = new SkillLevel[0];
        public OperatorSkill() {; }
        public OperatorSkill(string name) { this.name = name; }
    }
}
