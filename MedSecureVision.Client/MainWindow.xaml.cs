using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MedSecureVision.Client.ViewModels;

namespace MedSecureVision.Client;

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

    private void OnAuthenticationStateChanged(object? sender, string state)
    {
        Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case "Success":
                    PlaySuccessAnimation();
                    break;
                case "Failure":
                    PlayFailureAnimation();
                    break;
            }
        });
    }

    private void PlaySuccessAnimation()
    {
        var checkmark = SuccessCheckmark;
        checkmark.Visibility = Visibility.Visible;

        var scaleAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 3 }
        };
        var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));

        CheckmarkScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        CheckmarkScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        checkmark.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
    }

    private void PlayFailureAnimation()
    {
        var shakeAnimation = new DoubleAnimationUsingKeyFrames();
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(0)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-10, TimeSpan.FromMilliseconds(50)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(10, TimeSpan.FromMilliseconds(100)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-10, TimeSpan.FromMilliseconds(150)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(10, TimeSpan.FromMilliseconds(200)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(250)));

        var transform = new TranslateTransform();
        FaceGuide.RenderTransform = transform;
        transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
    }
}

