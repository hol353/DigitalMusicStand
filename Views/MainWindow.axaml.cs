using Avalonia.Controls;

namespace BlackFolder;

/// <summary>
/// Represents the main window of the application.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Constructor</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is closing.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        MainView mainView = Content as MainView;
        mainView.OnClosing();
        base.OnClosing(e); // Call the base method
    }
}
