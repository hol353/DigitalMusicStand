using Avalonia.Controls;

namespace BlackFolder;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class MainView : UserControl
{
    /// <summary>The PDF canvas control.</summary>
    //private PDFCanvas pdfCanvas;

    /// <summary>
    /// Constructor
    /// </summary>
    public MainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Application is about to close. Save annotations.
    /// </summary>
    internal void OnClosing()
    {
        var model = DataContext as MainViewModel;
        model.MusicLibrary.CloseAll();
        model.Settings.Save(model.BaseDirectory);
    }
}
