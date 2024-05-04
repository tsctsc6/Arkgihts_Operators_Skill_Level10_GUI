using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetArknightsData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace 明日方舟专三材料GUI
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        static internal ProcData? pc = null;
        static internal string[]? operatorArray = null;
        static internal OperatorSkill? operatorSkillData;
        const string depot_res_Path = @".\Data\depot_res.json";

        [ObservableProperty]
        string lastWriteTime = string.Empty;
        [ObservableProperty]
        string stateText = "Done";
        [ObservableProperty]
        int skillComboBox_SelectedIndex = 0;
        [ObservableProperty]
        string comboboxText = string.Empty;

        public ICollectionView OperatorCollectionView { get; private set; }
        //List<KeyValuePair<string, int>> synList;
        public ICollectionView SynCollectionView { get; private set; }
        //List<KeyValuePair<string, int>> lackList;
        public ICollectionView LackCollectionView { get; private set; }

        public MainWindowViewModel()
        {
            try
            {
                pc = new ProcData(JsonSerializer.Deserialize<ResourceInfoCollection>
                    (File.ReadAllText(GetDataFromWiki.ResourceDataPath)));
                pc.LoadDepot(depot_res_Path);
                operatorArray = JsonSerializer.Deserialize<OperatorCollection>
                    (File.ReadAllText(GetDataFromWiki.OperatorListPath))?.Names;
                LastWriteTime = File.GetLastWriteTime(depot_res_Path).ToString();
                //!! OperatorNmae_ComboBox.ItemsSource = OperatorArray;
            }
            catch (Exception e) { MessageBox.Show(e.Message, "发生错误", MessageBoxButton.OK,
                MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly); }

            OperatorCollectionView = CollectionViewSource.GetDefaultView(operatorArray);
            OperatorCollectionView.Filter = (o) =>
            {
                if (o is not string s) return false;
                return s.Contains(ComboboxText);
            };
            SynCollectionView = CollectionViewSource.GetDefaultView(new List<KeyValuePair<string, int>>());
            LackCollectionView = CollectionViewSource.GetDefaultView(new List<KeyValuePair<string, int>>());
        }

        [RelayCommand]
        private async Task InquareAsync()
        {
            StateText = "Running";
            operatorSkillData = await GetDataFromWiki.GetSpecializationDataAsync(ComboboxText);
            SkillLevel? data;
            try { data = operatorSkillData?.skills[SkillComboBox_SelectedIndex]; }
            catch (IndexOutOfRangeException) { ShowData(null); return; }
            var res = await Task.Run(() => Proc_Inquare_Async(data));
            ShowData(res);
        }

        private (List<KeyValuePair<string, int>>, Dictionary<string, int>)? Proc_Inquare_Async(SkillLevel? data)
        {
            if (pc == null) return null;
            if (data == null) return null;
            return pc.CalLack_Rarity2(pc.SkillLevelToDic(data));
        }

        private void ShowData((List<KeyValuePair<string, int>>, Dictionary<string, int>)? res)
        {
            if (res == null)
            {
                SynCollectionView = CollectionViewSource.GetDefaultView(new List<KeyValuePair<string, int>>());
                LackCollectionView = CollectionViewSource.GetDefaultView(new List<KeyValuePair<string, int>>());
            }
            else
            {
                SynCollectionView = CollectionViewSource.GetDefaultView(res.Value.Item1);
                LackCollectionView = CollectionViewSource.GetDefaultView(res.Value.Item2);
            }
            StateText = "Done";
        }

        [RelayCommand]
        private async Task UpdateAsync()
        {
            StateText = "Running";
            pc = new ProcData(await GetDataFromWiki.GetResourceDataAsync());
            operatorArray = [.. (await GetDataFromWiki.GetOperatorListAsync()).Names];
            OperatorCollectionView.Refresh();
            MessageBox.Show("更新完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            StateText = "Done";
        }

        [RelayCommand]
        private async Task OpenFileAsync()
        {
            if (!File.Exists(depot_res_Path))
            {
                if (!Directory.Exists(@".\Data")) Directory.CreateDirectory(@".\Data");
                File.WriteAllText(depot_res_Path, "{\"@type\":\"@penguin-statistics/depot\",\"items\":[]}");
            }
            Process subp = new Process();
            subp.StartInfo.FileName = @"C:\WINDOWS\system32\NOTEPAD.EXE";
            subp.StartInfo.Arguments = depot_res_Path;
            subp.Start();
            await subp.WaitForExitAsync();
            LastWriteTime = File.GetLastWriteTime(depot_res_Path).ToString();
            pc?.LoadDepot(depot_res_Path);
        }
    }
}
