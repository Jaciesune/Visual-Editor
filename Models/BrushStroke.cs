using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VE.Models
{
    public class BrushStroke
    {
        public List<Point> Points { get; set; } = new();
        public SKColor StrokeColor { get; set; }
        public float StrokeWidth { get; set; } = 4;
        public BrushSettings.BrushTipType TipType { get; set; }

    }
}
