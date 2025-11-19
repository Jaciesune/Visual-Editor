using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VE.Models
{
    public class BrushSettings : INotifyPropertyChanged
    {
        private int _r;
        public int R
        {
            get => _r;
            set { _r = value; OnPropertyChanged(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(R))); }
        }
        private int _g;
        public int G
        {
            get => _g;
            set { _g = value; OnPropertyChanged(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(G))); }
        }
        private int _b;
        public int B
        {
            get => _b;
            set { _b = value; OnPropertyChanged(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(B))); }
        }

        private BrushTipType _tipType = BrushTipType.Pencil;
        public BrushTipType TipType
        {
            get => _tipType;
            set { _tipType = value; OnPropertyChanged(); }
        }

        public enum BrushTipType
        {
            Pencil,
            Brush,
            Crayon,
            Marker,
            Spray
        }

        private int _sprayDensity = 12;
        public int SprayDensity
        {
            get => _sprayDensity;
            set { _sprayDensity = value; OnPropertyChanged(); }
        }

        private float _size = 8;
        public float Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}