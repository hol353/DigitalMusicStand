using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Skia;
using AvaloniaInkCanvasDemo.ViewModels;
using DotNetCampus.Inking;
using SkiaSharp;

namespace AvaloniaInkCanvasDemo.Views;

public partial class MainView : UserControl
{
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
        {
            inkCanvas.EditingMode = mode;
        }
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
        var storageFolder = await storageProvider.TryGetFolderFromPathAsync(model.Settings.MusicLibraryBaseDirectory);
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF File",
            
            SuggestedStartLocation = storageFolder,
            FileTypeFilter = new[] { new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } } }
        });

        if (result.Count > 0)
        {
            var pdfFilePath = result[0].Path.LocalPath;
            model.LoadPdf(pdfFilePath);
            MusicCanvas.Children.Clear();
            MusicCanvas.Children.AddRange(model.canvases);
        }
    }
}
