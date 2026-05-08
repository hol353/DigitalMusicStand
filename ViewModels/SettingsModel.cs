using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media;

namespace AvaloniaInkCanvasDemo.ViewModels;

public class SettingsModel
{
    /// <summary>
    /// Top level dirctory of music library (with no trailing space).
    /// </summary>
    public string MusicLibraryBaseDirectory { get; set; } = "/home/hol353/Seafile/Sheet Music";
    
}