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
            Layers.Add(new Layer { Name = "Tło" });
            SelectedLayer = Layers.First();

            Brush.PropertyChanged += (s, e) => OnPropertyChanged(nameof(BrushColor));
            OpenImageCommand = new Command(async () => await OpenImageAsync());
            SaveImageCommand = new Command(async () => await SaveImage());
            AddLayerCommand = new Command(AddLayer);
            ToggleLayerVisibilityCommand = new Command<Layer>(ToggleLayerVisibility);
            RemoveLayerCommand = new Command<Layer>(RemoveLayer);

        }

        // Warstwy //
        public ICommand SelectLayerCommand => new Command<Layer>(layer =>
        {
            foreach (var l in Layers)
                l.IsSelected = false;
            if (layer != null)
                layer.IsSelected = true;
            SelectedLayer = layer;
        });
        public ICommand ToggleLayerVisibilityCommand { get; }
        public ICommand RemoveLayerCommand { get; }
        public ICommand AddLayerCommand { get; }

        // Dodanie warstwy //

        private void AddLayer()
        {
            var newLayer = new Layer
            {
                Name = $"Warstwa {Layers.Count}",
                IsVisible = true
            };
            Layers.Add(newLayer);
            SelectedLayer = newLayer;
            OnPropertyChanged(nameof(Layers));
        }

        //--- Dodanie warstwy ---//

        // Ukrycie warstwy //

        private void ToggleLayerVisibility(Layer layer)
        {
            if (layer != null)
            {
                layer.IsVisible = !layer.IsVisible;
                OnPropertyChanged(nameof(Layers));
            }
        }

        //--- Ukrycie warstwy ---//

        //Usunięcie warstwy //

        private void RemoveLayer(Layer layer)
        {
            if (layer != null && Layers.Count > 1)
            {
                Layers.Remove(layer);
                if (SelectedLayer == layer)
                    SelectedLayer = Layers.FirstOrDefault();
                OnPropertyChanged(nameof(Layers));
            }
        }

        //--- Usunięcie warstwy ---//

        public ObservableCollection<Layer> Layers { get; set; } = new();
        private Layer _selectedLayer;
        public Layer SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                if (_selectedLayer != value)
                {
                    _selectedLayer = value;
                    OnPropertyChanged(nameof(SelectedLayer));
                    OnPropertyChanged(nameof(Layers));
                    // wymuszenioe ponownego renderowania warstw
                    foreach (var layer in Layers)
                        OnPropertyChangedForLayer(layer);
                }
            }
        }

        private void OnPropertyChangedForLayer(Layer layer)
        {
            var temp = layer.Name;
            layer.Name = temp;
        }

        //------ Warstwy ------//

        // Narzędzia //

        public string SelectedTool { get; set; }
        public ICommand ToolButtonClickedCommand => new Command<string>(tool =>
        {
            SelectedTool = tool;
            OnPropertyChanged(nameof(SelectedTool));
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

        public BrushSettings Brush { get; set; } = new BrushSettings();
        private BrushStroke _currentStroke;

        // usuwanie z warstwy //

        public void EraseOnSelectedLayer(double x, double y)
        {
            if (SelectedLayer == null) return;
            double r = Eraser.Size;

            var strokesToAdd = new List<BrushStroke>();

            foreach (var stroke in SelectedLayer.Strokes.Reverse().ToList())
            {
                var newSegments = new List<List<Point>>();
                var currentSegment = new List<Point>();

                foreach (var p in stroke.Points)
                {
                    double dx = x - p.X;
                    double dy = y - p.Y;
                    if (dx * dx + dy * dy > r * r)
                    {
                        currentSegment.Add(p);
                    }
                    else
                    {
                        if (currentSegment.Count > 0)
                        {
                            newSegments.Add(new List<Point>(currentSegment));
                            currentSegment.Clear();
                        }
                    }
                }
                if (currentSegment.Count > 0)
                    newSegments.Add(currentSegment);

                // Zamiana na fragmenty
                if (newSegments.Count == 0)
                    SelectedLayer.Strokes.Remove(stroke);
                else if (newSegments.Count == 1)
                {
                    stroke.Points.Clear(); // zachowanie stroke
                    stroke.Points.AddRange(newSegments[0]);
                }
                else
                {
                    int idx = SelectedLayer.Strokes.IndexOf(stroke);
                    SelectedLayer.Strokes.RemoveAt(idx);
                    foreach (var seg in newSegments)
                    {
                        if (seg.Count > 1)
                        {
                            var newStroke = new BrushStroke
                            {
                                Points = seg,
                                StrokeColor = stroke.StrokeColor,
                                StrokeWidth = stroke.StrokeWidth
                            };
                            SelectedLayer.Strokes.Insert(idx++, newStroke);
                        }
                    }
                }
            }
            OnPropertyChanged(nameof(Layers));
        }

        //------ Gumka ------//


        // Pędzel //
        public Color BrushColor => Color.FromRgb(Brush.R, Brush.G, Brush.B);

        public void StartStrokeOnSelectedLayer()
        {
            if (SelectedLayer == null) return;
            _currentStroke = new BrushStroke
            {
                StrokeColor = new SKColor((byte)Brush.R, (byte)Brush.G, (byte)Brush.B),
                StrokeWidth = 4
            };
            SelectedLayer.Strokes.Add(_currentStroke);
            OnPropertyChanged(nameof(Layers));
        }
        public void AddStrokePointOnSelectedLayer(double x, double y)
        {
            if (_currentStroke == null) return;
            _currentStroke.Points.Add(new Point(x, y));
            OnPropertyChanged(nameof(Layers));
        }
        public void EndStrokeOnSelectedLayer()
        {
            _currentStroke = null;
            OnPropertyChanged(nameof(Layers));
        }

        //------ Pędzel ------//

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

        // Color Picker //

        public ICommand SetColorCommand => new Command<string>(hex =>
        {
            var color = Color.FromArgb(hex);
            Brush.R = (int)(color.Red * 255);
            Brush.G = (int)(color.Green * 255);
            Brush.B = (int)(color.Blue * 255);
        });

        //--- Color Picker ---//


        //------ Pędzel ------//

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

            var filePath = await mainPage.ShowSaveFileDialog("obraz.png");
            if (filePath == null)
                return;

            int width = 700, height = 450;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            foreach (var layer in Layers.Where(l => l.IsVisible))
            {
                foreach (var stroke in layer.Strokes)
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
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }
        //------ SaveImage Command ------//

        // NewProjectCommand //

        public ICommand NewProjectCommand => new Command(async () => await NewProjectAsync());

        private async Task NewProjectAsync()
        {
            // Sprawdzenie czy jest na warstwach coś do zapisania
            bool hasContent = Layers.Any(l => l.Strokes.Count > 0);

            if (hasContent)
            {
                // Pokazanie pytania użytkownikowi czy kontynuować
                var answer = await Application.Current.MainPage.DisplayAlert(
                    "Nowy projekt",
                    "Na bieżącym płótnie są niezapisane zmiany. Czy chcesz utworzyć nowy projekt bez zapisu?",
                    "Tak",
                    "Anuluj");

                if (!answer)
                    return; // Przerwanie jeśli wybrał anuluj
            }

            // Tworzenie nowej pustej warstwy "Tło"
            Layers.Clear();
            Layers.Add(new Layer { Name = "Tło", IsVisible = true });
            SelectedLayer = Layers.First();

            // Czyszczenie podgląd, obraz itp:
            CanvasImage = null;
        }

        //------ NewProjectCommand ------/
    }
}