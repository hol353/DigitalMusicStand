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
        this.SizeChanged += OnSizeChanged;
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
        this.SizeChanged -= OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Transform the existing strokes to fit the new size of the control.
        var oldSize = e.PreviousSize;
        var newSize = e.NewSize;
        if (oldSize.Width > 0 && oldSize.Height > 0)
        {
            var scaleX = newSize.Width / oldSize.Width;
            var scaleY = newSize.Height / oldSize.Height;
            if (scaleX != 1 || scaleY != 1)
            {
                SKMatrix translation;
                //if (scale == 1)
                    translation = SKMatrix.CreateScale((float)scaleX, (float)scaleY);
                //else
                //    translation = SKMatrix.CreateScaleTranslation((float)scaleX, (float)scaleY,
                //                                                  (float)offsetX, (float)offsetY);
                foreach (var stroke in AvaloniaSkiaInkCanvas.StaticStrokeList)
                    stroke.SetTransform(translation);
            }
        }
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
            if (width > availableSize.Width)
            {
                width = availableSize.Width;
                height = width * imageAspectRatio;
            }
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
        AvaloniaSkiaInkCanvas.Render(context);
    }
}