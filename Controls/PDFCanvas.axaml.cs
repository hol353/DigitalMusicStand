using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.VisualTree;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Erasing;
using SkiaSharp;

namespace BlackFolder;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class PDFCanvas : UserControl
{
    private ZoomBorder zoomBorder;
    private StackPanel musicCanvas;
    private MusicLibrary musicLibrary;

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
    /// Load multiple pdf files.
    /// </summary>
    /// <param name="pdfFilePaths">The paths to the pdf files.</param>
    public void Load(MusicLibrary musicLibrary, IEnumerable<string> pdfFilePaths)
    {
        this.musicLibrary = musicLibrary;
        musicLibrary.CloseAll();
        musicCanvas.Children.Clear();
        foreach (var pdfFilePath in pdfFilePaths)
            Load(pdfFilePath);
        AnnotateMode = InkCanvasEditingMode.None;
    }


    /// <summary>
    /// Invoked when the user taps in the center of the screen.
    /// </summary>
    public event EventHandler CentreTap;

    /// <summary>
    /// Gets or sets the current editing mode of the InkCanvas.
    /// </summary>
    public InkCanvasEditingMode AnnotateMode
    {
        get
        {
            if (musicCanvas.Children.Count > 0)
            {
                var inkCanvas = musicCanvas.Children[0] as InkCanvas;
                return inkCanvas.EditingMode;
            }
            return InkCanvasEditingMode.None;
        }
        set
        {
            foreach (InkCanvas inkCanvas in musicCanvas.Children)
                inkCanvas.EditingMode = value;

            /*if (value == InkCanvasEditingMode.None && musicCanvas.Children.Count > 0)
            {
                PanInYDirectionOnly();
                ResizeChildren(new Avalonia.Size(Bounds.Width, Bounds.Height));
                InvalidateVisual();
            }
            else
            {
                // Turn panning off when in drawing or erasing mode, so that the user can draw/erase without panning the page.
                zoomBorder.MinOffsetX = zoomBorder.OffsetX;
                zoomBorder.MaxOffsetX = zoomBorder.OffsetX;
                zoomBorder.MinOffsetY = zoomBorder.OffsetY;
                zoomBorder.MaxOffsetY = zoomBorder.OffsetY; 
            }*/
        }
    }
    
    /// <summary>
    /// Sets the annotation colour
    /// </summary>
    /// <param name="colour">The ink colour</param>
    public void SetAnnotateColour(SKColor colour)
    {
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
        var model = DataContext as MainViewModel;
        model.PdfFilePath = pdfFilePath;

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
        if (AnnotateMode == InkCanvasEditingMode.None)
        {
            panStartTime = DateTime.Now;
            if (Math.Round(zoomBorder.ZoomX, 2) == 1 && Math.Round(zoomBorder.ZoomY, 2) == 1)  // no zoom applied
                PanInYDirectionOnly();
        }
    }

    /// <summary>
    /// Invoked when the user ends panning. Records the time spent panning to determine if a tap is a pan or a single tap.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPanEnded(object sender, PanEventArgs e)
    {
        if (AnnotateMode == InkCanvasEditingMode.None)
            panTime = DateTime.Now - panStartTime;
    }

    /// <summary>
    /// Invoked when the user starts zooming. Sets the min and max offsets for the ZoomBorder based on the current zoom level.
    /// </summary>
    private void OnZoomStarted(object sender, ZoomEventArgs e)
    {
        if (Math.Round(zoomBorder.ZoomX, 2) > 1 || Math.Round(zoomBorder.ZoomY, 2) > 1)
        {
            zoomBorder.MinOffsetX = -Bounds.Width;
            zoomBorder.MaxOffsetX = Bounds.Width;
            zoomBorder.MinOffsetY = -Bounds.Height;
            zoomBorder.MaxOffsetY = Bounds.Height;
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
            var viewPortHeight = zoomBorder.Bounds.Height;
            double scrollAmount = -viewPortHeight;  // scroll full page
            Point point = e.GetPosition(zoomBorder);

            // If the tap is in the center, toggle the toolbar instead of scrolling
            if (point.Y > viewPortHeight / 3 && point.Y < 2 * viewPortHeight / 3)
            {
                AnnotateMode = InkCanvasEditingMode.None;
                CentreTap.Invoke(this, EventArgs.Empty);
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