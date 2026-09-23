using System;
using System.IO;

namespace MusicStand;

public class FileItem
{
    private readonly string basePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileItem"/> class.
    /// </summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="basePath">Base path for relative path calculation.</param>
    public FileItem(string path, string basePath)
    {
        AbsolutePath = path;
        this.basePath = basePath;
    }

    /// <summary>
    /// Gets the relative path of the file with respect to the base path.
    /// </summary>
    public string AbsolutePath { get; }
    
    /// <summary>
    /// Gets the relative path of the file with respect to the base path.
    /// </summary>
    public string RelativePath => AbsolutePath.ToRelative(basePath);

    /// <summary>
    /// Gets the file name without extension.
    /// </summary>
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(AbsolutePath);

    /// <summary>
    /// Gets the file name without leading 'The ' or 'A ' for sorting purposes.
    /// </summary>
    public string FileNameForSorting
    {
        get
        {
            string fileName = Path.GetFileName(AbsolutePath);
            if (fileName.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
                return fileName[4..];

            if (fileName.StartsWith("A ", StringComparison.OrdinalIgnoreCase))
                return fileName[2..];

            return fileName;
        }
    }

}