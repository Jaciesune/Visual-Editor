using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VE.Models
{
    public class BucketSettings
    {
        public Color SelectedColor { get; set; }
        public ObservableCollection<Color> PredefinedColors { get; set; }
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }
}
