using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BlackFolder;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class FileSelect : UserControl
{
    private MainViewModel model;

    /// <summary>
    /// Constructor
    /// </summary>
    public FileSelect()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the control is loaded.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        model = DataContext as MainViewModel;
    }    

    /// <summary>
    /// User has clicked a navigation button.
    /// </summary>
    private void OnNavigationButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        string letter = button.Content.ToString();
        string selectedFile;
        if (letter == "#")
            selectedFile = model.MusicLibrary.RelativeFileNames.FirstOrDefault(file => Int32.TryParse(file, out int i));
        else
            selectedFile = model.MusicLibrary.RelativeFileNames.FirstOrDefault(file => file.StartsWith(letter, ignoreCase: true, CultureInfo.CurrentCulture));
        if (selectedFile != null)
        {
            ListBox.ScrollIntoView(model.MusicLibrary.RelativeFileNames.Last());
            ListBox.ScrollIntoView(selectedFile);
        }
    }    
}
