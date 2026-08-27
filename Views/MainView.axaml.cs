using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Skia;
using DotNetCampus.Inking;

namespace BlackFolder;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class MainView : UserControl
{
    /// <summary>The PDF canvas control.</summary>
    private PDFCanvas pdfCanvas;

    /// <summary>
    /// Constructor
    /// </summary>
    public MainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the control is attached to the visual tree.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        pdfCanvas = this.FindControl<PDFCanvas>("PDFCanvas");
        pdfCanvas.CentreTap += (sender, args) => SetToolbarVisibility(true);
        base.OnAttachedToVisualTree(e);
    }

    /// <summary>
    /// Invoked when the control is detached from the visual tree.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// Invoked when the pen button is clicked.
    /// </summary>
    private void PenModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PenModeButton.IsChecked == true)
            pdfCanvas.AnnotateMode = InkCanvasEditingMode.Ink;
        else
            pdfCanvas.AnnotateMode = InkCanvasEditingMode.None;
        EraserModeButton.IsChecked = false;
    }

    /// <summary>
    /// Invoked when the eraser button is clicked.
    /// </summary>
    private void EraserModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (EraserModeButton.IsChecked == true)
            pdfCanvas.AnnotateMode = InkCanvasEditingMode.EraseByPoint;
        else
            pdfCanvas.AnnotateMode = InkCanvasEditingMode.None;

        PenModeButton.IsChecked = false;
    }

    /// <summary>
    /// Invoked when the open button is clicked.
    /// </summary>
    private void OnOpenButtonClick(object sender, RoutedEventArgs e)
    {
        FilePopup.IsOpen = !FilePopup.IsOpen;
        if (FilePopup.IsOpen)
        {
            ComboBox.SelectionChanged += OnComboBoxSelectionChanged;
            ListBox.SelectionChanged += OnListBoxSelectionChanged;
            foreach (Button button in NavigationButtons.Children)
                button.Click += OnNavigationButtonClicked;
        }
        else
        {
            ComboBox.SelectionChanged -= OnComboBoxSelectionChanged;
            ListBox.SelectionChanged -= OnListBoxSelectionChanged;
            foreach (Button button in NavigationButtons.Children)
                button.Click -= OnNavigationButtonClicked;
        }
    }

    /// <summary>
    /// User has clicked a navigation button.
    /// </summary>
    private void OnNavigationButtonClicked(object sender, RoutedEventArgs e)
    {
        var model = DataContext as MainViewModel;

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

    /// <summary>
    /// User has selected a file to open in the treeview
    /// </summary>
    private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var model = DataContext as MainViewModel;

        var comboBox = (ComboBox)sender!;
        var relativeDirectory = comboBox.SelectedItem.ToString();

        model.MusicLibrary.SelectedDirectory = relativeDirectory;
    }

    /// <summary>
    /// User has selected a file to open in the treeview
    /// </summary>
    private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var model = DataContext as MainViewModel;

        var listBox = (ListBox)sender!;
        if (listBox.SelectedItem != null)
        {
            OnOpenButtonClick(null, null);
            SetToolbarVisibility(false);

            var relativePdfFilePath = listBox.SelectedItem.ToString();

            var relativeDirectory = model.MusicLibrary.SelectedDirectory;
            var absoluteDirectory = relativeDirectory.ToAbsolute(model.Settings.MusicLibraryBaseDirectory);

            // The file can be either .pdf or .txt (setlist).
            var absolutePdfFilePath = Path.Combine(absoluteDirectory, relativePdfFilePath) + ".pdf";
            if (File.Exists(absolutePdfFilePath))
                pdfCanvas.Load(model.MusicLibrary, [absolutePdfFilePath]);
            else 
            {
                // Handle .txt file case
                var absoluteTxtFilePath = Path.Combine(absoluteDirectory, relativePdfFilePath) + ".txt";
                if (File.Exists(absoluteTxtFilePath))
                    pdfCanvas.Load(model.MusicLibrary, File.ReadAllLines(absoluteTxtFilePath));
            }
        }
    }

    /// <summary>
    /// Sets the visibility of the toolbar.
    /// </summary>
    /// <param name="show">Show the toolbar?</param>
    public void SetToolbarVisibility(bool show)
    {
        Toolbar.IsVisible = show;
    }

    /// <summary>
    /// Invoked when a color is selected from the drop down.
    /// </summary>
    private void OnColourSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count is 1)
            if (e.AddedItems[0] is ISolidColorBrush brush)
                pdfCanvas.SetAnnotateColour(brush.Color.ToSKColor());
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
