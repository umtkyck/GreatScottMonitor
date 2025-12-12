using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MedSecureVision.Client.Views;

namespace MedSecureVision.Client.Services;

/// <summary>
/// System tray service for MedSecure Vision.
/// Provides background operation with tray icon, status indicator, and quick actions.
/// </summary>
public class SystemTrayService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private bool _disposed = false;
    private bool _isEnrolled = false;
    private bool _isServiceRunning = true;
    private readonly Action _showMainWindow;
    private readonly Action _showDashboard;
    private readonly Action<bool> _onServiceStateChanged;

    /// <summary>
    /// Event fired when enrollment status changes.
    /// </summary>
    public event EventHandler<bool>? EnrollmentStatusChanged;

    /// <summary>
    /// Creates a new SystemTrayService.
    /// </summary>
    /// <param name="showMainWindow">Action to show the main authentication window</param>
    /// <param name="showDashboard">Action to show the dashboard window</param>
    /// <param name="onServiceStateChanged">Callback when service state changes</param>
    public SystemTrayService(
        Action showMainWindow, 
        Action showDashboard,
        Action<bool> onServiceStateChanged)
    {
        _showMainWindow = showMainWindow;
        _showDashboard = showDashboard;
        _onServiceStateChanged = onServiceStateChanged;
        Initialize();
    }

    /// <summary>
    /// Initialize the system tray icon and context menu.
    /// </summary>
    private void Initialize()
    {
        // Create context menu
        _contextMenu = new ContextMenuStrip();
        _contextMenu.BackColor = Color.FromArgb(22, 27, 34);
        _contextMenu.ForeColor = Color.FromArgb(240, 246, 252);
        _contextMenu.Font = new Font("Segoe UI", 9f);
        _contextMenu.Renderer = new DarkMenuRenderer();

        // Add menu items
        var statusItem = new ToolStripMenuItem("● MedSecure Vision v1.0.0")
        {
            Enabled = false,
            ForeColor = Color.FromArgb(88, 166, 255)
        };
        _contextMenu.Items.Add(statusItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        // Enrollment status
        var enrollmentItem = new ToolStripMenuItem("Enrollment: Not Enrolled")
        {
            Name = "enrollmentStatus",
            ForeColor = Color.FromArgb(248, 81, 73)
        };
        _contextMenu.Items.Add(enrollmentItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        // Actions
        var showWindowItem = new ToolStripMenuItem("🔐 Open Authentication", null, (s, e) => _showMainWindow());
        var dashboardItem = new ToolStripMenuItem("📊 Open Dashboard", null, (s, e) => _showDashboard());
        var enrollItem = new ToolStripMenuItem("👤 Enroll Face", null, OnEnrollFace);
        
        _contextMenu.Items.Add(showWindowItem);
        _contextMenu.Items.Add(dashboardItem);
        _contextMenu.Items.Add(enrollItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        // Service controls
        var serviceMenu = new ToolStripMenuItem("⚙️ Service");
        var startItem = new ToolStripMenuItem("▶️ Start", null, (s, e) => SetServiceState(true)) { Name = "startService" };
        var stopItem = new ToolStripMenuItem("⏹️ Stop", null, (s, e) => SetServiceState(false)) { Name = "stopService" };
        var restartItem = new ToolStripMenuItem("🔄 Restart", null, OnRestartService);
        
        serviceMenu.DropDownItems.Add(startItem);
        serviceMenu.DropDownItems.Add(stopItem);
        serviceMenu.DropDownItems.Add(restartItem);
        _contextMenu.Items.Add(serviceMenu);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Exit
        var exitItem = new ToolStripMenuItem("❌ Exit", null, OnExit);
        _contextMenu.Items.Add(exitItem);

        // Create notify icon
        _notifyIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(false),
            Text = "MedSecure Vision\nStatus: Not Enrolled",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.DoubleClick += (s, e) => _showMainWindow();

        UpdateServiceMenuState();
    }

    /// <summary>
    /// Create a tray icon with enrollment status indicator.
    /// </summary>
    private Icon CreateTrayIcon(bool isEnrolled)
    {
        // Create a simple icon programmatically
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Draw shield background
        using var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, 32, 32),
            Color.FromArgb(88, 166, 255),
            Color.FromArgb(163, 113, 247),
            45f);
        
        // Shield shape
        var shieldPath = new System.Drawing.Drawing2D.GraphicsPath();
        shieldPath.AddArc(2, 2, 10, 10, 180, 90);
        shieldPath.AddArc(20, 2, 10, 10, 270, 90);
        shieldPath.AddLine(30, 12, 30, 20);
        shieldPath.AddArc(16, 20, 14, 10, 0, 90);
        shieldPath.AddArc(2, 20, 14, 10, 90, 90);
        shieldPath.AddLine(2, 20, 2, 12);
        shieldPath.CloseFigure();
        
        g.FillPath(gradientBrush, shieldPath);

        // Status indicator (green dot for enrolled, red for not)
        var statusColor = isEnrolled ? Color.FromArgb(63, 185, 80) : Color.FromArgb(248, 81, 73);
        using var statusBrush = new SolidBrush(statusColor);
        g.FillEllipse(statusBrush, 20, 20, 10, 10);
        
        // White border around status
        using var whitePen = new Pen(Color.White, 1.5f);
        g.DrawEllipse(whitePen, 20, 20, 10, 10);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>
    /// Update enrollment status and refresh tray icon.
    /// </summary>
    public void SetEnrollmentStatus(bool isEnrolled)
    {
        _isEnrolled = isEnrolled;
        
        if (_notifyIcon != null)
        {
            _notifyIcon.Icon = CreateTrayIcon(isEnrolled);
            _notifyIcon.Text = $"MedSecure Vision\nStatus: {(isEnrolled ? "Enrolled ✓" : "Not Enrolled")}";
        }

        // Update menu item
        if (_contextMenu?.Items["enrollmentStatus"] is ToolStripMenuItem item)
        {
            item.Text = isEnrolled ? "Enrollment: Active ✓" : "Enrollment: Not Enrolled";
            item.ForeColor = isEnrolled ? Color.FromArgb(63, 185, 80) : Color.FromArgb(248, 81, 73);
        }

        EnrollmentStatusChanged?.Invoke(this, isEnrolled);
    }

    /// <summary>
    /// Set service running state.
    /// </summary>
    private void SetServiceState(bool running)
    {
        _isServiceRunning = running;
        _onServiceStateChanged(running);
        UpdateServiceMenuState();

        if (_notifyIcon != null)
        {
            _notifyIcon.ShowBalloonTip(
                2000,
                "MedSecure Vision",
                running ? "Service started" : "Service stopped",
                ToolTipIcon.Info);
        }
    }

    /// <summary>
    /// Update service menu items based on current state.
    /// </summary>
    private void UpdateServiceMenuState()
    {
        if (_contextMenu == null) return;

        foreach (ToolStripItem item in _contextMenu.Items)
        {
            if (item is ToolStripMenuItem menuItem && menuItem.Text.Contains("Service"))
            {
                foreach (ToolStripItem subItem in menuItem.DropDownItems)
                {
                    if (subItem.Name == "startService")
                        subItem.Enabled = !_isServiceRunning;
                    else if (subItem.Name == "stopService")
                        subItem.Enabled = _isServiceRunning;
                }
            }
        }
    }

    /// <summary>
    /// Restart the service.
    /// </summary>
    private async void OnRestartService(object? sender, EventArgs e)
    {
        SetServiceState(false);
        await Task.Delay(1000);
        SetServiceState(true);
    }

    /// <summary>
    /// Open enrollment window.
    /// </summary>
    private void OnEnrollFace(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var enrollmentWindow = new AppleStyleEnrollmentWindow();
            if (enrollmentWindow.ShowDialog() == true)
            {
                SetEnrollmentStatus(true);
            }
        });
    }

    /// <summary>
    /// Exit the application.
    /// </summary>
    private void OnExit(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to exit MedSecure Vision?\nThe authentication service will stop.",
            "Exit MedSecure Vision",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Dispose();
            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// Show a balloon notification.
    /// </summary>
    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

/// <summary>
/// Custom dark theme renderer for the context menu.
/// </summary>
internal class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.FromArgb(240, 246, 252);
        base.OnRenderItemText(e);
    }
}

/// <summary>
/// Dark color table for menu styling.
/// </summary>
internal class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => Color.FromArgb(33, 38, 45);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(33, 38, 45);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(33, 38, 45);
    public override Color MenuBorder => Color.FromArgb(48, 54, 61);
    public override Color ToolStripDropDownBackground => Color.FromArgb(22, 27, 34);
    public override Color ImageMarginGradientBegin => Color.FromArgb(22, 27, 34);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(22, 27, 34);
    public override Color ImageMarginGradientEnd => Color.FromArgb(22, 27, 34);
    public override Color SeparatorDark => Color.FromArgb(48, 54, 61);
    public override Color SeparatorLight => Color.FromArgb(48, 54, 61);
}


