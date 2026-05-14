using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Erasing;
using MuPDFCore;
using SkiaSharp;

namespace Coda;

/// <summary>
/// Represents the main view of the application.
/// </summary>
public partial class MainView : UserControl
{
    /// <summary>
    /// Constructor
    /// </summary>
    public MainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the pen button is clicked.
    /// </summary>
    /// <param name="sender">Sender of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void PenModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        SetInkCanvasEditingMode(InkCanvasEditingMode.Ink);
    }

    /// <summary>
    /// Invoked when the eraser button is clicked.
    /// </summary>
    /// <param name="sender">Sender of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void EraserModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        SetInkCanvasEditingMode(InkCanvasEditingMode.EraseByPoint);
    }

    /// <summary>
    /// Invoked when the open button is clicked.
    /// </summary>
    /// <param name="sender">Sender of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void ToggleOpenButton_OnClick(object sender, RoutedEventArgs e)
    {
        FilePopup.IsOpen = !FilePopup.IsOpen;
        if (FilePopup.IsOpen)
        {
            ComboBox.SelectionChanged += OnComboBoxSelectionChanged;
            ListBox.SelectionChanged += OnListBoxSelectionChanged;
            foreach (Button button in NavigationButtons.Children)
                button.Click += OnNavigationButtonClicked;
        }
        else
        {
            ComboBox.SelectionChanged -= OnComboBoxSelectionChanged;
            ListBox.SelectionChanged -= OnListBoxSelectionChanged;
            foreach (Button button in NavigationButtons.Children)
                button.Click -= OnNavigationButtonClicked;
        }
    }

    /// <summary>
    /// User has clicked a navigation button.
    /// </summary>
    /// <param name="sender">The button clicked.</param>
    /// <param name="e">Event arguments.</param>
    private void OnNavigationButtonClicked(object sender, RoutedEventArgs e)
    {
        var model = DataContext as MainViewModel;

        var button = sender as Button;
        string letter = button.Content.ToString();
        string selectedFile;
        if (letter == "#")
            selectedFile = model.FileNames.FirstOrDefault(file => Int32.TryParse(file, out int i));
        else
            selectedFile = model.FileNames.FirstOrDefault(file => file.StartsWith(letter, ignoreCase: true, CultureInfo.CurrentCulture));
        if (selectedFile != null)
        {
            ListBox.ScrollIntoView(model.FileNames.Last());
            ListBox.ScrollIntoView(selectedFile);
        }
    }

    /// <summary>
    /// User has selected a file to open in the treeview
    /// </summary>
    /// <param name="sender">The treeview</param>
    /// <param name="e">The event arguments.</param>
    private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var model = DataContext as MainViewModel;

        var comboBox = (ComboBox)sender!;
        var relativeDirectory = comboBox.SelectedItem.ToString();

        model.ReadFiles(relativeDirectory);
    }

    /// <summary>
    /// User has selected a file to open in the treeview
    /// </summary>
    /// <param name="sender">The treeview</param>
    /// <param name="e">The event arguments.</param>
    private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var model = DataContext as MainViewModel;

        var listBox = (ListBox)sender!;
        var relativePdfFilePath = listBox.SelectedItem.ToString();

        var relativeDirectory = ComboBox.SelectedItem.ToString();
        var absoluteDirectory = relativeDirectory.ToAbsolute(model.Settings.MusicLibraryBaseDirectory);

        var absolutePdfFilePath = Path.Combine(absoluteDirectory, relativePdfFilePath) + ".pdf";
        if (File.Exists(absolutePdfFilePath))
        {
            Load(absolutePdfFilePath);
            ToggleOpenButton_OnClick(null, null);
        }
    }


    /// <summary>
    /// Load a pdf file.
    /// </summary>
    /// <param name="pdfFilePath">The path of the pdf file.</param>
    public void Load(string pdfFilePath)
    {
        var model = DataContext as MainViewModel;
        model.PdfFilePath = pdfFilePath;

        try
        {
            //Initialise the MuPDF context. This is needed to open or create documents.
            using MuPDFContext ctx = new MuPDFContext();

            //Open a PDF document
            using MuPDFDocument document = new MuPDFDocument(ctx, pdfFilePath);

            // Convert the bitmap to an Avalonia Bitmap and set it to the MusicImage control
            MusicCanvas.Children.Clear();
            for (int page = 0; page < document.Pages.Length; page++)
            {
                using var memoryStream = new MemoryStream();
                document.WriteImage(page, 2, PixelFormats.RGBA, memoryStream, RasterOutputFileTypes.PNG, false);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var sheetMusicControl = new SheetMusicControl();
                sheetMusicControl.Image = new Bitmap(memoryStream);
                sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.EraserViewCreator = new DelegateEraserViewCreator(() => new CustomEraserView());
                sheetMusicControl.AvaloniaSkiaInkCanvas.Settings.InkThickness = 2;

                var annotations = model.Annotations.Get(page + 1);
                foreach (var annotation in annotations)
                    sheetMusicControl.AvaloniaSkiaInkCanvas.AddStaticStroke(annotation);

                MusicCanvas.Children.Add(sheetMusicControl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Invoked when a color is selected from the drop down.
    /// </summary>
    /// <param name="sender">Sender of the event.</param>
    /// <param name="e">Event arguments.</param>
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

    /// <summary>
    /// Sets the editing mode
    /// </summary>
    /// <param name="mode">Draw vs erase.</param>
    private void SetInkCanvasEditingMode(InkCanvasEditingMode mode)
    {
        foreach (InkCanvas inkCanvas in MusicCanvas.Children)
            inkCanvas.EditingMode = mode;
    }

    /// <summary>
    /// Sets the editing colour
    /// </summary>
    /// <param name="colour">The ink colour</param>
    private void SetInkCanvasInkColour(SKColor colour)
    {
        foreach (InkCanvas inkCanvas in MusicCanvas.Children)
            inkCanvas.AvaloniaSkiaInkCanvas.Settings.InkColor = colour;
    }

    /// <summary>
    /// Application is about to close. Save annotations.
    /// </summary>
    internal void OnClosing()
    {
        var model = DataContext as MainViewModel;
        var pages = MusicCanvas?.Children.Cast<SheetMusicControl>()
                                         .Select(child => child.Strokes);
        if (pages != null)
            model.Annotations.Save(MusicCanvas.Bounds, pages);
    }
   
}
