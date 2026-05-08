using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Skia;
using AvaloniaInkCanvasDemo.ViewModels;
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
        var model = DataContext as MainViewModel;

        model.SaveAnnotations(PdfFilePath, MusicCanvas.Children.Cast<InkCanvas>());
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
        var model = DataContext as MainViewModel;

        var storageProvider = ((Window)this.VisualRoot).StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF File",
            SuggestedFileName = model.Settings.MusicLibraryBaseDirectory,
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
            var model = DataContext as MainViewModel;

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

                model.LoadAnnotations(pdfFilePath, page + 1, inkCanvas);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering PDF: {ex.Message}");
        }
    }
}
