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

    /// <summary>The current scale factor for pinch zoom. Ranges from 1.0-3.0</summary>
    private double scale = 1.0;

    /// <summary>The current horizontal offset from the left edge of the viewport.</summary>
    private double offsetX;

    /// <summary>The current vertical offset from the top edge of the viewport.</summary>
    private double offsetY;

    /// <summary>The current vertical offset from the top edge of the viewport.</summary>
    private Rect renderRectangle;

    private Point scaleOrigin;
    
    /// <summary>Tracks the last pan position for mouse/touch dragging.</summary>
    private Point lastPanPosition;
    
    /// <summary>Indicates if panning is currently active.</summary>
    private bool isPanning;
    
    /// <summary>Additional manual pan offset applied during dragging.</summary>
    private Point manualPanOffset;

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
        this.Tapped += OnSingleTap;
        this.DoubleTapped += OnDoubleTap;
        this.PointerPressed += OnPointerPressed;
        this.PointerMoved += OnPointerMoved;
        this.PointerReleased += OnPointerReleased;
        this.StrokeCollected += OnStrokeCollected;
        this.StrokeErased += OnStrokeErased;

        var pinchZoomRecognizer = new PinchGestureRecognizer();
        GestureRecognizers.Add(pinchZoomRecognizer);
        AddHandler(InputElement.PinchEvent, OnPinchZoom);
        AddHandler(InputElement.PinchEndedEvent, OnPinchEndedZoom);
    }

    private void OnStrokeCollected(object sender, AvaloniaSkiaInkCanvasStrokeCollectedEventArgs e)
    {
        SaveStrokes();
    }

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
    /// Handles pointer pressed to start panning.
    /// </summary>
    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (scale > 1.0 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            isPanning = true;
            lastPanPosition = e.GetPosition(this);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved to pan the image.
    /// </summary>
    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (isPanning && scale > 1.0)
        {
            Point currentPosition = e.GetPosition(this);
            double deltaX = currentPosition.X - lastPanPosition.X;
            double deltaY = currentPosition.Y - lastPanPosition.Y;

            manualPanOffset = new Point(manualPanOffset.X + deltaX, manualPanOffset.Y + deltaY);
            lastPanPosition = currentPosition;

            InvalidateMeasure();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer released to stop panning.
    /// </summary>
    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        isPanning = false;
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

            // If the tap is in the top-left corner, toggle the toolbar instead of scrolling
            if (point.Y < 50 && point.X < Bounds.Left + 50)
            {
                mainView.SetToolbarVisibility(true);
                return;
            }
            
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
        singleTapCancellationToken?.Cancel();
        scale = e.Scale;
        scaleOrigin = e.ScaleOrigin;

        scale = (scale - 1) * 0.5 + 1; // Adjust the scale factor to make zooming less sensitive
        // Clamp the scale to prevent excessive zooming.
        scale = Math.Clamp(scale, 1.0, 3.0);

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
        if (page.Bitmap != null)
        {
            double imageAspectRatio = page.Bitmap.Size.Height / page.Bitmap.Size.Width;
            width = height / imageAspectRatio;
        }

        // Calculate the actual size of the control based on the scale factor.
        actualSize = new Size(width * scale, height * scale);

        // Calculate the scale origin relative to the control's size.
        double scaleOriginX = scaleOrigin.X / width;
        double scaleOriginY = scaleOrigin.Y / height;

        // Center the scaled image within the control by offsetting half of the extra size.
        var diffX = (actualSize.Width - width) * scaleOriginX;
        var diffY = (actualSize.Height - height) * scaleOriginY;

        if (manualPanOffset.X > 0 || manualPanOffset.Y > 0)
        {
            offsetX = manualPanOffset.X;
            offsetY = manualPanOffset.Y;
        }
        else
        {
            offsetX = -diffX;
            offsetY = -diffY;
        }

        double margin = 10;
        renderRectangle = new Rect(new Point(offsetX, offsetY), new Size(actualSize.Width, actualSize.Height - margin));

        base.MeasureCore(actualSize);

        var paths = page.Annotations?.Paths;
        if (paths != null)
        {
            // Determine scaling factor because the strokes may have been made on different bounds to the canvas.
            double scaleX = actualSize.Width / page.Annotations.Width;
            double scaleY = actualSize.Height / page.Annotations.Height;

            if (scaleX != 1 || scaleY != 1)
            {
                SKMatrix translation;
                if (scale == 1)
                    translation = SKMatrix.CreateScale((float)scaleX, (float)scaleY);
                else
                    translation = SKMatrix.CreateScaleTranslation((float)scaleX, (float)scaleY,
                                                                  (float)offsetX, (float)offsetY);
                foreach (var stroke in AvaloniaSkiaInkCanvas.StaticStrokeList)
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
        if (page.Bitmap != null)
        {
            //var pen = new Pen(Brushes.Red, 2);
            context.DrawRectangle(Brushes.White, null, renderRectangle);
            context.DrawImage(page.Bitmap, new Rect(0, 0, page.Bitmap.Size.Width, page.Bitmap.Size.Height), renderRectangle);
        }
        base.Render(context);
    }
}