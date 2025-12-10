using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using MedSecureVision.Client.ViewModels;
using MedSecureVision.Client.Views;

// Resolve WPF vs WinForms/System.Drawing ambiguities
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using MessageBox = System.Windows.MessageBox;

namespace MedSecureVision.Client;

/// <summary>
/// Main authentication window for MedSecure Vision.
/// Provides Face ID-like biometric authentication experience.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AuthenticationViewModel viewModel)
        {
            viewModel.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }
    }

    /// <summary>
    /// Handles authentication state changes and updates UI accordingly.
    /// </summary>
    private void OnAuthenticationStateChanged(object? sender, string state)
    {
        Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case "Searching":
                    UpdateUIForSearching();
                    break;
                case "Positioning":
                    UpdateUIForPositioning();
                    break;
                case "Verifying":
                    UpdateUIForVerifying();
                    break;
                case "Success":
                    PlaySuccessAnimation();
                    break;
                case "Failure":
                    PlayFailureAnimation();
                    break;
            }
        });
    }

    private void UpdateUIForSearching()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
        StatusTitle.Text = "Looking for face...";
        StatusDescription.Text = "Position your face within the frame";
        UpdateFaceGuideColor("#58A6FF");
    }

    private void UpdateUIForPositioning()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xD2, 0x99, 0x22));
        StatusTitle.Text = "Adjusting...";
        StatusDescription.Text = "Move closer and center your face";
        UpdateFaceGuideColor("#D29922");
    }

    private void UpdateUIForVerifying()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xA3, 0x71, 0xF7));
        StatusTitle.Text = "Verifying identity...";
        StatusDescription.Text = "Please hold still";
        UpdateFaceGuideColor("#A371F7");
    }

    private void UpdateFaceGuideColor(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        var brush = new SolidColorBrush(color);
        
        FaceGuide.Stroke = new LinearGradientBrush(
            color,
            Color.FromRgb(0xA3, 0x71, 0xF7),
            new Point(0, 0),
            new Point(1, 1));
            
        if (FaceGuide.Effect is DropShadowEffect shadow)
        {
            shadow.Color = color;
        }
    }

    private void PlaySuccessAnimation()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
        StatusTitle.Text = "Welcome!";
        StatusDescription.Text = "Authentication successful";

        // Show success checkmark
        SuccessCheckmark.Visibility = Visibility.Visible;

        // Create scale animation
        var scaleAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
        {
            EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 5 }
        };

        var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));

        var scaleTransform = new ScaleTransform(0, 0);
        SuccessCheckmark.RenderTransform = scaleTransform;
        SuccessCheckmark.RenderTransformOrigin = new Point(0.5, 0.5);

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        SuccessCheckmark.BeginAnimation(OpacityProperty, opacityAnimation);

        // Update face guide to green
        UpdateFaceGuideColor("#3FB950");
    }

    private void PlayFailureAnimation()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));
        StatusTitle.Text = "Not recognized";
        StatusDescription.Text = "Please try again or use PIN";

        // Shake animation
        var shakeAnimation = new DoubleAnimationUsingKeyFrames();
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(0)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-15, TimeSpan.FromMilliseconds(50)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(15, TimeSpan.FromMilliseconds(100)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-15, TimeSpan.FromMilliseconds(150)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(15, TimeSpan.FromMilliseconds(200)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-10, TimeSpan.FromMilliseconds(250)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(10, TimeSpan.FromMilliseconds(300)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(350)));

        var transform = new TranslateTransform();
        FaceGuide.RenderTransform = transform;
        transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);

        // Update to red
        UpdateFaceGuideColor("#F85149");
    }

    // ═══════════════════════════════════════════════════════════════
    // Window Chrome Event Handlers
    // ═══════════════════════════════════════════════════════════════

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ═══════════════════════════════════════════════════════════════
    // Action Button Event Handlers
    // ═══════════════════════════════════════════════════════════════

    private void FallbackAuth_Click(object sender, RoutedEventArgs e)
    {
        var fallbackWindow = new FallbackAuthWindow();
        fallbackWindow.Owner = this;
        fallbackWindow.ShowDialog();
    }

    private void EnrollFace_Click(object sender, RoutedEventArgs e)
    {
        var enrollmentWindow = new EnrollmentWindow();
        enrollmentWindow.Owner = this;
        var result = enrollmentWindow.ShowDialog();
        
        if (result == true)
        {
            // Enrollment successful
            StatusTitle.Text = "Face enrolled!";
            StatusDescription.Text = "You can now authenticate with your face";
            StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
        }
    }
}
