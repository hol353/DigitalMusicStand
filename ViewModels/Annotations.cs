using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Avalonia;
using Avalonia.Skia;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Primitive;
using SkiaSharp;

namespace Coda;

/// <summary>
/// Represents all annotations for a file.
/// </summary>
/// <param name="Model">The model instance.</param>
public class Annotations(MainViewModel Model)
{
    /// <summary>
    /// Get annotations for a page.
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    /// <returns>List of annotations.</returns>
    public IReadOnlyList<SkiaStroke> Get(int pageNumber)
    {
        var pdfRelativeDirectory = Path.GetDirectoryName(Model.PdfFilePath).ToRelative(Model.Settings.MusicLibraryBaseDirectory);
        var pdfFileName = Path.GetFileNameWithoutExtension(Model.PdfFilePath);
        var svgFileName = Path.Combine(Model.BaseDirectory, "Annotations", pdfRelativeDirectory, $"{pdfFileName}.{pageNumber}.svg");
        List<SkiaStroke> strokes = new();
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

                    strokes.Add(SkiaStroke.CreateStaticStroke(InkId.NewId(), skPath, stylusPointListSpan, color, 0.1f, true, inkStrokeRenderer: null));
                }
            }
        }
        return strokes;
    }    

    /// <summary>
    /// Save annotations for all pages.
    /// </summary>
    /// <param name="bounds">The page bounds.</param>
    /// <param name="pages">The annotations for each page.</param>
    public void Save(Rect bounds, IEnumerable<IReadOnlyList<SkiaStroke>> pages)
    {
        if (Model.PdfFilePath != null)
        {
            var pdfRelativeDirectory = Path.GetDirectoryName(Model.PdfFilePath).ToRelative(Model.Settings.MusicLibraryBaseDirectory);
            var pdfFileName = Path.GetFileNameWithoutExtension(Model.PdfFilePath);
            var annotationsBaseDirectory = Path.Combine(Model.BaseDirectory, "Annotations");
            var annotationPdfDirectory = Path.Combine(annotationsBaseDirectory, pdfRelativeDirectory);
            Directory.CreateDirectory(annotationPdfDirectory);

            foreach (var oldFile in Directory.GetFiles(annotationPdfDirectory, $"{pdfFileName}.*.svg"))
                File.Delete(oldFile);

            using var skPaint = new SKPaint();
            skPaint.IsAntialias = true;
            skPaint.Style = SKPaintStyle.Fill;

            int pageNumber = 1;
            foreach (var strokes in pages)
            {
                if (strokes.Any())
                {
                    var saveSvgFile = Path.Combine(annotationPdfDirectory, $"{pdfFileName}.{pageNumber}.svg");
                    using var fileStream = File.Create(saveSvgFile);
                    using var skCanvas = SKSvgCanvas.Create(bounds.ToSKRect(), fileStream);

                    for (var i = 0; i < strokes.Count; i++)
                    {
                        var stroke = strokes[i];
                        skPaint.Color = stroke.Color;
                        skCanvas.DrawPath(stroke.Path, skPaint);
                    }
                }
                pageNumber++;
            }
        }
    }
}