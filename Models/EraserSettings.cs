using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VE.Models
{
    public class EraserSettings : INotifyPropertyChanged
    {
        private int _size = 10;
        public int Size
        {
            get => _size;
            set
            {
                if (value < 2) value = 2;
                if (value > 40) value = 40;
                _size = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}