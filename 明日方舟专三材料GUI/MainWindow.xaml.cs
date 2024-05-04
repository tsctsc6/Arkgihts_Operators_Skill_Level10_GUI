using System.Windows;
using System.Windows.Controls;

namespace 明日方舟专三材料GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not ComboBox sender2) return;
            if (DataContext is not MainWindowViewModel DataContext2) return;
            sender2.SelectedIndex = -1;
            DataContext2!.OperatorCollectionView.Refresh();
            sender2.IsDropDownOpen = true;
        }

        /*
        private async void Skill_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SkillLevel? data;
            try { data = OperatorSkillData?.skills[Skill_ComboBox.SelectedIndex]; }
            catch (IndexOutOfRangeException) { ShowData(null); return; }
            var res = await Task.Run(() => Proc_Inquare_Async(data));
            ShowData(res);
        }
        */
    }
}
