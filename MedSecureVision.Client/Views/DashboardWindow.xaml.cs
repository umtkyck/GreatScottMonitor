using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

// Resolve WPF vs WinForms/System.Drawing ambiguities
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace MedSecureVision.Client.Views;

/// <summary>
/// Dashboard window for managing enrolled faces and viewing statistics.
/// Provides controls for enrollment management, deletion, and data export.
/// </summary>
public partial class DashboardWindow : Window
{
    /// <summary>
    /// Collection of enrolled faces for display.
    /// </summary>
    public ObservableCollection<EnrolledFace> EnrolledFaces { get; } = new();

    public DashboardWindow()
    {
        InitializeComponent();
        LoadData();
    }

    /// <summary>
    /// Load enrolled face data and statistics.
    /// </summary>
    private void LoadData()
    {
        // Demo data - in production, load from database
        var hasEnrollment = CheckEnrollmentStatus();
        
        if (hasEnrollment)
        {
            EnrolledFaces.Add(new EnrolledFace
            {
                Id = "1",
                Name = "Primary Face",
                EnrolledDate = "Enrolled: " + DateTime.Now.AddDays(-5).ToString("MMM dd, yyyy"),
                Quality = 0.95f
            });
            
            EmptyState.Visibility = Visibility.Collapsed;
            FacesList.ItemsSource = EnrolledFaces;
            
            // Update stats
            TotalAuthsText.Text = "47";
            SuccessRateText.Text = "98%";
            LastAuthText.Text = DateTime.Now.AddMinutes(-15).ToString("HH:mm, MMM dd");
            
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(63, 185, 80));
            StatusText.Text = "Active";
        }
        else
        {
            EmptyState.Visibility = Visibility.Visible;
            FacesList.Visibility = Visibility.Collapsed;
            
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(248, 81, 73));
            StatusText.Text = "Not Enrolled";
        }
    }

    /// <summary>
    /// Check if user has enrolled face data.
    /// </summary>
    private bool CheckEnrollmentStatus()
    {
        // In production, check database/local storage
        // For demo, check if enrollment file exists
        var enrollmentPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MedSecureVision", "enrollment.dat");
        return File.Exists(enrollmentPath);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void EnrollNow_Click(object sender, RoutedEventArgs e)
    {
        var enrollmentWindow = new EnrollmentWindow();
        enrollmentWindow.Owner = this;
        if (enrollmentWindow.ShowDialog() == true)
        {
            LoadData(); // Refresh
        }
    }

    private void ReenrollFace_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will replace your current face enrollment. Continue?",
            "Re-enroll Face",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            EnrollNow_Click(sender, e);
        }
    }

    private void EditFace_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Edit feature coming soon!", "Edit Face", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteFace_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to delete this face enrollment?\nYou will need to re-enroll to use face authentication.",
            "Delete Enrollment",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            // Delete enrollment data
            var enrollmentPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MedSecureVision", "enrollment.dat");
            
            if (File.Exists(enrollmentPath))
                File.Delete(enrollmentPath);

            EnrolledFaces.Clear();
            LoadData();
            
            MessageBox.Show("Face enrollment deleted.", "Deleted", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportData_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Export feature coming soon!", "Export Data", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

/// <summary>
/// Represents an enrolled face record.
/// </summary>
public class EnrolledFace
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string EnrolledDate { get; set; } = "";
    public float Quality { get; set; }
}

