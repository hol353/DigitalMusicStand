using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace BlackFolder;

public class MusicLibrary
{
    private MainViewModel model;

    /// <summary>
    /// The top-level path of the music library.
    /// </summary>
    public string BasePath { get; set; }

    /// <summary>
    /// The list of directories (relative to BasePath) in the music library.
    /// </summary>
    public List<string> Directories { get; set; } = new();

    /// <summary>
    /// The list of open files in the music library.
    /// </summary>
    public IEnumerable<MusicFile> OpenFiles { get; private set;}
    
    /// <summary>
    /// The list of files in the music library (relative to SelectedDirectory, no extension).
    /// </summary>
    public ObservableCollection<string> RelativeFileNames { get; set; } = new();


    /// <summary>
    /// Initializes a new instance of the <see cref="MusicLibrary"/> class.
    /// </summary>
    /// <param name="settings"></param>
    public MusicLibrary(MainViewModel model)
    {
        this.model = model;
        BasePath = model.Settings.MusicLibraryBaseDirectory;
        ReadDirectories(BasePath);
        ReadFiles();
    }

    /// <summary>
    /// Opens a file in the music library. The file can be either a .pdf or a .txt (setlist) file.
    /// </summary>
    /// <param name="fileName"></param>
    public void Open(string fileName)
    {
        if (File.Exists(fileName))
        {
            List<MusicFile> openFiles = new();
            if (Path.GetExtension(fileName).ToLower() == ".pdf")
                openFiles.Add(new MusicFile(model, fileName));
            else 
            {
                // Handle .txt file case
                string[] fileNames = File.ReadAllLines(fileName);
                foreach (var pdfFileName in fileNames)
                    if (File.Exists(pdfFileName))
                        openFiles.Add(new MusicFile(model, pdfFileName));
            }
            OpenFiles = openFiles;
        }
    }

    /// <summary>
    /// Closes all open files in the music library.
    /// </summary>
    public void CloseAll()
    {
        if (OpenFiles != null)
            foreach (var file in OpenFiles)
                file.Save();
        OpenFiles = null;
    }

    /// <summary>
    /// Read all directories and files.
    /// </summary>
    private void ReadDirectories(string directory)
    {
        foreach (var dir in Directory.GetDirectories(directory).Order())
            Directories.Add(dir.ToRelative(BasePath));
    }
    
    /// <summary>
    /// Read all files in selected directory.
    /// </summary>
    public void ReadFiles()
    {
        var absoluteSelectedDirectory = model.Settings.SelectedDirectory?.ToAbsolute(BasePath);
        if (absoluteSelectedDirectory != null && Directory.Exists(absoluteSelectedDirectory))
        {
            RelativeFileNames.Clear();
            foreach (var file in Directory.GetFiles(absoluteSelectedDirectory).Order())
                RelativeFileNames.Add(Path.GetFileNameWithoutExtension(file)
                                          .ToRelative(absoluteSelectedDirectory));
        }
    }    
}