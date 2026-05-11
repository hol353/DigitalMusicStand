using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media;
using ReactiveUI;

namespace Coda;

public class MainViewModel : ReactiveObject
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MainViewModel()
    {
        SolidColorBrushCollection =
        [
            Brushes.Red,
            Brushes.Yellow,
            Brushes.Black,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Orange,
            Brushes.Purple
        ];
        Settings = new();
        Annotations = new(this);
    }

    /// <summary>
    /// Page of the currenly open pdf file.
    /// </summary>
    public string PdfFilePath { get; set; }

    /// <summary>
    /// Collection of colour brushes/
    /// </summary>
    public ObservableCollection<IBrush> SolidColorBrushCollection { get; }

    /// <summary>
    /// The directory where the application stores settings/annotations.
    /// </summary>
    public string BaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DigitalSheetMusic");

    /// <summary>
    /// The application settings
    /// </summary>
    public SettingsModel Settings { get; }

    /// <summary>
    /// The instance to load/save annotations.
    /// </summary>
    public Annotations Annotations { get; }
}
