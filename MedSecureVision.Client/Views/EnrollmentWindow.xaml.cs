using System.Windows;
using MedSecureVision.Client.ViewModels;

namespace MedSecureVision.Client.Views;

public partial class EnrollmentWindow : Window
{
    private readonly EnrollmentViewModel _viewModel;

    public EnrollmentWindow(EnrollmentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        var success = await _viewModel.CaptureFrameAsync();
        if (!success)
        {
            MessageBox.Show(_viewModel.StatusMessage, "Capture Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Get userId from authentication
        var userId = "temp-user-id";
        var success = await _viewModel.CompleteEnrollmentAsync(userId);
        
        if (success)
        {
            MessageBox.Show("Enrollment completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Enrollment failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

