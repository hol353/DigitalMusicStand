using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;

namespace BlackFolder;

public class MainViewModel : ReactiveObject
{
    /// <summary>Name of application.</summary>
    private string applicationName;

    /// <summary>Is the app in pen mode?</summary>
    private bool _isPenMode;

    /// <summary>Is the app in eraser mode?</summary>
    private bool _isEraserMode;

    /// <summary>Is the app in highlighter mode?</summary>
    private bool _isHighlighterMode;

    /// <summary>Is file select mode enabled?</summary>    
    private bool _isFileSelectMode;

    /// <summary>
    /// Is the toolbar visible?
    /// </summary>    
    private bool _isToolbarVisible = true;

    /// <summary>
    /// The currently selected file name (relative to BasePath).
    /// </summary>
    private string _selectedFile;

    /// <summary>
    /// The currently selected colour.
    /// </summary>
    private ISolidColorBrush _color = Brushes.Red;

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
    /// Is the toolbar visible?
    /// </summary>
    public bool IsToolbarVisible
    {
        get => _isToolbarVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _isToolbarVisible, value);
            if (!IsToolbarVisible)
            {
                IsPenMode = false;
                IsEraserMode = false;
                IsHighlighterMode = false;
                IsFileSelectMode = false;
            }
        }
    }

    /// <summary>
    /// Is pen mode enabled?
    /// </summary>
    public bool IsPenMode 
    {
        get => _isPenMode;
        set
        {
            if (value)
            {
                IsEraserMode = false;
                IsHighlighterMode = false;
                IsFileSelectMode = false;
                SelectedBrush = SolidColorBrushCollection[Settings.SelectedPenColorIndex];
            }
            else if (value != _isPenMode)
                Settings.SelectedPenColorIndex = SolidColorBrushCollection.IndexOf(SelectedBrush);
            this.RaiseAndSetIfChanged(ref _isPenMode, value);
        }
    }

    /// <summary>
    /// Is eraser mode enabled?
    /// </summary>
    public bool IsEraserMode 
    {
        get => _isEraserMode;
        set
        {
            if (value)
            {
                IsPenMode = false;
                IsHighlighterMode = false;
                IsFileSelectMode = false;
            }
            this.RaiseAndSetIfChanged(ref _isEraserMode, value);
        }
    }

    /// <summary>
    /// Is highlighter mode enabled?
    /// </summary>
    public bool IsHighlighterMode
    {
        get => _isHighlighterMode;
        set
        {
            if (value)
            {
                IsPenMode = false;
                IsEraserMode = false;
                IsFileSelectMode = false;
                SelectedBrush = SolidColorBrushCollection[Settings.SelectedHighlighterColorIndex];
            }
            else if (value != _isHighlighterMode)
                Settings.SelectedHighlighterColorIndex = SolidColorBrushCollection.IndexOf(SelectedBrush);
            this.RaiseAndSetIfChanged(ref _isHighlighterMode, value);
        }
    }


    /// <summary>
    /// Is file select mode enabled?
    /// </summary>
    public bool IsFileSelectMode 
    {
        get => _isFileSelectMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isFileSelectMode, value);
            if (IsFileSelectMode)
            {
                IsPenMode = false;
                IsEraserMode = false;
                IsHighlighterMode = false;
            }
        }
    }

    /// <summary>
    /// The currently selected directory (relative to BasePath).
    /// </summary>
    public string SelectedDirectory 
    { 
        get => Settings.SelectedDirectory; 
        set 
        { 
            if (value != Settings.SelectedDirectory)
            {
                this.RaisePropertyChanging("SelectedDirectory");
                Settings.SelectedDirectory = value;
                MusicLibrary.ReadFiles(); 
                this.RaisePropertyChanged("SelectedDirectory");
            }
        } 
    }

    /// <summary>
    /// The currently selected file (relative to SelectedDirectory).
    /// </summary>
    public string SelectedFile 
    { 
        get => _selectedFile; 
        set 
        {
            this.RaiseAndSetIfChanged(ref _selectedFile, value);
            if (value != null)
                IsToolbarVisible = false;
        }
    }    

    /// <summary>
    /// Collection of colour brushes/
    /// </summary>
    public ObservableCollection<ISolidColorBrush> SolidColorBrushCollection { get; }

    /// <summary>
    /// The currently selected colour.
    /// </summary>
    public ISolidColorBrush SelectedBrush
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    /// <summary>
    /// The directory where the application stores settings/annotations.
    /// </summary>
    public string BaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), applicationName );

    /// <summary>
    /// The application settings
    /// </summary>
    public SettingsModel Settings { get; }

}
