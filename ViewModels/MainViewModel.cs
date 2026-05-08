using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Skia.Helpers;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Primitive;
using DynamicData;
using SkiaSharp;

namespace AvaloniaInkCanvasDemo.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        SolidColorBrushCollection =
        [
            Brushes.Red,
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

    public void LoadAnnotations(string PdfFilePath, int pageNumber, InkCanvas canvas)
    {
        var pdfDirectory = Path.GetDirectoryName(PdfFilePath);
        var pdfRelativeDirectory = pdfDirectory.Replace(Settings.MusicLibraryBaseDirectory, "").TrimStart("/").ToString();
        var pdfFileName = Path.GetFileNameWithoutExtension(PdfFilePath);
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

                    var stroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), skPath, stylusPointListSpan, color, 0.1f, true, inkStrokeRenderer:null);
                    
                    canvas.AvaloniaSkiaInkCanvas.AddStaticStroke(stroke);
                }
            }
        }
    }

    public void SaveAnnotations(string PdfFilePath, IEnumerable<InkCanvas> pages)
    {
        var pdfDirectory = Path.GetDirectoryName(PdfFilePath);
        var pdfRelativeDirectory = pdfDirectory.Replace(Settings.MusicLibraryBaseDirectory, "").TrimStart("/").ToString();
        var pdfFileName = Path.GetFileNameWithoutExtension(PdfFilePath);
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
