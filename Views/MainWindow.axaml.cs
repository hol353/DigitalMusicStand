using Avalonia.Controls;
using AvaloniaInkCanvasDemo.ViewModels;

namespace AvaloniaInkCanvasDemo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        var mainViewModel = DataContext as MainViewModel;
        mainViewModel.SaveAnnotations();   
        base.OnClosing(e); // Call the base method
    }
}
