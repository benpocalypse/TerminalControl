using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Renci.SshNet;
using System;
using System.Text;
using System.Threading.Tasks;

namespace TerminalControl.Controls;

public partial class TerminalControl : UserControl
{
    private SshClient? _sshClient;
    private ShellStream? _shellStream;
    private bool _isConnected;
    private readonly byte[] _buffer = new byte[4096];
    private string _currentOutput = string.Empty;

    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<string>? OutputReceived;
    
    public bool IsConnected => _isConnected;

    public TerminalControl()
    {
        InitializeComponent();
        XTerm.KeyDown += OnKeyDown;
    }

    public async Task<bool> ConnectSSH(string host, int port, string username, string password)
    {
        if (_isConnected) return true;

        try
        {
            await Task.Run(() =>
            {
                _sshClient = new SshClient(host, port, username, password);
                _sshClient.Connect();

                _shellStream = _sshClient.CreateShellStream("xterm-256color", 80, 24, 800, 600, 1024);
                _isConnected = true;

                _ = Task.Run(ReadShellStream);
            });

            Connected?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception)
        {
            _isConnected = false;
            return false;
        }
    }

    public async Task Disconnect()
    {
        try
        {
            _shellStream?.Dispose();
            _sshClient?.Disconnect();
            _sshClient?.Dispose();
        }
        catch { }

        _isConnected = false;
        _shellStream = null;
        _sshClient = null;
        Disconnected?.Invoke(this, "Disconnected");
    }

    private async Task ReadShellStream()
    {
        if (_shellStream == null) return;

        while (_isConnected && _shellStream != null && _shellStream.CanRead)
        {
            try
            {
                if (_shellStream.DataAvailable)
                {
                    int bytesRead = _shellStream.Read(_buffer, 0, _buffer.Length);
                    if (bytesRead > 0)
                    {
                        var data = new byte[bytesRead];
                        Array.Copy(_buffer, data, bytesRead);
                        string output = Encoding.UTF8.GetString(data);
                        
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _currentOutput += output;
                            OutputReceived?.Invoke(this, output);
                            // Display output in a simple way for now
                            // In a real implementation, you'd feed this to the terminal
                            System.Diagnostics.Debug.WriteLine($"SSH Output: {output}");
                        });
                    }
                }
                else
                {
                    await Task.Delay(10);
                }
            }
            catch
            {
                await Disconnect();
                break;
            }
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isConnected || _shellStream == null || !_shellStream.CanWrite) return;

        try
        {
            byte[]? bytes = null;

            switch (e.Key)
            {
                case Key.Enter: bytes = Encoding.UTF8.GetBytes("\r"); break;
                case Key.Back: bytes = new byte[] { 0x7f }; break;
                case Key.Up: bytes = Encoding.UTF8.GetBytes("\x1b[A"); break;
                case Key.Down: bytes = Encoding.UTF8.GetBytes("\x1b[B"); break;
                case Key.Right: bytes = Encoding.UTF8.GetBytes("\x1b[C"); break;
                case Key.Left: bytes = Encoding.UTF8.GetBytes("\x1b[D"); break;
                case Key.Tab: bytes = Encoding.UTF8.GetBytes("\t"); break;
                case Key.Escape: bytes = new byte[] { 0x1b }; break;
                case Key.Home: bytes = Encoding.UTF8.GetBytes("\x1b[H"); break;
                case Key.End: bytes = Encoding.UTF8.GetBytes("\x1b[F"); break;
                case Key.PageUp: bytes = Encoding.UTF8.GetBytes("\x1b[5~"); break;
                case Key.PageDown: bytes = Encoding.UTF8.GetBytes("\x1b[6~"); break;
                case Key.Delete: bytes = Encoding.UTF8.GetBytes("\x1b[3~"); break;
                case Key.Insert: bytes = Encoding.UTF8.GetBytes("\x1b[2~"); break;
            }

            // Handle function keys (F1-F12)
            if (bytes == null && e.Key >= Key.F1 && e.Key <= Key.F12)
            {
                var fKeyMap = new System.Collections.Generic.Dictionary<Key, string>
                {
                    { Key.F1, "\x1bOP" },
                    { Key.F2, "\x1bOQ" },
                    { Key.F3, "\x1bOR" },
                    { Key.F4, "\x1bOS" },
                    { Key.F5, "\x1b[15~" },
                    { Key.F6, "\x1b[17~" },
                    { Key.F7, "\x1b[18~" },
                    { Key.F8, "\x1b[19~" },
                    { Key.F9, "\x1b[20~" },
                    { Key.F10, "\x1b[21~" },
                    { Key.F11, "\x1b[23~" },
                    { Key.F12, "\x1b[24~" }
                };
                if (fKeyMap.TryGetValue(e.Key, out var fKeySeq))
                {
                    bytes = Encoding.UTF8.GetBytes(fKeySeq);
                }
            }

            // Handle letters with Shift
            if (bytes == null && e.Key >= Key.A && e.Key <= Key.Z)
            {
                char c = e.Key.ToString().ToLower()[0];
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) c = char.ToUpper(c);
                bytes = Encoding.UTF8.GetBytes(c.ToString());
            }
            else if (bytes == null && e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                char c = e.Key.ToString()[1];
                bytes = Encoding.UTF8.GetBytes(c.ToString());
            }
            else if (bytes == null && e.Key == Key.Space)
            {
                bytes = Encoding.UTF8.GetBytes(" ");
            }

            if (bytes != null)
            {
                _shellStream.Write(bytes, 0, bytes.Length);
                _shellStream.Flush();
            }
        }
        catch { }
    }

    // Allow clearing the terminal display
    public void Clear()
    {
        _currentOutput = string.Empty;
        // In a real implementation, you'd clear the terminal display here
    }
}
