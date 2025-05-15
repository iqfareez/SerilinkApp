using System.Collections.Generic;

namespace SerilinkApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// The available baud rates for the serial port.
    /// </summary>
    public IEnumerable<string> BaudRates { get; } = new[]
    {
        "300", "600", "1200", "2400", "4800", "9600", "14400",
        "19200", "28800", "38400", "57600", "115200"
    };

    /// <summary>
    /// Default baud rate for the serial port.
    /// </summary>
    private string _selectedBaudRate = "9600";

    public string SelectedBaudRate
    {
        get => _selectedBaudRate;
        set => SetProperty(ref _selectedBaudRate, value);
    }
}