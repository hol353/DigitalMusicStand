using System;
using System.IO;

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
}