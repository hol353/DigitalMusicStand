using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
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
    /// <summary>The time when the user started panning. Used to determine if a tap is a pan or a single tap.</summary>
    private DateTime panStartTime = DateTime.MinValue;

    /// <summary>The time the user spent panning. Used to determine if a tap is a pan or a single tap.</summary>
    private TimeSpan panTime;

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
        zoomBorder.PanEnded += OnPanEnded;
        zoomBorder.ZoomDeltaChanged += (s, e) => OnZoomStarted(null, null);
        this.Tapped += OnSingleTap;
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
        zoomBorder.PanEnded -= OnPanEnded;
        this.Tapped -= OnSingleTap;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// Invoked when the size of the ZoomBorder changes. Resizes all child controls to fit the new size.
    /// </summary>
    private void OnZoomBorderSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ResizeChildren(e.NewSize);
    }

    /// <summary>
    /// Invoked when the user starts panning. Sets the min and max offsets for the ZoomBorder based on the current zoom level and the height of the music canvas.
    /// </summary>
    private void OnPanStarted(object sender, PanEventArgs e)
    {
        panStartTime = DateTime.Now;
        var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
        if (Math.Round(zoomBorder.ZoomX, 2) == 1 && Math.Round(zoomBorder.ZoomY, 2) == 1)  // no zoom applied
            PanInYDirectionOnly();
    }


    private void OnPanEnded(object sender, PanEventArgs e)
    {
        panTime = DateTime.Now - panStartTime;
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
    private void ResizeChildren(Avalonia.Size newSize)
    {
        var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
        zoomBorder.Zoom(1.0, Bounds.Width / 2, Bounds.Height / 2);
        foreach (SheetMusicControl sheetMusicControl in MusicCanvas.Children)
        {
            sheetMusicControl.Width = newSize.Width;
            sheetMusicControl.Height = newSize.Height;
        }
    }

    /// <summary>
    /// Handles single-tap gestures.
    /// </summary>
    private void OnSingleTap(object sender, TappedEventArgs e)
    {
        // detect if the user is currently panning, and if so, ignore the tap
        if (panTime.TotalMilliseconds < 400)
        {
            var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
            var viewPortHeight = zoomBorder.Bounds.Height;
            double scrollAmount = -viewPortHeight;  // scroll full page
            Point point = e.GetPosition(zoomBorder);

            // If the tap is in the center, toggle the toolbar instead of scrolling
            if (point.Y > viewPortHeight / 3 && point.Y < 2 * viewPortHeight / 3)
            {
                SetToolbarVisibility(true);
                return;
            }
            
            if (point.Y < viewPortHeight / 2)
                scrollAmount = -scrollAmount;

            // Pan ZoomBorder to the tapped point
            zoomBorder.PanDelta(0, scrollAmount);
        }
        e.Handled = true;
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
            PanInYDirectionOnly();
            SetToolbarVisibility(false);
            ResizeChildren(new Avalonia.Size(Bounds.Width, Bounds.Height));
            InvalidateVisual();
        }
        else
        {
            // Turn panning off when in drawing or erasing mode, so that the user can draw/erase without panning the page.
            var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1");
            zoomBorder.MinOffsetX = zoomBorder.OffsetX;
            zoomBorder.MaxOffsetX = zoomBorder.OffsetX;
            zoomBorder.MinOffsetY = zoomBorder.OffsetY;
            zoomBorder.MaxOffsetY = zoomBorder.OffsetY; 
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
    /// Allows panning in the Y direction only.
    /// </summary>
    private void PanInYDirectionOnly()
    {
        var zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder1"); 
        double maximumHeight = (MusicCanvas.Children.Count-1) * Bounds.Height;       
        zoomBorder.MinOffsetX = 0;
        zoomBorder.MaxOffsetX = 0;
        zoomBorder.MinOffsetY = -maximumHeight;
        zoomBorder.MaxOffsetY = 0;
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
