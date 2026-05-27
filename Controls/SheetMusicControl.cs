
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DotNetCampus.Inking;

namespace BlackFolder;

public class SheetMusicControl : InkCanvas
{
    public Bitmap Image { get; set; }


    protected override Size MeasureCore(Size availableSize)
    {
        var width = availableSize.Width;
        var height = availableSize.Height;

        var mainWindow = this?.Parent?.Parent?.Parent?.Parent?.Parent as Window;

        // Get the top panel.
        var grid = this.FindLogicalAncestorOfType<Grid>();
        var topPanel = grid?.FindControl<StackPanel>("Toolbar");
        if (grid == null || topPanel == null)
            throw new Exception("Cannot find grid and/or top panel");

        if (mainWindow != null && Image != null)
        {
            double imageAspectRatio = Image.Size.Height / Image.Size.Width;
            height = mainWindow.Height - 50; // magic number representing height of toolbar.
            width = height / imageAspectRatio;
        }

        if (double.IsInfinity(width))
        {
            width = 0;
        }

        if (double.IsInfinity(height))
        {
            height = 0;
        }

        var s = new Size(width, height);
        base.MeasureCore(s);
        return s;
    }    


    public override void Render(DrawingContext context)
    {
        var rectangle = new Rect(new Point(), Bounds.Size);

        if (Image != null)
        {
            double imageAspectRatio = Image.Size.Height / Image.Size.Width;
            double height = Bounds.Size.Height;
            double width = height / imageAspectRatio;
            double left = (Bounds.Size.Width - width) / 2;
            Rect destRectangle = new Rect(left, 0, width, height);

            if (Image != null)
                context.DrawImage(Image, new Rect(0, 0, Image.Size.Width, Image.Size.Height), destRectangle);            
        }
            
        base.Render(context);
    }    
}