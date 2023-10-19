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
using System.Windows.Markup;
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
        static internal ProcData? Pc = null;
        static internal List<string>? OperatorList = null;
        static internal OperatorSkill? OperatorSkillData;
        const string depot_res_Path = @".\Data\depot_res.json";
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                Pc = new ProcData(JsonSerializer.Deserialize<ResourceInfoCollection>
                    (File.ReadAllText(GetDataFromWiki.ResourceDataPath)));
                Pc.LoadDepot(depot_res_Path);
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
            SkillLevel? data;
            try { data = OperatorSkillData?.skills[Skill_ComboBox.SelectedIndex]; }
            catch (IndexOutOfRangeException) { ShowData(null); return; }
            var res = await Task.Run(() => Proc_Inquare_Async(data));
            ShowData(res);
        }
        private (List<KeyValuePair<string, int>>, Dictionary<string, int>)? Proc_Inquare_Async(SkillLevel? data)
        {
            if (Pc == null) return null;
            return Pc.CalLack_Rarity2(Pc.SkillLevelToDic(data));
        }
        private async void Skill_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SkillLevel? data;
            try { data = OperatorSkillData?.skills[Skill_ComboBox.SelectedIndex]; }
            catch (IndexOutOfRangeException) { ShowData(null); return; }
            var res = await Task.Run(() => Proc_Inquare_Async(data));
            ShowData(res);
        }

        private async void Update_Button_Click(object sender, RoutedEventArgs e)
        {
            Pc = new ProcData(await GetDataFromWiki.GetResourceDataAsync());
            OperatorList = (await GetDataFromWiki.GetOperatorListAsync()).Names.ToList();
            OperatorNmae_ComboBox.ItemsSource = OperatorList;
            MessageBox.Show("更新完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowData((List<KeyValuePair<string, int>>, Dictionary<string, int>)? res)
        {
            if (res == null)
            {
                DataGrid_syn.ItemsSource = null;
                DataGrid_lack.ItemsSource = null;
                return;
            }
            DataGrid_syn.ItemsSource = res.Value.Item1;
            DataGrid_lack.ItemsSource = res.Value.Item2;
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
