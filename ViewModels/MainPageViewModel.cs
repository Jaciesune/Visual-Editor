using Microsoft.Maui.Graphics;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VE.Models;

namespace VE.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public BrushSettings Brush { get; set; } = new BrushSettings();
        
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