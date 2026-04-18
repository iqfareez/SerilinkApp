using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerilinkApp.Models;

namespace SerilinkApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly SerialPortService _serialPortService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availablePorts = new();

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private string _selectedBaudRate = "9600";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectButtonText = "Connect";

    [ObservableProperty]
    private bool _clearAfterSend;

    [ObservableProperty]
    private bool _showSentMessages = true;

    [ObservableProperty]
    private bool _showTimestamp;

    [ObservableProperty]
    private int _selectedLineEndingIndex = 1; // Default to NewLine

    /// <summary>
    /// The available baud rates for the serial port.
    /// </summary>
    public IEnumerable<string> BaudRates { get; } = new[]
    {
        "300", "600", "1200", "2400", "4800", "9600", "14400",
        "19200", "28800", "38400", "57600", "115200"
    };

    public MainWindowViewModel()
    {
        _serialPortService = new SerialPortService();
        _serialPortService.DataReceived += OnDataReceived;
        _serialPortService.ErrorOccurred += OnErrorOccurred;
        _serialPortService.ConnectionStateChanged += OnConnectionStateChanged;

        RefreshPorts();
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        var ports = _serialPortService.GetAvailablePorts();
        AvailablePorts.Clear();

        foreach (var port in ports)
        {
            AvailablePorts.Add(port);
        }

        if (AvailablePorts.Any() && SelectedPort == null)
        {
            SelectedPort = AvailablePorts.First();
        }
    }

    [RelayCommand]
    private void Connect()
    {
        if (IsConnected)
        {
            _serialPortService.Disconnect();
        }
        else
        {
            if (string.IsNullOrEmpty(SelectedPort))
            {
                AppendOutput("Please select a port");
                return;
            }

            if (!int.TryParse(SelectedBaudRate, out int baudRate))
            {
                AppendOutput("Invalid baud rate");
                return;
            }

            if (_serialPortService.Connect(SelectedPort, baudRate))
            {
                AppendOutput($"Connected to {SelectedPort} at {baudRate} baud");
            }
        }
    }

    [RelayCommand]
    private void Send()
    {
        if (string.IsNullOrEmpty(InputText))
        {
            return;
        }

        if (!IsConnected)
        {
            AppendOutput("Not connected to any port");
            return;
        }

        string dataToSend = InputText + GetLineEnding();

        if (_serialPortService.SendData(dataToSend))
        {
            if (ShowSentMessages)
            {
                AppendOutput($">> {InputText}", isOutgoing: true);
            }

            if (ClearAfterSend)
            {
                InputText = string.Empty;
            }
        }
    }

    [RelayCommand]
    private void ClearOutput()
    {
        OutputText = string.Empty;
    }

    private string GetLineEnding()
    {
        return SelectedLineEndingIndex switch
        {
            0 => string.Empty,           // None
            1 => "\n",                   // NewLine
            2 => "\r",                   // Carriage Return
            3 => "\r\n",                 // NL & CR
            _ => string.Empty
        };
    }

    private void OnDataReceived(object? sender, string data)
    {
        AppendOutput(data, isOutgoing: false);
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        AppendOutput($"[ERROR] {error}");
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        IsConnected = isConnected;
        ConnectButtonText = isConnected ? "Disconnect" : "Connect";
    }

    private void AppendOutput(string text, bool isOutgoing = false)
    {
        string timestamp = ShowTimestamp ? $"[{DateTime.Now:HH:mm:ss.fff}] " : string.Empty;
        OutputText += timestamp + text;
        
        // Add newline only if the text doesn't already end with one
        if (!text.EndsWith("\n") && !text.EndsWith("\r\n"))
        {
            OutputText += Environment.NewLine;
        }
    }

    public void Dispose()
    {
        _serialPortService.DataReceived -= OnDataReceived;
        _serialPortService.ErrorOccurred -= OnErrorOccurred;
        _serialPortService.ConnectionStateChanged -= OnConnectionStateChanged;
        _serialPortService.Dispose();
    }
    
    /// <summary>
    /// Exit the application.
    /// </summary>
    [RelayCommand]
    public void ExitApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
