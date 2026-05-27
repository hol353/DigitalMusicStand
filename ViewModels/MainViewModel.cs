using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media;
using ReactiveUI;

namespace BlackFolder;

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
        ReadDirectories(Settings.MusicLibraryBaseDirectory);
        ReadFiles(SelectedDirectory);
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
    /// Collection of relative directory paths.
    /// </summary>
    public ObservableCollection<string> Directories { get; } = new();

    /// <summary>
    /// Currently selected directory path.
    /// </summary>
    public string SelectedDirectory { get; set; } = "Brass Band A-L";

    /// <summary>
    /// Collection of file names (no extension)
    /// </summary>
    public ObservableCollection<string> FileNames { get; } = new();

    /// <summary>
    /// The application settings
    /// </summary>
    public SettingsModel Settings { get; }

    /// <summary>
    /// The instance to load/save annotations.
    /// </summary>
    public Annotations Annotations { get; }

    /// <summary>
    /// Read all directories and files.
    /// </summary>
    private void ReadDirectories(string directory)
    {
        foreach (var dir in Directory.GetDirectories(directory).Order())
            Directories.Add(dir.ToRelative(Settings.MusicLibraryBaseDirectory));
    }
    
    /// <summary>
    /// Read all directories and files.
    /// </summary>
    public void ReadFiles(string relativeDirectory)
    {
        var absoluteDirectory = relativeDirectory.ToAbsolute(Settings.MusicLibraryBaseDirectory);        
        FileNames.Clear();
        foreach (var file in Directory.GetFiles(absoluteDirectory).Order())
            FileNames.Add(Path.GetFileNameWithoutExtension(file));
    }
}
