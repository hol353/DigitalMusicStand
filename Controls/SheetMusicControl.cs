using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
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
        actualSize = new Size(width, height);
        base.MeasureCore(actualSize);

        var paths = svg?.Paths;
        if (paths != null)
        {
            // Determine scaling factor because the strokes may have been made on different bounds to the canvas.
            double scaleX = actualSize.Width / svg.Width;
            double scaleY = actualSize.Height / svg.Height;
            if (scaleX != 1 || scaleY != 1)
            {
                var translation = SKMatrix.CreateScale((float)scaleX, (float)scaleY);
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
        var rectangle = new Rect(new Point(), Bounds.Size);

        if (image != null)
            context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), rectangle);
        base.Render(context);
    }
}