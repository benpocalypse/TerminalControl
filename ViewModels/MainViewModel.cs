using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TerminalControl.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isConnected;

    public MainWindowViewModel()
    {
        ConnectCommand = new RelayCommand(OnConnect);
        DisconnectCommand = new RelayCommand(OnDisconnect, () => IsConnected);
    }

    public IRelayCommand ConnectCommand { get; }
    public IRelayCommand DisconnectCommand { get; }

    private void OnConnect()
    {
        Status = "Connecting...";
    }

    private void OnDisconnect()
    {
        Status = "Disconnected";
        IsConnected = false;
    }
}
