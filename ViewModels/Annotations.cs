using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Skia;
using SkiaSharp;

namespace BlackFolder;

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
    public SVG Get(int pageNumber)
    {
        var pdfRelativeDirectory = Path.GetDirectoryName(Model.PdfFilePath).ToRelative(Model.Settings.MusicLibraryBaseDirectory);
        var pdfFileName = Path.GetFileNameWithoutExtension(Model.PdfFilePath);
        var svgFileName = Path.Combine(Model.BaseDirectory, "Annotations", pdfRelativeDirectory, $"{pdfFileName}.{pageNumber}.svg");
        if (File.Exists(svgFileName))
            return new SVG(svgFileName);
        return
            null;
    }    

    /// <summary>
    /// Save annotations for all pages.
    /// </summary>
    /// <param name="bounds">The page bounds.</param>
    /// <param name="pages">The annotations for each page.</param>
    public void Save(IEnumerable<SheetMusicControl> pages)
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
            foreach (var page in pages)
            {
                if (page.Strokes.Any())
                {
                    var saveSvgFile = Path.Combine(annotationPdfDirectory, $"{pdfFileName}.{pageNumber}.svg");
                    using var fileStream = File.Create(saveSvgFile);
                    using var skCanvas = SKSvgCanvas.Create(page.Bounds.ToSKRect(), fileStream);

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
}