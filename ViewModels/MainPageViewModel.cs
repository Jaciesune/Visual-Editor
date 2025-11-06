using Microsoft.Maui.Graphics;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VE.Models;
using VE.Views;

namespace VE.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public MainPageViewModel()
        {
            Brush.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(BrushColor));
            };

            OpenImageCommand = new Command(async () => await OpenImageAsync());
            SaveImageCommand = new Command(async () => await SaveImage());

        }

        // Obsługa opcji narzędzi //

        public string SelectedTool { get; set; }
        public View ToolOptionsView { get; set; }

        public ICommand ToolButtonClickedCommand => new Command<string>(tool =>
        {
            SelectedTool = tool;
            OnPropertyChanged(nameof(SelectedTool));
            OnPropertyChanged(nameof(ToolOptionsView));
        });

        //------ Narzędzia ------//

        // Gumka //
        public EraserSettings Eraser { get; set; } = new EraserSettings();
        private bool _isEraserActive;
        public bool IsEraserActive
        {
            get => _isEraserActive;
            set { _isEraserActive = value; OnPropertyChanged(nameof(IsEraserActive)); }
        }

        private SKPoint _eraserPreviewPosition;
        public SKPoint EraserPreviewPosition
        {
            get => _eraserPreviewPosition;
            set { _eraserPreviewPosition = value; OnPropertyChanged(nameof(EraserPreviewPosition)); }
        }

        //------ Gumka ------//

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

        // OpenImage Command //
        public ICommand OpenImageCommand { get; }

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

        //------ OpenImage Command ------//

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

        // SaveImage Command //
        public ICommand SaveImageCommand { get; }

        public async Task SaveImage()
        {
            var mainPage = (Application.Current.MainPage as MainPage);

            // Wywołaj zapis dialogu
            var filePath = await mainPage.ShowSaveFileDialog("obraz.png");
            if (filePath == null)
                return; // użytkownik anulował

            // generowanie bitmapy z SKSurface
            int width = 700;
            int height = 450;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // narysuj wszystko z PaintSurface
            foreach (var stroke in Strokes)
            {
                if (stroke.Points.Count < 2) continue;
                using var paint = new SKPaint
                {
                    Color = stroke.StrokeColor,
                    Style = SKPaintStyle.Stroke,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeWidth = 4,
                    IsAntialias = true
                };
                for (int i = 1; i < stroke.Points.Count; i++)
                {
                    var p1 = stroke.Points[i - 1];
                    var p2 = stroke.Points[i];
                    canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint);
                }
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }
        //------ SaveImage Command ------//
    }
}