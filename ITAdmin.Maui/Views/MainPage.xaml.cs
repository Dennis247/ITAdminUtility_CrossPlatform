using ITAdmin.Maui.ViewModels;

namespace ITAdmin.Maui.Views;

[QueryProperty(nameof(Username), "username")]
public partial class MainPage : ContentPage
{
    private MainViewModel _viewModel;

    public string Username { get; set; } = string.Empty;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null && !string.IsNullOrEmpty(Username))
        {
            _viewModel.Username = Username;
            _viewModel.StartMonitoringCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.StopMonitoringCommand.Execute(null);
    }
}