using System.ComponentModel;
using System.Runtime.CompilerServices;
using VE.Models;
using Microsoft.Maui.Graphics;

namespace VE.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public BrushSettings Brush { get; set; } = new BrushSettings();

        public MainPageViewModel()
        {
            Brush.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(BrushColor));
            };
        }

        public Color BrushColor => Color.FromRgb(Brush.R, Brush.G, Brush.B);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}