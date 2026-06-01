using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace BlackFolder;

/// <summary>
/// Represents all annotations in an SVG file.
/// </summary>
/// <param name="Model">The model instance.</param>
public class SVG
{
    private XmlDocument svg;

    public SVG(string svgFileName)
    {
        svg = new XmlDocument();
        svg.Load(svgFileName);
    }

    public int Width => Int32.Parse(svg.DocumentElement.GetAttribute(("width").ToString()));

    public int Height => Int32.Parse(svg.DocumentElement.GetAttribute(("height").ToString()));

    public IEnumerable<SVGPath> Paths => svg.GetElementsByTagName("path")
                                            .Cast<XmlNode>()
                                            .Select(n => new SVGPath(n));


    public class SVGPath(XmlNode pathNode)
    {
        public SKColor Color
        {
            get
            {
                string colorName = pathNode.Attributes["fill"].Value;
                var fieldInfo = typeof(SKColors).GetFields(BindingFlags.Static | BindingFlags.Public)
                                                .First(fieldInfo => fieldInfo.Name.ToLower() == colorName);
                return (SKColor)fieldInfo.GetValue(null);
            }
        }

        public SKPath Path
        {
            get
            {
                var pathData = pathNode.Attributes["d"].Value;
                return SKPath.ParseSvgPathData(pathData); 
            }
        }
    }   
}