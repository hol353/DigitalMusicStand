using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace BlackFolder;

public class MusicPage
{
    private int pageNumber;
    private MainViewModel model;
    private string fileName;
    

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicPage"/> class.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="fileName"></param>
    /// <param name="bitmap"></param>
    /// <param name="pageNumber"></param>
    public MusicPage(MainViewModel model, string fileName, Bitmap bitmap, int pageNumber)
    {
        this.Bitmap = bitmap;
        this.pageNumber = pageNumber;
        this.fileName = fileName;
        this.model = model;
        Annotations = Annotations.Create(model, fileName, pageNumber);
    }

    public Bitmap Bitmap { get; }

    public Annotations Annotations { get; set; }   

    /// <summary>
    /// Save annotations for all pages.
    /// </summary>
    public void Save()
    {
        if (fileName != null)
            Annotations?.Save(model, fileName, pageNumber);
    }    

}