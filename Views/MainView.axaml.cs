using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Skia;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Erasing;
using MuPDFCore;
using SkiaSharp;

namespace BlackFolder;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class MainView : UserControl
{
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
        var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
        zoomBorder.ResizeBehavior = ResizeBehaviorMode.ReapplyStretch;
        
        zoomBorder.SizeChanged += OnZoomBorderSizeChanged;
        zoomBorder.PanStarted += OnPanStarted;
        zoomBorder.ZoomStarted += OnZoomStarted;
        base.OnAttachedToVisualTree(e);
    }

    /// <summary>
    /// Invoked when the control is detached from the visual tree.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
        zoomBorder.SizeChanged -= OnZoomBorderSizeChanged;
        zoomBorder.PanStarted -= OnPanStarted;
        zoomBorder.ZoomStarted -= OnZoomStarted;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// Invoked when the size of the ZoomBorder changes. Resizes all child controls to fit the new size.
    /// </summary>
    private void OnZoomBorderSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ResizeChildren(e);
    }

    /// <summary>
    /// Invoked when the user starts panning. Sets the min and max offsets for the ZoomBorder based on the current zoom level and the height of the music canvas.
    /// </summary>
    private void OnPanStarted(object sender, PanEventArgs e)
    {
        double maximumHeight = (MusicCanvas.Children.Count-1) * Bounds.Height;
        
        var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
        if (Math.Round(zoomBorder.ZoomX, 2) == 1 && Math.Round(zoomBorder.ZoomY, 2) == 1)  // no zoom applied
        {
            zoomBorder.MinOffsetX = 0;
            zoomBorder.MaxOffsetX = 0;
            zoomBorder.MinOffsetY = -maximumHeight;
            zoomBorder.MaxOffsetY = 0;
        }
    }

    /// <summary>
    /// Invoked when the user starts zooming. Sets the min and max offsets for the ZoomBorder based on the current zoom level.
    /// </summary>
    private void OnZoomStarted(object sender, ZoomEventArgs e)
    {
        var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
        if (Math.Round(zoomBorder.ZoomX, 2) > 1 || Math.Round(zoomBorder.ZoomY, 2) > 1)
        {
            zoomBorder.MinOffsetX = -Bounds.Width;
            zoomBorder.MaxOffsetX = Bounds.Width;
            zoomBorder.MinOffsetY = -Bounds.Height;
            zoomBorder.MaxOffsetY = Bounds.Height;
        }
    }    

    /// <summary>
    /// Resizes all child controls to fit the new size of the ZoomBorder.
    /// </summary>
    private void ResizeChildren(SizeChangedEventArgs e)
    {
        var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
        zoomBorder.Zoom(1.0, Bounds.Width / 2, Bounds.Height / 2);
        foreach (SheetMusicControl sheetMusicControl in MusicCanvas.Children)
        {
            sheetMusicControl.Width = e.NewSize.Width;
            sheetMusicControl.Height = e.NewSize.Height;
        }
    }

    /// <summary>
    /// Invoked when the pen button is clicked.
    /// </summary>
    private void PenModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PenModeButton.IsChecked == true)
            SetInkCanvasEditingMode(InkCanvasEditingMode.Ink);
        else
            SetInkCanvasEditingMode(InkCanvasEditingMode.None);
        EraserModeButton.IsChecked = false;
    }

    /// <summary>
    /// Invoked when the eraser button is clicked.
    /// </summary>
    private void EraserModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (EraserModeButton.IsChecked == true)
            SetInkCanvasEditingMode(InkCanvasEditingMode.EraseByPoint);
        else
            SetInkCanvasEditingMode(InkCanvasEditingMode.None);

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
        SetInkCanvasEditingMode(InkCanvasEditingMode.None);
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
            var relativePdfFilePath = listBox.SelectedItem.ToString();

            var relativeDirectory = model.MusicLibrary.SelectedDirectory;
            var absoluteDirectory = relativeDirectory.ToAbsolute(model.Settings.MusicLibraryBaseDirectory);

            MusicCanvas.Children.Clear();

            // The file can be either .pdf or .txt (setlist).
            var absolutePdfFilePath = Path.Combine(absoluteDirectory, relativePdfFilePath) + ".pdf";
            if (File.Exists(absolutePdfFilePath))
                Load(absolutePdfFilePath);
            else 
            {
                var absoluteTxtFilePath = Path.Combine(absoluteDirectory, relativePdfFilePath) + ".txt";
                if (File.Exists(absoluteTxtFilePath))
                {
                    // Handle .txt file case
                    string[] fileNames = File.ReadAllLines(absoluteTxtFilePath);
                    foreach (var fileName in fileNames)
                    {
                        if (File.Exists(fileName))
                        {
                            Load(fileName);
                        }
                    }
                }
            }
            OnOpenButtonClick(null, null);
            SetToolbarVisibility(false);
        }
    }
      
    /// <summary>
    /// Load a pdf file.
    /// </summary>
    /// <param name="pdfFilePath">The path of the pdf file.</param>
    public void Load(string pdfFilePath)
    {
        var model = DataContext as MainViewModel;
        model.PdfFilePath = pdfFilePath;

        try
        {
            model.MusicLibrary.Open(pdfFilePath);
            foreach (var file in model.MusicLibrary.OpenFiles)
            {
                foreach (var page in file.Pages)
                {
                    var sheetMusicControl = new SheetMusicControl(this, page);
                    sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.EraserViewCreator = new DelegateEraserViewCreator(() => new CustomEraserView());
                    sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.InkThickness = 2;
                    MusicCanvas.Children.Add(sheetMusicControl);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering PDF: {ex.Message}");
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
        {
            if (e.AddedItems[0] is ISolidColorBrush brush)
            {
                SetInkCanvasInkColour(brush.Color.ToSKColor());
            }
        }
    }

    /// <summary>
    /// Sets the editing mode
    /// </summary>
    /// <param name="mode">Draw vs erase.</param>
    private void SetInkCanvasEditingMode(InkCanvasEditingMode mode)
    {
        foreach (InkCanvas inkCanvas in MusicCanvas.Children)
            inkCanvas.EditingMode = mode;

        if (mode == InkCanvasEditingMode.None && MusicCanvas.Children.Count > 0)
        {
            SetToolbarVisibility(false);
            InvalidateVisual();
        }
        
    }

    /// <summary>
    /// Sets the editing colour
    /// </summary>
    /// <param name="colour">The ink colour</param>
    private void SetInkCanvasInkColour(SKColor colour)
    {
        foreach (InkCanvas inkCanvas in MusicCanvas.Children)
            inkCanvas.AvaloniaSkiaInkCanvas.Settings.InkColor = colour;
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
