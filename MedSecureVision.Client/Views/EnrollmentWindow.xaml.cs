using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using MedSecureVision.Client.ViewModels;

namespace MedSecureVision.Client.Views;

/// <summary>
/// Face enrollment window for registering user biometric data.
/// Guides users through a multi-step face capture process.
/// </summary>
public partial class EnrollmentWindow : Window
{
    private readonly EnrollmentViewModel? _viewModel;
    private int _capturedFrames = 0;
    private const int RequiredFrames = 8;

    private readonly string[] _stepInstructions = new[]
    {
        "Look straight at the camera",
        "Turn your head slightly left",
        "Turn your head slightly right",
        "Tilt your head up slightly",
        "Tilt your head down slightly",
        "Move closer to the camera",
        "Move back from the camera",
        "Final capture - look straight"
    };

    /// <summary>
    /// Default constructor for design-time and standalone use.
    /// </summary>
    public EnrollmentWindow()
    {
        InitializeComponent();
        UpdateProgress();
    }

    /// <summary>
    /// Constructor with dependency injection for enrollment view model.
    /// </summary>
    public EnrollmentWindow(EnrollmentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        UpdateProgress();
    }

    /// <summary>
    /// Allow window dragging.
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
        if (_capturedFrames > 0)
        {
            var result = MessageBox.Show(
                "You have captured frames. Are you sure you want to cancel enrollment?",
                "Cancel Enrollment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Capture a frame of the user's face.
    /// </summary>
    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        // Disable button during capture
        CaptureButton.IsEnabled = false;

        // Play capture flash animation
        await PlayCaptureFlashAsync();

        if (_viewModel != null)
        {
            var success = await _viewModel.CaptureFrameAsync();
            if (success)
            {
                _capturedFrames++;
                UpdateProgress();
            }
            else
            {
                ShowMessage("Could not capture frame. Please adjust your position.", false);
            }
        }
        else
        {
            // Demo mode
            _capturedFrames++;
            UpdateProgress();
        }

        // Re-enable button
        CaptureButton.IsEnabled = true;
    }

    /// <summary>
    /// Complete the enrollment process.
    /// </summary>
    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_capturedFrames < RequiredFrames)
        {
            ShowMessage($"Please capture {RequiredFrames - _capturedFrames} more frames.", false);
            return;
        }

        CompleteButton.IsEnabled = false;
        CompleteButton.Content = "Processing...";

        if (_viewModel != null)
        {
            var userId = "temp-user-id"; // TODO: Get from authentication
            var success = await _viewModel.CompleteEnrollmentAsync(userId);

            if (success)
            {
                ShowMessage("Enrollment completed successfully!", true);
                await Task.Delay(1500);
                DialogResult = true;
                Close();
            }
            else
            {
                ShowMessage("Enrollment failed. Please try again.", false);
                CompleteButton.IsEnabled = true;
                CompleteButton.Content = "Done";
            }
        }
        else
        {
            // Demo mode
            await Task.Delay(2000);
            ShowMessage("Enrollment completed successfully!", true);
            await Task.Delay(1500);
            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Cancel button handler.
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CloseButton_Click(sender, e);
    }

    /// <summary>
    /// Update progress indicator and instructions.
    /// </summary>
    private void UpdateProgress()
    {
        int currentStep = Math.Min(_capturedFrames + 1, RequiredFrames);
        StepTitle.Text = $"Step {currentStep} of {RequiredFrames}";
        StepDescription.Text = _stepInstructions[Math.Min(_capturedFrames, RequiredFrames - 1)];
        ProgressText.Text = $"{_capturedFrames}/{RequiredFrames}";

        // Update progress ring (approximate dash array calculation)
        double progress = (double)_capturedFrames / RequiredFrames;
        ProgressRing.StrokeDashArray = new DoubleCollection { progress * 4, 4 };

        // Enable complete button when enough frames captured
        if (_capturedFrames >= RequiredFrames)
        {
            CompleteButton.IsEnabled = true;
            CompleteButton.Opacity = 1;
            StatusMessage.Text = "Ready to complete enrollment!";
            
            // Change face guide to green
            UpdateFaceGuideColor("#3FB950");
        }
        else
        {
            StatusMessage.Text = _stepInstructions[_capturedFrames];
        }

        // Show direction arrow for turn instructions
        ShowDirectionArrow(_capturedFrames);
    }

    /// <summary>
    /// Show direction arrow for head turn instructions.
    /// </summary>
    private void ShowDirectionArrow(int step)
    {
        switch (step)
        {
            case 1: // Turn left
                DirectionArrow.Opacity = 1;
                DirectionArrow.RenderTransform = new RotateTransform(180, 15, 15);
                break;
            case 2: // Turn right
                DirectionArrow.Opacity = 1;
                DirectionArrow.RenderTransform = new RotateTransform(0, 15, 15);
                break;
            case 3: // Tilt up
                DirectionArrow.Opacity = 1;
                DirectionArrow.RenderTransform = new RotateTransform(-90, 15, 15);
                break;
            case 4: // Tilt down
                DirectionArrow.Opacity = 1;
                DirectionArrow.RenderTransform = new RotateTransform(90, 15, 15);
                break;
            default:
                DirectionArrow.Opacity = 0;
                break;
        }
    }

    /// <summary>
    /// Update face guide color.
    /// </summary>
    private void UpdateFaceGuideColor(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        FaceGuide.Stroke = new SolidColorBrush(color);
        if (FaceGuide.Effect is DropShadowEffect shadow)
        {
            shadow.Color = color;
        }
    }

    /// <summary>
    /// Play capture flash animation.
    /// </summary>
    private async Task PlayCaptureFlashAsync()
    {
        var fadeIn = new DoubleAnimation(0, 0.8, TimeSpan.FromMilliseconds(50));
        var fadeOut = new DoubleAnimation(0.8, 0, TimeSpan.FromMilliseconds(200));

        CaptureFlash.BeginAnimation(OpacityProperty, fadeIn);
        await Task.Delay(50);
        CaptureFlash.BeginAnimation(OpacityProperty, fadeOut);
        await Task.Delay(200);
    }

    /// <summary>
    /// Show status message.
    /// </summary>
    private void ShowMessage(string message, bool isSuccess)
    {
        StatusMessage.Text = message;
        StatusMessage.Foreground = new SolidColorBrush(
            isSuccess ? Color.FromRgb(0x3F, 0xB9, 0x50) : Color.FromRgb(0xF8, 0x51, 0x49));
    }
}
