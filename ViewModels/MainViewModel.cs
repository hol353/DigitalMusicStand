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
        Settings = LoadSettings();
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
    public string BaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), applicationName );

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

    /// <summary>
    /// Load the application settings from disk.
    /// </summary>
    /// <returns>An instance of the settings model.</returns>
    private SettingsModel LoadSettings()
    {
        try
        {
            Directory.CreateDirectory(BaseDirectory);
            var path = Path.Combine(BaseDirectory, "settings.json");
            if (!File.Exists(path))
                throw new Exception($"{BaseDirectory}/settings.json file does not exist");

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SettingsModel>(json, opts) ?? new SettingsModel();
        }
        catch (Exception)
        {
            string path = Path.Combine(BaseDirectory, "Library");
            Directory.CreateDirectory(path);
            return new SettingsModel() 
            { 
                MusicLibraryBaseDirectory = path
            };
        }
    }
}
