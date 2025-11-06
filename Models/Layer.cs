using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VE.Models
{
    public class Layer
    {
        public string Name { get; set; }
        public ObservableCollection<BrushStroke> Strokes { get; } = new();
        public bool IsVisible { get; set; } = true;
    }
}
