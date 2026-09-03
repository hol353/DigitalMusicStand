using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Erasing;
using ReactiveUI;
using SkiaSharp;

namespace BlackFolder;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class PDFCanvas : UserControl
{
    private MainViewModel model;
    private ZoomBorder zoomBorder;
    private StackPanel musicCanvas;

    /// <summary>The time when the user started panning. Used to determine if a tap is a pan or a single tap.</summary>
    private DateTime panStartTime = DateTime.MinValue;

    /// <summary>The time the user spent panning. Used to determine if a tap is a pan or a single tap.</summary>
    private TimeSpan panTime;

    /// <summary>
    /// Constructor
    /// </summary>
    public PDFCanvas()
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
        model.WhenAnyValue(x => x.SelectedFile).Subscribe(file => LoadFile(file));
        model.WhenAnyValue(x => x.IsPenMode).Subscribe(isPenMode => SetAnnotateMode(isPenMode ? InkCanvasEditingMode.Ink : InkCanvasEditingMode.None));
        model.WhenAnyValue(x => x.IsEraserMode).Subscribe(isEraserMode => SetAnnotateMode(isEraserMode ? InkCanvasEditingMode.EraseByPoint : InkCanvasEditingMode.None));
        model.WhenAnyValue(x => x.SelectedBrush).Subscribe(brush => SetAnnotateBrush(brush));
    }

    /// <summary>
    /// Load a pdf or txt file.
    /// </summary>
    /// <param name="file"></param>
    private void LoadFile(string file)
    {
        if (file != null)
        {
            // The file can be either .pdf or .txt (setlist).
            var absolutePdfFilePath = Path.Combine(model.Settings.MusicLibraryBaseDirectory, model.SelectedDirectory, file) + ".pdf";
            if (File.Exists(absolutePdfFilePath))
                Load([absolutePdfFilePath]);
            else
            {
                // Handle .txt file case
                var absoluteTxtFilePath = Path.ChangeExtension(absolutePdfFilePath, ".txt");
                if (File.Exists(absoluteTxtFilePath))
                    Load(File.ReadAllLines(absoluteTxtFilePath));
            }
        }
    }

    /// <summary>
    /// Load multiple pdf files.
    /// </summary>
    /// <param name="pdfFilePaths">The paths to the pdf files.</param>
    private void Load(IEnumerable<string> pdfFilePaths)
    {
        model.MusicLibrary.CloseAll();
        musicCanvas.Children.Clear();
        foreach (var pdfFilePath in pdfFilePaths)
            Load(pdfFilePath);
    }

    /// <summary>
    /// Gets or sets the current editing mode of the InkCanvas.
    /// </summary>
    private void SetAnnotateMode(InkCanvasEditingMode mode)
    {
        foreach (InkCanvas inkCanvas in musicCanvas.Children)
            inkCanvas.EditingMode = mode;

        if (mode == InkCanvasEditingMode.None && musicCanvas.Children.Count > 0)
        {
            PanInYDirectionOnly();
            ResizeChildren(new Size(Bounds.Width, Bounds.Height));
            InvalidateVisual();
        }
        else
            PanOff();
    }

    /// <summary>
    /// Sets the annotation brush
    /// </summary>
    /// <param name="colour">The ink colour</param>
    public void SetAnnotateBrush(ISolidColorBrush solidColorBrush)
    {
        SKColor colour = new SKColor(solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B, solidColorBrush.Color.A);
        foreach (InkCanvas inkCanvas in musicCanvas.Children)
            inkCanvas.AvaloniaSkiaInkCanvas.Settings.InkColor = colour;
    }

    /// <summary>
    /// Invoked when the control is attached to the visual tree.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        zoomBorder = this.FindControl<ZoomBorder>("ZoomBorder");
        musicCanvas = this.FindControl<StackPanel>("MusicCanvas");

        zoomBorder.ResizeBehavior = ResizeBehaviorMode.ReapplyStretch;
        zoomBorder.SizeChanged += OnZoomBorderSizeChanged;
        zoomBorder.PanStarted += OnPanStarted;
        zoomBorder.PanEnded += OnPanEnded;
        zoomBorder.ZoomDeltaChanged += (s, e) => OnZoomStarted(null, null);
        this.Tapped += OnSingleTap;
    }

    /// <summary>
    /// Invoked when the control is detached from the visual tree.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        zoomBorder.SizeChanged -= OnZoomBorderSizeChanged;
        zoomBorder.PanStarted -= OnPanStarted;
        zoomBorder.PanEnded -= OnPanEnded;
        this.Tapped -= OnSingleTap;
        base.OnDetachedFromVisualTree(e);
        zoomBorder = null;
    }

    /// <summary>
    /// Load a pdf file.
    /// </summary>
    /// <param name="pdfFilePath">The path of the pdf file.</param>
    private void Load(string pdfFilePath)
    {
        try
        {
            model.MusicLibrary.Open(pdfFilePath);
            
            foreach (var file in model.MusicLibrary.OpenFiles)
            {
                foreach (var page in file.Pages)
                {
                    var sheetMusicControl = new PDFPageCanvas(this, page);
                    sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.EraserViewCreator = new DelegateEraserViewCreator(() => new CustomEraserView());
                    sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.InkThickness = 2;
                    musicCanvas.Children.Add(sheetMusicControl);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering PDF: {ex.Message}");
        }
    }    

    /// <summary>
    /// Invoked when the size of the ZoomBorder changes. Resizes all child controls to fit the new size.
    /// </summary>
    private void OnZoomBorderSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ResizeChildren(e.NewSize);
    }

    /// <summary>
    /// Resizes all child controls to fit the new size of the ZoomBorder.
    /// </summary>
    private void ResizeChildren(Avalonia.Size newSize)
    {
        zoomBorder.Zoom(1.0, Bounds.Width / 2, Bounds.Height / 2);
        foreach (PDFPageCanvas sheetMusicControl in musicCanvas.Children)
        {
            sheetMusicControl.Width = newSize.Width;
            sheetMusicControl.Height = newSize.Height;
        }
    } 

    /// <summary>
    /// Invoked when the user starts panning. Sets the min and max offsets for the ZoomBorder based on the current zoom level and the height of the music canvas.
    /// </summary>
    private void OnPanStarted(object sender, PanEventArgs e)
    {
        if (!model.IsPenMode && !model.IsEraserMode && musicCanvas.Children.Count > 0)
        {
            panStartTime = DateTime.Now;
            if (Math.Round(zoomBorder.ZoomX, 2) == 1 && Math.Round(zoomBorder.ZoomY, 2) == 1)  
                PanInYDirectionOnly(); // No zoom applied. Pan in Y direction only
        }
    }

    /// <summary>
    /// Invoked when the user ends panning. Records the time spent panning to determine if a tap is a pan or a single tap.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPanEnded(object sender, PanEventArgs e)
    {
        if (!model.IsPenMode && !model.IsEraserMode && musicCanvas.Children.Count > 0)
            panTime = DateTime.Now - panStartTime;
    }

    /// <summary>
    /// Invoked when the user starts zooming. Sets the min and max offsets for the ZoomBorder based on the current zoom level.
    /// </summary>
    private void OnZoomStarted(object sender, ZoomEventArgs e)
    {
        if (Math.Round(zoomBorder.ZoomX, 2) > 1 || Math.Round(zoomBorder.ZoomY, 2) > 1)
            PanNormally();
    }

    /// <summary>
    /// Handles single-tap gestures.
    /// </summary>
    private void OnSingleTap(object sender, TappedEventArgs e)
    {
        // detect if the user is currently panning, and if so, ignore the tap
        if (panTime.TotalMilliseconds < 400)
        {
            var viewPortHeight = zoomBorder.Bounds.Height;
            double scrollAmount = -viewPortHeight;  // scroll full page
            Point point = e.GetPosition(zoomBorder);

            // If the tap is in the center, toggle the toolbar instead of scrolling
            if (point.Y > viewPortHeight / 3 && point.Y < 2 * viewPortHeight / 3)
            {
                OnCentreTap();
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
    /// Invoked when the user taps in the center of the screen. 
    /// </summary>
    private void OnCentreTap()
    {
        model.IsToolbarVisible = !model.IsToolbarVisible;
    }

    /// <summary>
    /// Pan normally, allowing panning in both X and Y directions.
    /// </summary>
    private void PanNormally()
    {
        zoomBorder.MinOffsetX = -Bounds.Width;
        zoomBorder.MaxOffsetX = Bounds.Width;
        zoomBorder.MinOffsetY = -Bounds.Height;
        zoomBorder.MaxOffsetY = Bounds.Height;
    }

    /// <summary>
    /// Turn all panning off, so that the user can draw/erase without panning the page.
    /// </summary>
    private void PanOff()
    {
        zoomBorder.MinOffsetX = zoomBorder.OffsetX;
        zoomBorder.MaxOffsetX = zoomBorder.OffsetX;
        zoomBorder.MinOffsetY = zoomBorder.OffsetY;
        zoomBorder.MaxOffsetY = zoomBorder.OffsetY;
    }
    
    /// <summary>
    /// Allows panning in the Y direction only.
    /// </summary>
    private void PanInYDirectionOnly()
    {
        double maximumHeight = (musicCanvas.Children.Count-1) * Bounds.Height;       
        zoomBorder.MinOffsetX = 0;
        zoomBorder.MaxOffsetX = 0;
        zoomBorder.MinOffsetY = -maximumHeight;
        zoomBorder.MaxOffsetY = 0;
    }      
}