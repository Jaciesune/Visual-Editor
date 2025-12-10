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
            set
            {
                if (_r == value) return;
                _r = value;
                OnPropertyChanged();
            }
        }
        private int _g;
        public int G
        {
            get => _g;
            set
            {
                if (_g == value) return;
                _g = value;
                OnPropertyChanged();
            }
        }
        private int _b;
        public int B
        {
            get => _b;
            set
            {
                if (_b == value) return;
                _b = value;
                OnPropertyChanged();
            }
        }

        private BrushTipType _tipType = BrushTipType.Pencil;
        public BrushTipType TipType
        {
            get => _tipType;
            set
            {
                if (_tipType == value) return;
                _tipType = value;
                OnPropertyChanged();
            }
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