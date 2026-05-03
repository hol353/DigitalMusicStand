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

    private string PdfFilePath;

    public MainView()
    {
        InitializeComponent();
    }

    private void PenModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        SetInkCanvasEditingMode(InkCanvasEditingMode.Ink);
    }

    private void EraserModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        SetInkCanvasEditingMode(InkCanvasEditingMode.EraseByPoint);
    }

    private void SaveStrokeAsSvgButton_OnClick(object sender, RoutedEventArgs e)
    {
        var baseDirectory = Path.GetDirectoryName(PdfFilePath);
        var baseFileName = Path.GetFileNameWithoutExtension(PdfFilePath);

        foreach (var oldFile in Directory.GetFiles(baseDirectory, $"{baseFileName}.*.svg"))
            File.Delete(oldFile);

        using var skPaint = new SKPaint();
        skPaint.IsAntialias = true;
        skPaint.Style = SKPaintStyle.Fill;

        for (var page = 0; page < MusicCanvas.Children.Count; page++)
        {
            var inkCanvas = MusicCanvas.Children[page] as InkCanvas;

            var saveSvgFile = Path.Combine(baseDirectory, $"{baseFileName}.{page}.svg");
            using var fileStream = File.Create(saveSvgFile);
            var bounds = inkCanvas.Bounds.ToSKRect();
            using var skCanvas = SKSvgCanvas.Create(bounds, fileStream);

            for (var i = 0; i < inkCanvas.Strokes.Count; i++)
            {
                var stroke = inkCanvas.Strokes[i];
                skPaint.Color = stroke.Color;
                skCanvas.DrawPath(stroke.Path, skPaint);
            }
        }
    }

    private void SelectingItemsControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count is 1)
        {
            if (e.AddedItems[0] is ISolidColorBrush brush)
            {
                SetInkCanvasInkColour(brush.Color.ToSKColor());
            }
        }
    }

    private void SetInkCanvasEditingMode(InkCanvasEditingMode mode)
    {
        foreach (InkCanvas inkCanvas in MusicCanvas.Children)
            inkCanvas.EditingMode = mode;
    }


    private void SetInkCanvasInkColour(SKColor colour)
    {
        foreach (InkCanvas inkCanvas in MusicCanvas.Children)
            inkCanvas.AvaloniaSkiaInkCanvas.Settings.InkColor = colour;
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
            PdfFilePath = result[0].Path.LocalPath;
            RenderPdfToView(PdfFilePath);
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

            MusicCanvas.Children.Clear();

            // Convert the bitmap to an Avalonia Bitmap and set it to the MusicImage control
            for (int page = 0; page < document.Pages.Length; page++)
            {
                using var memoryStream = new MemoryStream();
                document.WriteImage(page, 2, PixelFormats.RGBA, memoryStream, RasterOutputFileTypes.PNG, false);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var inkCanvas = new InkCanvas();
                inkCanvas.Image = new Bitmap(memoryStream);
                inkCanvas.AvaloniaSkiaInkCanvas.Settings.EraserViewCreator = new DelegateEraserViewCreator(() => new CustomEraserView());
                MusicCanvas.Children.Add(inkCanvas);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering PDF: {ex.Message}");
        }
    }
}
