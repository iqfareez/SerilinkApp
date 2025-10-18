using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SerilinkApp.Models;

/// <summary>
/// The Serial Port Service.
/// </summary>
public class SerialPortService : IDisposable
{
    private SerialPort? _serialPort;
    private CancellationTokenSource? _cancellationTokenSource;
    
    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<bool>? ConnectionStateChanged;
    
    public bool IsConnected => _serialPort?.IsOpen ?? false;
    
    /// <summary>
    /// Gets all available serial port names
    /// </summary>
    public string[] GetAvailablePorts()
    {
        try
        {
            return SerialPort.GetPortNames().OrderBy(p => p).ToArray();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error getting ports: {ex.Message}");
            return Array.Empty<string>();
        }
    }
    
    /// <summary>
    /// Opens a serial port connection
    /// </summary>
    public bool Connect(string portName, int baudRate)
    {
        try
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                Disconnect();
            }
            
            _serialPort = new SerialPort(portName, baudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            
            _serialPort.Open();
            
            // Start reading data asynchronously
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ReadDataAsync(_cancellationTokenSource.Token));
            
            ConnectionStateChanged?.Invoke(this, true);
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Connection failed: {ex.Message}");
            _serialPort?.Dispose();
            _serialPort = null;
            return false;
        }
    }
    
    /// <summary>
    /// Closes the serial port connection
    /// </summary>
    public void Disconnect()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            
            _serialPort?.Dispose();
            _serialPort = null;
            
            ConnectionStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Disconnection error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Sends data through the serial port
    /// </summary>
    public bool SendData(string data)
    {
        try
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                ErrorOccurred?.Invoke(this, "Port is not open");
                return false;
            }
            
            _serialPort.Write(data);
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Send failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Reads data from the serial port asynchronously
    /// </summary>
    private async Task ReadDataAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                if (_serialPort.BytesToRead > 0)
                {
                    string data = _serialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        DataReceived?.Invoke(this, data);
                    }
                }
                
                // Small delay to prevent CPU hogging (busy-wait) while still being responsive enough
                // to catch incoming data. Also allows quick cancellation response.
                await Task.Delay(10, cancellationToken);
            }
            catch (TimeoutException)
            {
                // Normal timeout, continue reading
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, exit gracefully
                break;
            }
            catch (Exception ex)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    ErrorOccurred?.Invoke(this, $"Read error: {ex.Message}");
                }
                break;
            }
        }
    }
    
    public void Dispose()
    {
        Disconnect();
        _cancellationTokenSource?.Dispose();
    }
}
