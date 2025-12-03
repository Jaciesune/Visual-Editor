using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace VE.Models
{
    public class ProjectFile
    {
        public int CanvasWidth { get; set; }
        public int CanvasHeight { get; set; }

        public List<ProjectLayer> Layers { get; set; } = new();
    }

    public class ProjectLayer
    {
        public string Name { get; set; }
        public bool IsVisible { get; set; }

        // obraz zakodowany jako PNG
        public byte[] BitmapPng { get; set; }
    }
}