using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using ReactiveUI;

namespace BlackFolder;

public class MainViewModel : ReactiveObject
{
    /// <summary>Name of application.</summary>
    private string applicationName;

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
        this.applicationName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        Settings = SettingsModel.Create(BaseDirectory);
        MusicLibrary = new(this);
    }

    /// <summary>
    /// The music library instance.
    /// </summary>
    public MusicLibrary MusicLibrary { get; }

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
    public string BaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), applicationName );

    /// <summary>
    /// The application settings
    /// </summary>
    public SettingsModel Settings { get; }

}
