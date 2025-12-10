using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VE.Models
{
    public class EyedropperColorSlot
    {
        public Color Color { get; set; }
    }

    public class EyedropperSettings
    {
        public ObservableCollection<EyedropperColorSlot> Slots { get; set; }
            = new ObservableCollection<EyedropperColorSlot>();
    }
}
