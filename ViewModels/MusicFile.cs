using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using MuPDFCore;

namespace BlackFolder;

public class MusicFile
{
    /// <summary>
    /// The path of the file.
    /// </summary>
    private string path;
    private MainViewModel model;


    /// <summary>
    /// Initializes a new instance of the <see cref="MusicFile"/> class.
    /// </summary>
    /// <param name="path"></param>
    public MusicFile(MainViewModel model, string path)
    {
        this.path = path;
        this.model = model;
        Load(path);
    }

    /// <summary>
    /// The list of pages in the file.
    /// </summary>
    public List<MusicPage> Pages { get; } = new();   

    /// <summary>
    /// Called when the music file is closed.
    /// </summary>
    public void Save()
    {
        foreach ( var page in Pages)
            page.Save();
    }

    /// <summary>
    /// Load a pdf file.
    /// </summary>
    /// <param name="pdfFilePath">The path of the pdf file.</param>
    private void Load(string pdfFilePath)
    {
        if (File.Exists(pdfFilePath))
        {
            // Initialise the MuPDF context. This is needed to open or create documents.
            using MuPDFContext ctx = new MuPDFContext();

            // Open a PDF document
            using MuPDFDocument document = new MuPDFDocument(ctx, pdfFilePath);

            // Convert each page of the PDf to a MusicPage instance.
            for (int page = 0; page < document.Pages.Length; page++)
            {
                using var memoryStream = new MemoryStream();
                document.WriteImage(page, 1.5, PixelFormats.RGBA, memoryStream, RasterOutputFileTypes.PNG, false);
                memoryStream.Seek(0, SeekOrigin.Begin);
                Pages.Add(new MusicPage(model, pdfFilePath, new Bitmap(memoryStream), page + 1));
            }
        }
    }    
}
