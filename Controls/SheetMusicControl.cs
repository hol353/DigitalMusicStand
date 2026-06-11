using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Primitive;
using SkiaSharp;

namespace BlackFolder;

/// <summary>
/// Encapsulates a user control that shows a page of music and allows annotations.
/// </summary>
public class SheetMusicControl : InkCanvas
{
    /// <summary>The music bitmap to show.</summary>
    private Bitmap image;

    /// <summary>The annotations from the .svg to show.</summary>
    private SVG svg;

    /// <summary>The actual annotations (strokes) to show.</summary>
    private List<SkiaStroke> strokes = new();

    /// <summary>The size of the control.</summary>
    private Size actualSize;

    /// <summary>The current scale factor for pinch zoom. Ranges from 0.5-3.0</summary>
    private double scale = 1.0;

    /// <summary>The current horizontal offset from the left edge of the viewport.</summary>
    private double offsetX;

    /// <summary>The current vertical offset from the top edge of the viewport.</summary>
    private double offsetY;


    /// <summary>The current vertical offset from the top edge of the viewport.</summary>
    private Rect renderRectangle;

    private Point scaleOrigin;
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="image">The music bitmap to show.</param>
    /// <param name="svg">The annotations from the .svg to show.</param>
    public SheetMusicControl(Bitmap image, SVG svg)
    {
        this.image = image;
        this.svg = svg;
        if (svg != null)
        {
            List<InkStylusPoint> points = new();
            StylusPointListSpan stylusPointListSpan = new(points, 0, points.Count);
            foreach (var annotation in svg.Paths)
            {
                var stroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), annotation.Path, stylusPointListSpan, annotation.Color, 0.1f, true, inkStrokeRenderer: null);
                strokes.Add(stroke);
                AvaloniaSkiaInkCanvas.AddStaticStroke(stroke);
            }
        }
        this.Tapped += OnSingleTap;
        this.DoubleTapped += OnDoubleTap;

        var pinchZoomRecognizer = new PinchGestureRecognizer();
        GestureRecognizers.Add(pinchZoomRecognizer);
        AddHandler(InputElement.PinchEvent, OnPinchZoom);
        AddHandler(InputElement.PinchEndedEvent, OnPinchEndedZoom);
    }

    /// <summary>
    /// Handles double-tap gestures.
    /// </summary>
    private CancellationTokenSource singleTapCancellationToken;

    private async void OnSingleTap(object sender, TappedEventArgs e)
    {
        // Cancel any previous single-tap logic if a double-tap is detected
        singleTapCancellationToken?.Cancel();
        singleTapCancellationToken = new CancellationTokenSource();

        try
        {
            // Wait for a short delay to check if a double-tap occurs
            await Task.Delay(300, singleTapCancellationToken.Token); // 300ms is a common double-tap threshold

            // If no double-tap occurs, execute single-tap logic
            var scrollViewer = this.FindLogicalAncestorOfType<ScrollViewer>();
            double scrollAmount = scrollViewer.Viewport.Height;  // scroll full page
            Point point = e.GetPosition(scrollViewer);
            if (point.Y < scrollViewer.Viewport.Height / 2)
                scrollAmount = -scrollAmount;

            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, scrollViewer.Offset.Y + scrollAmount);
            e.Handled = true;
        }
        catch (TaskCanceledException)
        {
            // Double-tap detected, cancel single-tap logic
        }
    }

    /// <summary>
    /// Handles double-tap gestures.
    /// </summary>
    private void OnDoubleTap(object sender, TappedEventArgs e)
    {
        singleTapCancellationToken?.Cancel();
        var scrollViewer = this.FindLogicalAncestorOfType<ScrollViewer>();
        double scrollAmount = scrollViewer.Viewport.Height / 2;  // scroll half page
        Point point = e.GetPosition(scrollViewer);
        if (point.Y < scrollViewer.Viewport.Height / 2)
            scrollAmount = -scrollAmount;

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, scrollViewer.Offset.Y + scrollAmount);
        e.Handled = true;
    }

    /// <summary>
    /// Handles pinch zoom gestures.
    /// </summary>
    public void OnPinchZoom(object sender, PinchEventArgs e)
    {
        scale = e.Scale;
        scaleOrigin = e.ScaleOrigin;

        // Clamp the scale to prevent excessive zooming.
        scale = Math.Clamp(scale, 0.5, 3.0);

        InvalidateMeasure();
        InvalidateVisual();

        e.Handled = true;
    }    

    private void OnPinchEndedZoom(object sender, PinchEndedEventArgs e)
    {
        
    }

    /// <summary>
    /// Called by the layout panel to determine what size the control needs.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The size the control wants.</returns>
    protected override Size MeasureCore(Size availableSize)
    {
        var scrollViewer = this.FindLogicalAncestorOfType<ScrollViewer>();

        var width = availableSize.Width;
        var height = scrollViewer.Bounds.Height;
        if (image != null)
        {
            double imageAspectRatio = image.Size.Height / image.Size.Width;
            width = height / imageAspectRatio;
        }

        actualSize = new Size(width * scale, height * scale);

        // Apply zoom scale
        offsetX = scaleOrigin.X - (scaleOrigin.X * scale);
        offsetY = scaleOrigin.Y - (scaleOrigin.Y * scale);
        double margin = 10;
        renderRectangle = new Rect(new Point(offsetX, offsetY), new Size(width, height - margin));

        base.MeasureCore(actualSize);

        var paths = svg?.Paths;
        if (paths != null)
        {
            // Determine scaling factor because the strokes may have been made on different bounds to the canvas.
            double scaleX = actualSize.Width / svg.Width;
            double scaleY = actualSize.Height / svg.Height;

            if (scaleX != 1 || scaleY != 1)
            {
                SKMatrix translation;
                if (scale == 1)
                    translation = SKMatrix.CreateScale((float)scaleX, (float)scaleY);
                else
                    translation = SKMatrix.CreateScaleTranslation((float)scaleX, (float)scaleY,
                                                                  (float)offsetX, (float)offsetY);
                foreach (var stroke in strokes)
                    stroke.SetTransform(translation);
            }
        }

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
        if (image != null)
        {
            //var pen = new Pen(Brushes.Red, 2);
            context.DrawRectangle(Brushes.White, null, renderRectangle);
            context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), renderRectangle);
        }
        base.Render(context);
    }
}