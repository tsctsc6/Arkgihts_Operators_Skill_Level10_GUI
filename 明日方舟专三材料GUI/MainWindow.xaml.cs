using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using GetArknightsData;

namespace 明日方舟专三材料GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static internal Dictionary<string, ResourceInfo>? ResourceDictionary = null;
        static internal List<string>? OperatorList = null;
        static internal OperatorSkill? OperatorSkillData;
        const string depot_res_Path = @".\Data\depot_res.json";
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                ResourceDictionary = ProcData.GetResourceDictionary(
                    JsonSerializer.Deserialize<ResourceInfoCollection>
                    (File.ReadAllText(GetDataFromWiki.ResourceDataPath)));
                OperatorList = JsonSerializer.Deserialize<OperatorCollection>
                    (File.ReadAllText(GetDataFromWiki.OperatorListPath))?.Names.ToList();
                Label_Time.Content = File.GetLastWriteTime(depot_res_Path).ToString();
                OperatorNmae_ComboBox.ItemsSource = OperatorList;
            }
            catch (Exception e) { MessageBox.Show(e.Message, "发生错误", MessageBoxButton.OK, MessageBoxImage.Error);}
        }

        private void OperatorNmae_ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            OperatorNmae_ComboBox.SelectedIndex = -1;
            if (!string.IsNullOrEmpty(OperatorNmae_ComboBox.Text))
            {
                List<string> searchList = new List<string>();
                searchList = OperatorList.FindAll(delegate (string s) { return s.Contains(OperatorNmae_ComboBox.Text.Trim()); });
                OperatorNmae_ComboBox.ItemsSource = searchList;
            }
            else OperatorNmae_ComboBox.ItemsSource = OperatorList;
            OperatorNmae_ComboBox.IsDropDownOpen = true;
        }

        private async void Inquare_Button_Click(object sender, RoutedEventArgs e)
        {
            OperatorSkillData = await GetDataFromWiki.GetSpecializationDataAsync(OperatorNmae_ComboBox.Text);
            await Task.Run(() => Proc_Inquare_Async(OperatorSkillData.skills[Skill_ComboBox.SelectedIndex]));
            ShowData();
        }
        private void Proc_Inquare_Async(SkillLevel? data)
        {
            
        }
        private void Skill_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowData();
        }

        private async void Update_Button_Click(object sender, RoutedEventArgs e)
        {
            ResourceDictionary = ProcData.GetResourceDictionary(await GetDataFromWiki.GetResourceDataAsync());
            OperatorList = (await GetDataFromWiki.GetOperatorListAsync()).Names.ToList();
            OperatorNmae_ComboBox.ItemsSource = OperatorList;
            MessageBox.Show("更新完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowData()
        {
            int SkillIndex = 0;
            Graph.Plot.Clear();
            Graph.Render(true);
            DataGrid_syn.ItemsSource = null;
            DataGrid_lack.ItemsSource = null;
            SkillIndex = Skill_ComboBox.SelectedIndex;
            if (OperatorSkillData?.skills[SkillIndex] == null) return;
            Dictionary<string, int> res = OperatorSkillData.skills[SkillIndex].Statistics();
            double[] MatNum = new double[res.Count];
            int i = 0;
            foreach (var kv in res)
            {
                MatNum[i] = kv.Value;
                i++;
            }
            var bar = Graph.Plot.AddBar(MatNum);
            bar.ShowValuesAboveBars = true;
            bar.Font.Bold = true;
            bar.Font.Size = 18;

            List<KeyValuePair<string, int>>? res_syn;
            List<KeyValuePair<string, int>>? res_lack;
            myDepot.Cal(skillData, SkillIndex, out res_syn, out res_lack);
            List<string> syn_name = new List<string>();
            List<int> syn_num = new List<int>();

            Graph.Plot.SetAxisLimits(yMin: 0);
            Graph.Render(true);
            DataGrid_syn.ItemsSource = res_syn;
            DataGrid_lack.ItemsSource = res_lack;
        }

        private void OpenFile_Button_Click(object sender, RoutedEventArgs e)
        {
            Process subp = new Process();
            subp.StartInfo.FileName = @"C:\WINDOWS\system32\NOTEPAD.EXE";
            subp.StartInfo.Arguments = depot_res_Path;
            subp.Start();
            subp.WaitForExit();
            Label_Time.Content = File.GetLastWriteTime(depot_res_Path).ToString();
        }
    }
}
