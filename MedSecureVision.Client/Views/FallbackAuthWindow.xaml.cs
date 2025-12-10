using System.Windows;
using MedSecureVision.Client.Services;

namespace MedSecureVision.Client.Views;

public partial class FallbackAuthWindow : Window
{
    private readonly IFallbackAuthService _fallbackAuthService;

    public FallbackAuthWindow(IFallbackAuthService fallbackAuthService)
    {
        InitializeComponent();
        _fallbackAuthService = fallbackAuthService;
    }

    private async void AuthenticateButton_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        if (string.IsNullOrEmpty(pin))
        {
            MessageBox.Show("Please enter a PIN", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var success = await _fallbackAuthService.AuthenticateWithPinAsync(pin);
        if (success)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Invalid PIN. Please try again.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            PinBox.Clear();
        }
    }

    private async void WindowsHelloButton_Click(object sender, RoutedEventArgs e)
    {
        var success = await _fallbackAuthService.AuthenticateWithWindowsHelloAsync();
        if (success)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Windows Hello authentication failed.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

