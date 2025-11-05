using Microsoft.Maui.Graphics;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VE.Models;

namespace VE.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {

        // Obsługa BrushStroke //
        public ObservableCollection<BrushStroke> Strokes { get; } = new();

        // tymczasowo przechowuje aktualnie rysowaną linię
        private BrushStroke _currentStroke;

        public BrushSettings Brush { get; set; } = new BrushSettings();

        public void StartStroke()
        {
            _currentStroke = new BrushStroke
            {
                StrokeColor = new SKColor((byte)Brush.R, (byte)Brush.G, (byte)Brush.B)
            };
            Strokes.Add(_currentStroke);
            OnPropertyChanged(nameof(Strokes));
        }

        public void AddStrokePoint(double x, double y)
        {
            _currentStroke?.Points.Add(new Point(x, y));
            OnPropertyChanged(nameof(Strokes));
        }

        public void EndStroke()
        {
            _currentStroke = null;
            OnPropertyChanged(nameof(Strokes));
        }


        //------ BrushStroke ------//

        public ObservableCollection<Point> BrushPoints { get; } = new();

        public void AddBrushPoint(double x, double y)
        {
            BrushPoints.Add(new Point(x, y));
            OnPropertyChanged(nameof(BrushPoints));
        }

        public void ClearBrushPoints()
        {
            BrushPoints.Clear();
            OnPropertyChanged(nameof(BrushPoints));
        }

        private ImageSource _canvasImage;
        public ImageSource CanvasImage
        {
            get => _canvasImage;
            set { _canvasImage = value; OnPropertyChanged(nameof(CanvasImage)); }
        }

        public ICommand OpenImageCommand { get; }

        public MainPageViewModel()
        {
            Brush.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(BrushColor));
            };

            OpenImageCommand = new Command(async () => await OpenImageAsync());
        }

        private async Task OpenImageAsync()
        {
            ImageLoadError = null;
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Wybierz obraz...",
                    FileTypes = FilePickerFileType.Images
                });

                if (result == null)
                    return;

                var ext = Path.GetExtension(result.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    ImageLoadError = "Obsługiwane są wyłącznie pliki PNG i JPG.";
                    return;
                }

                using var fileStream = await result.OpenReadAsync();

                using var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms);
                ms.Position = 0;

                CanvasImage = ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));

            }
            catch (Exception ex) {
                ImageLoadError = "Błąd odczytu pliku: " + ex.Message;
            }
        }

        public Color BrushColor => Color.FromRgb(Brush.R, Brush.G, Brush.B);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string _imageLoadError;
        public string ImageLoadError
        {
            get => _imageLoadError;
            set { _imageLoadError = value; OnPropertyChanged(nameof(ImageLoadError)); }
        }

    }
}