using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Avalonia.Skia.Helpers;
using AvaloniaInkCanvasDemo.Views.ErasingView;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Erasing;
using DotNetCampus.Inking.Primitive;
using DynamicData;
using MuPDFCore;
using SkiaSharp;

namespace AvaloniaInkCanvasDemo.ViewModels;

public class MainViewModel : ViewModelBase
{
    private List<SheetMusicControl> pages = new();
    private string pdfFilePath;

    public MainViewModel()
    {
        SolidColorBrushCollection =
        [
            Brushes.Red,
            Brushes.Yellow,
            Brushes.Black,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Orange,
            Brushes.Purple
        ];
        Settings = new();
    }

    public ObservableCollection<IBrush> SolidColorBrushCollection { get; }

    public string BaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DigitalSheetMusic");

    public SettingsModel Settings { get; }

    public IReadOnlyList<SheetMusicControl> canvases => pages;

    public void LoadPdf(string pdfFilePath)
    {
        pages.Clear();
        this.pdfFilePath = pdfFilePath;

        try
        {
            //Initialise the MuPDF context. This is needed to open or create documents.
            using MuPDFContext ctx = new MuPDFContext();

            //Open a PDF document
            using MuPDFDocument document = new MuPDFDocument(ctx, pdfFilePath);

            // Convert the bitmap to an Avalonia Bitmap and set it to the MusicImage control
            for (int page = 0; page < document.Pages.Length; page++)
            {
                using var memoryStream = new MemoryStream();
                document.WriteImage(page, 2, PixelFormats.RGBA, memoryStream, RasterOutputFileTypes.PNG, false);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var sheetMusicControl = new SheetMusicControl();
                sheetMusicControl.Image = new Bitmap(memoryStream);
                sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.EraserViewCreator = new DelegateEraserViewCreator(() => new CustomEraserView());
                sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.InkThickness = 2;

                LoadAnnotations(page + 1, sheetMusicControl);
                pages.Add(sheetMusicControl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering PDF: {ex.Message}");
        }
    }

    public void SaveAnnotations()
    {
        if (pdfFilePath != null)
        {
            var pdfDirectory = Path.GetDirectoryName(pdfFilePath);
            var pdfRelativeDirectory = pdfDirectory.Replace(Settings.MusicLibraryBaseDirectory, "").TrimStart("/").ToString();
            var pdfFileName = Path.GetFileNameWithoutExtension(pdfFilePath);
            var annotationsBaseDirectory = Path.Combine(BaseDirectory, "Annotations");
            var annotationPdfDirectory = Path.Combine(annotationsBaseDirectory, pdfRelativeDirectory);
            Directory.CreateDirectory(annotationPdfDirectory);

            foreach (var oldFile in Directory.GetFiles(annotationPdfDirectory, $"{pdfFileName}.*.svg"))
                File.Delete(oldFile);

            using var skPaint = new SKPaint();
            skPaint.IsAntialias = true;
            skPaint.Style = SKPaintStyle.Fill;

            int pageNumber = 1;
            foreach (var page in pages)
            {
                if (page.Strokes.Any())
                {
                    var saveSvgFile = Path.Combine(annotationPdfDirectory, $"{pdfFileName}.{pageNumber}.svg");
                    using var fileStream = File.Create(saveSvgFile);
                    var bounds = page.Bounds.ToSKRect();
                    using var skCanvas = SKSvgCanvas.Create(bounds, fileStream);

                    for (var i = 0; i < page.Strokes.Count; i++)
                    {
                        var stroke = page.Strokes[i];
                        skPaint.Color = stroke.Color;
                        skCanvas.DrawPath(stroke.Path, skPaint);
                    }
                }

                pageNumber++;
            }
        }
    }    

    private void LoadAnnotations(int pageNumber, InkCanvas canvas)
    {
        var pdfDirectory = Path.GetDirectoryName(pdfFilePath);
        var pdfRelativeDirectory = pdfDirectory.Replace(Settings.MusicLibraryBaseDirectory, "").TrimStart("/").ToString();
        var pdfFileName = Path.GetFileNameWithoutExtension(pdfFilePath);
        var svgFileName = Path.Combine(BaseDirectory, "Annotations", pdfRelativeDirectory, $"{pdfFileName}.{pageNumber}.svg");
        if (File.Exists(svgFileName))
        {
            XmlDocument svg = new XmlDocument();
            svg.Load(svgFileName);
            var paths = svg.GetElementsByTagName("path");
            foreach (XmlNode pathNode in paths)
            {
                if (pathNode.Attributes["d"] != null)
                {
                    string colorName = pathNode.Attributes["fill"].Value;
                    var fieldInfo = typeof(SKColors).GetFields(BindingFlags.Static | BindingFlags.Public)
                                                    .First(fieldInfo => fieldInfo.Name.ToLower() == colorName);
                    var color = (SKColor)fieldInfo.GetValue(null);

                    var pathData = pathNode.Attributes["d"].Value;
                    var skPath = SKPath.ParseSvgPathData(pathData);

                    List<InkStylusPoint> points = new();
                    foreach (var p in points)
                    {
                        points.Add(new InkStylusPoint(p.X, p.Y));
                    }

                    StylusPointListSpan stylusPointListSpan = new(points, 0, points.Count);

                    var stroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), skPath, stylusPointListSpan, color, 0.1f, true, inkStrokeRenderer: null);

                    canvas.AvaloniaSkiaInkCanvas.AddStaticStroke(stroke);
                }
            }
        }
    }

}
