using System;
using Avalonia;
using Avalonia.ReactiveUI;

namespace BlackFolder;

// Inking control for Avalonia: https://github.com/dotnet-campus/DotNetCampus.InkCanvas
// Core PDF renderer: https://mupdf.com/
// .NET bindings for MuPDF: https://github.com/arklumpus/MuPDFCore

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
