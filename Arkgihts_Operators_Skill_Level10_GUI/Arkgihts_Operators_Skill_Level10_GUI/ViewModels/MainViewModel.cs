using CommunityToolkit.Mvvm.ComponentModel;

namespace Arkgihts_Operators_Skill_Level10_GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Welcome to Avalonia!";
}