using Avalonia.Controls;
using System;

namespace TerminalControl.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // We don't need to wire up Click events here since we're using Commands
        // But we need to handle the terminal events
        Terminal.Connected += OnTerminalConnected;
        Terminal.Disconnected += OnTerminalDisconnected;
        Terminal.OutputReceived += OnTerminalOutput;
    }

    private void OnTerminalConnected(object? sender, EventArgs e)
    {
        StatusText.Text = "Connected";
    }

    private void OnTerminalDisconnected(object? sender, string message)
    {
        StatusText.Text = message;
    }

    private void OnTerminalOutput(object? sender, string output)
    {
        // Handle output if needed
    }
}
