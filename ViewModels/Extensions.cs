using System;
using System.IO;
using Avalonia;
using SkiaSharp;

namespace BlackFolder;

/// <summary>
/// Extension methods.
/// </summary>
public static class Extensions
{

    /// <summary>
    /// Convert a path to a relative path.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <param name="basePath">The path to make it relative to.</param>
    public static string ToRelative(this string path, string basePath)
    {
        return path.Replace(basePath, "").TrimStart('/').ToString();
    }

    /// <summary>
    /// Convert a path to a relative path.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <param name="basePath">The path to make it relative to.</param>
    public static string ToAbsolute(this string path, string basePath)
    {
        return Path.Combine(basePath, path);
    }    

    /// <summary>
    /// Converts an Avalonia Rect to a SkiaSharp SKRect.
    /// </summary>
    /// <param name="rect">The Avalonia Rect to convert.</param>
    /// <returns>The SkiaSharp SKRect.</returns>
    public static SKRect ToSKRect(this Rect rect)
    {
        return new SKRect(
            (float)rect.X,
            (float)rect.Y,
            (float)rect.Right,
            (float)rect.Bottom);
    }    
}