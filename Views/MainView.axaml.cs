using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Skia;
using AvaloniaInkCanvasDemo.Views.ErasingView;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Erasing;
using DotNetCampus.Inking.StrokeRenderers.WpfForSkiaInkStrokeRenderers;
using MuPDFCore;
using SkiaSharp;

namespace AvaloniaInkCanvasDemo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        var settings = InkCanvas.AvaloniaSkiaInkCanvas.Settings;
        settings.EraserViewCreator = new DelegateEraserViewCreator(() => new CustomEraserView());
    }

    private void PenModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        InkCanvas.EditingMode = InkCanvasEditingMode.Ink;
    }

    private void EraserModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        InkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
    }

    private void SwitchRendererButton_OnClick(object sender, RoutedEventArgs e)
    {
        var settings = InkCanvas.AvaloniaSkiaInkCanvas.Settings;

        if (settings.InkStrokeRenderer is null)
        {
            settings.InkStrokeRenderer = new WpfForSkiaInkStrokeRenderer();
        }
        else
        {
            settings.InkStrokeRenderer = null;
        }
    }

    private void SaveStrokeAsSvgButton_OnClick(object sender, RoutedEventArgs e)
    {
        var saveFolder = Path.Join(AppContext.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
        Directory.CreateDirectory(saveFolder);

        using var skPaint = new SKPaint();
        skPaint.IsAntialias = true;
        skPaint.Style = SKPaintStyle.Fill;

        for (var i = 0; i < InkCanvas.Strokes.Count; i++)
        {
            var saveSvgFile = Path.Join(saveFolder, $"{i}.svg");
            using var fileStream = File.Create(saveSvgFile);

            var stroke = InkCanvas.Strokes[i];

            var bounds = InkCanvas.Bounds.ToSKRect();
            using var skCanvas = SKSvgCanvas.Create(bounds, fileStream);

            skPaint.Color = stroke.Color;
            skCanvas.DrawPath(stroke.Path, skPaint);
        }
    }

    private void SelectingItemsControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count is 1)
        {
            if (e.AddedItems[0] is ISolidColorBrush brush)
            {
                InkCanvas.AvaloniaSkiaInkCanvas.Settings.InkColor = brush.Color.ToSKColor();
            }
        }
    }

    private async void OpenPdfButton_OnClick(object sender, RoutedEventArgs e)
    {
        var storageProvider = ((Window)this.VisualRoot).StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF File",
            FileTypeFilter = new[] { new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } } }
        });

        if (result.Count > 0)
        {
            var pdfFilePath = result[0].Path.LocalPath;
            RenderPdfToView(pdfFilePath);
        }
    }

    private void RenderPdfToView(string pdfFilePath)
    {
        try
        {
            //Initialise the MuPDF context. This is needed to open or create documents.
            using MuPDFContext ctx = new MuPDFContext();

            //Open a PDF document
            using MuPDFDocument document = new MuPDFDocument(ctx, pdfFilePath);

            // Convert the bitmap to an Avalonia Bitmap and set it to the MusicImage control
            using var memoryStream = new MemoryStream();
            document.WriteImage(0, 2, PixelFormats.RGBA, memoryStream, RasterOutputFileTypes.PNG, false);
            memoryStream.Seek(0, SeekOrigin.Begin);
            
            var avaloniaBitmap = new Bitmap(memoryStream);
            MusicImage.Source = avaloniaBitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering PDF: {ex.Message}");
        }
    }    
}
