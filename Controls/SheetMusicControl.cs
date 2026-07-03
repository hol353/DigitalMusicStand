using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Contexts;
using DotNetCampus.Inking.Primitive;
using SkiaSharp;

namespace BlackFolder;

/// <summary>
/// Encapsulates a user control that shows a page of music and allows annotations.
/// </summary>
public class SheetMusicControl : InkCanvas
{
    /// <summary>The music page to show.</summary>
    private MusicPage page;

    /// <summary>The main view.</summary>
    private MainView mainView;

    /// <summary>The size of the control.</summary>
    private Size actualSize;

    /// <summary>The current vertical offset from the top edge of the viewport.</summary>
    private Rect renderRectangle;

    /// <summary>The time when the user started panning. Used to determine if a tap is a pan or a single tap.</summary>
    private DateTime panStartTime = DateTime.MinValue;

    /// <summary>The time the user spent panning. Used to determine if a tap is a pan or a single tap.</summary>
    private TimeSpan panTime;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="image">The music bitmap to show.</param>
    /// <param name="svg">The annotations from the .svg to show.</param>
    public SheetMusicControl(MainView mainView, MusicPage page)
    {
        this.page = page;
        this.mainView = mainView;
        if (page.Annotations != null)
        {
            List<InkStylusPoint> points = new();
            StylusPointListSpan stylusPointListSpan = new(points, 0, points.Count);
            foreach (var annotation in page.Annotations.Paths)
            {
                var stroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), annotation.Path, stylusPointListSpan, annotation.Color, 0.1f, true, inkStrokeRenderer: null);
                AvaloniaSkiaInkCanvas.AddStaticStroke(stroke);              
            }
        }
    }

    /// <summary>
    /// Called when the control is attached to the visual tree.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.StrokeCollected += OnStrokeCollected;
        this.StrokeErased += OnStrokeErased;
        this.Tapped += OnSingleTap;
        var zoomBorder = this.FindLogicalAncestorOfType<ZoomBorder>();
        zoomBorder.PanStarted += (s, e) => panStartTime = DateTime.Now;
        zoomBorder.PanEnded += (s, e) => panTime = DateTime.Now - panStartTime;
    }

    /// <summary>
    /// Called when the control is detached from the visual tree.
    /// </summary>
    /// <param name="e"></param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        this.StrokeCollected -= OnStrokeCollected;
        this.StrokeErased -= OnStrokeErased;
        this.Tapped -= OnSingleTap;
    }

    /// <summary>
    /// Called when a stroke is collected. Saves the current annotations to the .svg file.
    /// </summary>
    private void OnStrokeCollected(object sender, AvaloniaSkiaInkCanvasStrokeCollectedEventArgs e)
    {
        SaveStrokes();
    }

    /// <summary>
    /// Called when a stroke is erased. Saves the current annotations to the .svg file.
    /// </summary>
    private void OnStrokeErased(object sender, ErasingCompletedEventArgs e)
    {
        SaveStrokes();
    }

    /// <summary>
    /// Saves the current annotations to the .svg file.
    /// </summary>
    private void SaveStrokes()
    {
        var bounds = new SKRect(0, 0, (float)actualSize.Width, (float)actualSize.Height);
        var newAnnotations = Annotations.Create(bounds, AvaloniaSkiaInkCanvas.StaticStrokeList);
        page.Annotations = newAnnotations;
    }

    /// <summary>
    /// Handles single-tap gestures.
    /// </summary>
    private void OnSingleTap(object sender, TappedEventArgs e)
    {
        // detect if the user is currently panning, and if so, ignore the tap
        if (panTime.TotalMilliseconds < 400)
        {
            var scrollViewer = this.FindLogicalAncestorOfType<ScrollViewer>();
            var zoomBorder = this.FindLogicalAncestorOfType<ZoomBorder>();
            var viewPortHeight = zoomBorder.Bounds.Height;
            double scrollAmount = -viewPortHeight;  // scroll full page
            Point point = e.GetPosition(scrollViewer);

            // If the tap is in the top-left corner, toggle the toolbar instead of scrolling
            if (point.Y < 50 && point.X < Bounds.Left + 50)
            {
                mainView.SetToolbarVisibility(true);
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
    /// Called by the layout panel to determine what size the control needs.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The size the control wants.</returns>
    protected override Size MeasureCore(Size availableSize)
    {
        var zoomBorder = this.FindLogicalAncestorOfType<ZoomBorder>();

        var width = availableSize.Width;
        var height = zoomBorder.Bounds.Height;
        if (page.Bitmap != null)
        {
            double imageAspectRatio = page.Bitmap.Size.Height / page.Bitmap.Size.Width;
            width = height / imageAspectRatio;
        }

        // Calculate the actual size of the control based on the scale factor.
        actualSize = new Size(width, height);

        double margin = 0;
        renderRectangle = new Rect(new Point(0, 0), new Size(actualSize.Width, actualSize.Height - margin));

        base.MeasureCore(actualSize);

        return actualSize;
    }

    /// <summary>
    /// Called when positioning child elements as part of a layout pass.
    /// </summary>
    /// <param name="finalSize">The size available to the control.</param>
    /// <returns>The actual size used by the control.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        return base.ArrangeOverride(actualSize);
    }

    /// <summary>
    /// Render the control by drawing the bitmap.
    /// </summary>
    /// <param name="context">The drawing context to draw on.</param>
    public override void Render(DrawingContext context)
    {
        if (page.Bitmap != null)
        {
            var pen = new Pen(Brushes.Red, 2);
            context.DrawRectangle(Brushes.White, pen, renderRectangle);
            context.DrawImage(page.Bitmap, new Rect(0, 0, page.Bitmap.Size.Width, page.Bitmap.Size.Height), renderRectangle);
        }
        base.Render(context);
    }
}