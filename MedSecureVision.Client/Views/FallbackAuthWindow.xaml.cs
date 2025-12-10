using System.Windows;
using System.Windows.Input;
using MedSecureVision.Client.Services;

// Resolve WPF vs WinForms ambiguities
using MessageBox = System.Windows.MessageBox;

namespace MedSecureVision.Client.Views;

/// <summary>
/// Fallback authentication window for PIN, Windows Hello, and Smart Card authentication.
/// Used when face recognition fails or is unavailable.
/// </summary>
public partial class FallbackAuthWindow : Window
{
    private readonly IFallbackAuthService? _fallbackAuthService;

    /// <summary>
    /// Default constructor for design-time and standalone use.
    /// </summary>
    public FallbackAuthWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Constructor with dependency injection for fallback auth service.
    /// </summary>
    public FallbackAuthWindow(IFallbackAuthService fallbackAuthService)
    {
        InitializeComponent();
        _fallbackAuthService = fallbackAuthService;
    }

    /// <summary>
    /// Allow window dragging from any position.
    /// </summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    /// <summary>
    /// Close button handler.
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Authenticate using PIN.
    /// </summary>
    private async void AuthenticateButton_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        if (string.IsNullOrEmpty(pin))
        {
            ShowError("Please enter your PIN");
            return;
        }

        if (pin.Length < 4)
        {
            ShowError("PIN must be at least 4 digits");
            return;
        }

        if (_fallbackAuthService != null)
        {
            var success = await _fallbackAuthService.AuthenticateWithPinAsync(pin);
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("Invalid PIN. Please try again.");
                PinBox.Clear();
                PinBox.Focus();
            }
        }
        else
        {
            // Demo mode - accept any 6-digit PIN
            if (pin.Length == 6 && int.TryParse(pin, out _))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("Please enter a valid 6-digit PIN");
                PinBox.Clear();
                PinBox.Focus();
            }
        }
    }

    /// <summary>
    /// Authenticate using Windows Hello (fingerprint, face, or PIN).
    /// </summary>
    private async void WindowsHelloButton_Click(object sender, RoutedEventArgs e)
    {
        if (_fallbackAuthService != null)
        {
            var success = await _fallbackAuthService.AuthenticateWithWindowsHelloAsync();
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("Windows Hello authentication failed or is not available.");
            }
        }
        else
        {
            ShowInfo("Windows Hello is not configured. Please use PIN authentication.");
        }
    }

    /// <summary>
    /// Authenticate using Smart Card.
    /// </summary>
    private void SmartCardButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInfo("Smart Card authentication requires a physical card reader.\nPlease insert your smart card and try again.");
    }

    /// <summary>
    /// Cancel button handler.
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Show error message with modern styling.
    /// </summary>
    private void ShowError(string message)
    {
        MessageBox.Show(message, "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Show info message with modern styling.
    /// </summary>
    private void ShowInfo(string message)
    {
        MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
