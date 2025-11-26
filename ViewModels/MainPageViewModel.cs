using Microsoft.Maui.Graphics;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using VE.Models;
using VE.Views;

namespace VE.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public MainPageViewModel()
        {
            Layers.Add(new Layer { Name = "Tło", IsVisible = true, Bitmap = new SKBitmap(CanvasWidth, CanvasHeight) });
            SelectedLayer = Layers.First();

            Brush.PropertyChanged += (s, e) => OnPropertyChanged(nameof(BrushColor));
            OpenImageCommand = new Command(async () => await OpenImageAsync());
            SaveImageCommand = new Command(async () => await SaveImage());
            AddLayerCommand = new Command(AddLayer);
            ToggleLayerVisibilityCommand = new Command<Layer>(ToggleLayerVisibility);
            RemoveLayerCommand = new Command<Layer>(RemoveLayer);
            BucketSettings = new BucketSettings
            {
                PredefinedColors = new ObservableCollection<Color>
                {
                    Colors.Black, Colors.White, Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Cyan
                },
                SelectedColor = Colors.Black,
                Red = 0,
                Green = 0,
                Blue = 0
            };
            _pendingCanvasWidth = _canvasWidth;
            _pendingCanvasHeight = _canvasHeight;

        }

        // Canvas //

        private int _canvasWidth = 700;
        private int _canvasHeight = 450;
        private int _pendingCanvasWidth = 700;
        private int _pendingCanvasHeight = 450;

        public int CanvasWidth
        {
            get => _canvasWidth;
            private set { _canvasWidth = value; OnPropertyChanged(nameof(CanvasWidth)); } // prywatny setter!
        }
        public int CanvasHeight
        {
            get => _canvasHeight;
            private set { _canvasHeight = value; OnPropertyChanged(nameof(CanvasHeight)); }
        }

        // Właściwości powiązane z entry
        public int PendingCanvasWidth
        {
            get => _pendingCanvasWidth;
            set { _pendingCanvasWidth = value; OnPropertyChanged(nameof(PendingCanvasWidth)); }
        }
        public int PendingCanvasHeight
        {
            get => _pendingCanvasHeight;
            set { _pendingCanvasHeight = value; OnPropertyChanged(nameof(PendingCanvasHeight)); }
        }

        // Komenda do zmiany rozmiaru płótna (canvas)
        public ICommand ResizeCanvasCommand => new Command(ResizeCanvas);


        // Zmiana wielkości //

        public void ResizeCanvas()
        {
            CanvasWidth = PendingCanvasWidth;
            CanvasHeight = PendingCanvasHeight;

            foreach (var layer in Layers)
            {
                var newBmp = new SKBitmap(CanvasWidth, CanvasHeight);
                using (var canvas = new SKCanvas(newBmp))
                {
                    canvas.Clear(SKColors.Transparent);
                    if (layer.Bitmap != null)
                        canvas.DrawBitmap(layer.Bitmap, 0, 0);
                }
                layer.Bitmap = newBmp;
            }
            OnPropertyChanged(nameof(Layers));
        }


        //--- Zmiana wielkości ---//

        //------ Canvas ------//

        private List<Point> _currentStrokePoints; // Tymczasowa zmienna na czas rysowania pociągnięcia
        public IReadOnlyList<Point> CurrentStrokePoints => _currentStrokePoints;

        // Bucket //

        public BucketSettings BucketSettings { get; set; }

        public ICommand SetBucketColorCommand => new Command<Color>(color =>
        {
            BucketSettings.SelectedColor = color;
            BucketSettings.Red = (int)(color.Red * 255);
            BucketSettings.Green = (int)(color.Green * 255);
            BucketSettings.Blue = (int)(color.Blue * 255);
            OnPropertyChanged(nameof(BucketSettings));
        });
        public ICommand FillBucketCommand => new Command<Point>(pt => FillWithBucket(pt));

        private void FillWithBucket(Point pt)
        {
            if (SelectedLayer?.Bitmap == null) return;

            int x = (int)pt.X;
            int y = (int)pt.Y;
            var bitmap = SelectedLayer.Bitmap;

            if (x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height) return;

            SKColor targetColor = bitmap.GetPixel(x, y);

            SKColor fillColor = new SKColor(
                (byte)(BucketSettings.SelectedColor.Red * 255),
                (byte)(BucketSettings.SelectedColor.Green * 255),
                (byte)(BucketSettings.SelectedColor.Blue * 255),
                (byte)(BucketSettings.SelectedColor.Alpha * 255));

            if (AreColorsSimilar(targetColor, fillColor, tolerance: 10)) return;

            Queue<(int, int)> queue = new();
            queue.Enqueue((x, y));
            HashSet<(int, int)> visited = new();

            int[] dx = { -1, -1, -1, 0, 1, 1, 1, 0 };
            int[] dy = { -1, 0, 1, 1, 1, 0, -1, -1 };

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                if (cx < 0 || cy < 0 || cx >= bitmap.Width || cy >= bitmap.Height) continue;
                if (visited.Contains((cx, cy))) continue;

                var currentColor = bitmap.GetPixel(cx, cy);

                if (AreColorsSimilar(currentColor, targetColor, tolerance: 10))
                {
                    bitmap.SetPixel(cx, cy, fillColor);
                    visited.Add((cx, cy));

                    for (int dir = 0; dir < 8; dir++)
                    {
                        int nx = cx + dx[dir];
                        int ny = cy + dy[dir];
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            OnPropertyChangedForLayer(SelectedLayer);
            OnPropertyChanged(nameof(Layers));
        }

        private bool AreColorsSimilar(SKColor c1, SKColor c2, int tolerance = 10)
        {
            return Math.Abs(c1.Red - c2.Red) <= tolerance &&
                   Math.Abs(c1.Green - c2.Green) <= tolerance &&
                   Math.Abs(c1.Blue - c2.Blue) <= tolerance &&
                   Math.Abs(c1.Alpha - c2.Alpha) <= tolerance;
        }

        //------ Bucket ------//


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

        private void AddLayer()
        {
            var bmp = new SKBitmap(CanvasWidth, CanvasHeight);
            bmp.Erase(SKColors.Transparent);

            var newLayer = new Layer
            {
                Name = $"Warstwa {Layers.Count}",
                IsVisible = true,
                Bitmap = bmp
            };
            Layers.Add(newLayer);
            SelectedLayer = newLayer;
            OnPropertyChanged(nameof(Layers));
        }

        private void ToggleLayerVisibility(Layer layer)
        {
            if (layer != null)
            {
                layer.IsVisible = !layer.IsVisible;
                OnPropertyChanged(nameof(Layers));
            }
        }

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

        public Color BrushColor => Color.FromRgb(Brush.R, Brush.G, Brush.B);

        public void StartStrokeOnSelectedLayer()
        {
            _currentStrokePoints = new List<Point>();
        }
        public void AddStrokePointOnSelectedLayer(double x, double y)
        {
            _currentStrokePoints?.Add(new Point(x, y));
        }
        public void EndStrokeOnSelectedLayer()
        {
            if (_currentStrokePoints == null || _currentStrokePoints.Count < 2 || SelectedLayer?.Bitmap == null)
            {
                _currentStrokePoints = null;
                return;
            }
            using var canvas = new SKCanvas(SelectedLayer.Bitmap);

            var color = new SKColor((byte)Brush.R, (byte)Brush.G, (byte)Brush.B);

            switch (Brush.TipType)
            {
                case BrushSettings.BrushTipType.Pencil:
                    using (var paint = new SKPaint
                    {
                        Color = color,
                        StrokeWidth = Brush.Size,
                        Style = SKPaintStyle.Stroke,
                        StrokeCap = SKStrokeCap.Round,
                        IsAntialias = true
                    })
                    {
                        for (int i = 1; i < _currentStrokePoints.Count; i++)
                        {
                            var p1 = _currentStrokePoints[i - 1];
                            var p2 = _currentStrokePoints[i];
                            canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint);
                        }
                    }
                    break;

                case BrushSettings.BrushTipType.Brush:
                    using (var paint = new SKPaint
                    {
                        Color = color,
                        StrokeWidth = Brush.Size * 2f,
                        Style = SKPaintStyle.Stroke,
                        StrokeCap = SKStrokeCap.Round,
                        IsAntialias = true,
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3) // miękki efekt
                    })
                    {
                        for (int i = 1; i < _currentStrokePoints.Count; i++)
                        {
                            var p1 = _currentStrokePoints[i - 1];
                            var p2 = _currentStrokePoints[i];
                            canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint);
                        }
                    }
                    break;

                case BrushSettings.BrushTipType.Crayon:
                    using (var paint = new SKPaint
                    {
                        Color = color,
                        StrokeWidth = Brush.Size,
                        Style = SKPaintStyle.Stroke,
                        StrokeCap = SKStrokeCap.Round,
                        IsAntialias = false,
                        PathEffect = SKPathEffect.CreateDash(new float[] { 7, 3 }, 0)
                    })
                    {
                        for (int i = 1; i < _currentStrokePoints.Count; i++)
                        {
                            var p1 = _currentStrokePoints[i - 1];
                            var p2 = _currentStrokePoints[i];
                            canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint);
                        }
                    }
                    break;

                case BrushSettings.BrushTipType.Marker:
                    using (var paint = new SKPaint
                    {
                        Color = color.WithAlpha(120), // lekka transparencja
                        StrokeWidth = Brush.Size * 3f,
                        Style = SKPaintStyle.Stroke,
                        StrokeCap = SKStrokeCap.Round,
                        IsAntialias = true
                    })
                    {
                        for (int i = 1; i < _currentStrokePoints.Count; i++)
                        {
                            var p1 = _currentStrokePoints[i - 1];
                            var p2 = _currentStrokePoints[i];
                            canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint);
                        }
                        // kropka na początku i końcu
                        using var circlePaint = new SKPaint
                        {
                            Color = color.WithAlpha(120),
                            Style = SKPaintStyle.Fill
                        };
                        canvas.DrawCircle((float)_currentStrokePoints.First().X, (float)_currentStrokePoints.First().Y, Brush.Size * 1.5f, circlePaint);
                        canvas.DrawCircle((float)_currentStrokePoints.Last().X, (float)_currentStrokePoints.Last().Y, Brush.Size * 1.5f, circlePaint);
                    }
                    break;

                case BrushSettings.BrushTipType.Spray:
                    using (var paint = new SKPaint
                    {
                        Color = color,
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true
                    })
                    {
                        var density = Math.Max(Brush.SprayDensity, 1);
                        var rand = new Random();
                        foreach (var p in _currentStrokePoints)
                        {
                            for (int i = 0; i < density; i++)
                            {
                                float dx = (float)((rand.NextDouble() - 0.5) * Brush.Size);
                                float dy = (float)((rand.NextDouble() - 0.5) * Brush.Size);
                                float radius = 1.1f;
                                canvas.DrawCircle((float)p.X + dx, (float)p.Y + dy, radius, paint);
                            }
                        }
                    }
                    break;
            }

            _currentStrokePoints = null;
            OnPropertyChangedForLayer(SelectedLayer);
            OnPropertyChanged(nameof(Layers));
        }


        public void EraseOnSelectedLayer(double x, double y)
        {
            if (SelectedLayer?.Bitmap == null) return;
            using var canvas = new SKCanvas(SelectedLayer.Bitmap);

            using var paint = new SKPaint
            {
                Color = SKColors.Transparent,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = Eraser.Size,
                BlendMode = SKBlendMode.Clear
            };

            canvas.DrawCircle((float)x, (float)y, Eraser.Size / 2f, paint);
            OnPropertyChangedForLayer(SelectedLayer);
            OnPropertyChanged(nameof(Layers));
        }

        //------ Gumka i Pędzel ------//

        private ImageSource _canvasImage;
        public ImageSource CanvasImage
        {
            get => _canvasImage;
            set { _canvasImage = value; OnPropertyChanged(nameof(CanvasImage)); }
        }

        public ICommand SetColorCommand => new Command<string>(hex =>
        {
            var color = Color.FromArgb(hex);
            Brush.R = (int)(color.Red * 255);
            Brush.G = (int)(color.Green * 255);
            Brush.B = (int)(color.Blue * 255);
        });

        public ObservableCollection<BrushSettings.BrushTipType> BrushTipTypes { get; } = new ObservableCollection<BrushSettings.BrushTipType>
        {
            BrushSettings.BrushTipType.Pencil,
            BrushSettings.BrushTipType.Brush,
            BrushSettings.BrushTipType.Crayon,
            BrushSettings.BrushTipType.Marker,
            BrushSettings.BrushTipType.Spray,
        };

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
            catch (Exception ex)
            {
                ImageLoadError = "Błąd odczytu pliku: " + ex.Message;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string _imageLoadError;
        public string ImageLoadError
        {
            get => _imageLoadError;
            set { _imageLoadError = value; OnPropertyChanged(nameof(ImageLoadError)); }
        }

        public ICommand SaveImageCommand { get; }
        public async Task SaveImage()
        {
            var mainPage = (Application.Current.MainPage as MainPage);

            var filePath = await mainPage.ShowSaveFileDialog("obraz.png");
            if (filePath == null)
                return;

            int width = CanvasWidth, height = CanvasHeight;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            foreach (var layer in Layers.Where(l => l.IsVisible))
            {
                if (layer.Bitmap != null)
                    canvas.DrawBitmap(layer.Bitmap, 0, 0);
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }

        public ICommand NewProjectCommand => new Command(async () => await NewProjectAsync());

        private async Task NewProjectAsync()
        {
            // Sprawdzenie, czy jest jakakolwiek zawartość (poza pustą warstwą Tło)
            bool hasContent = ProjectHasContent();

            if (hasContent)
            {
                var answer = await Application.Current.MainPage.DisplayAlert(
                    "Nowy projekt",
                    "Na bieżącym płótnie są niezapisane zmiany. Czy chcesz utworzyć nowy projekt bez zapisu?",
                    "Tak",
                    "Anuluj");

                if (!answer)
                    return;
            }

            Layers.Clear();
            Layers.Add(new Layer { Name = "Tło", IsVisible = true, Bitmap = new SKBitmap(CanvasWidth, CanvasHeight) });
            SelectedLayer = Layers.First();
            CanvasImage = null;
        }

        private bool ProjectHasContent()
        {
            if (Layers.Count > 1)
                return true;
            if (Layers.Count == 1 && !IsBitmapEmpty(Layers[0].Bitmap))
                return true;
            return false;
        }

        // Definicja sprawdzania, czy bitmapa (warstwa) jest pusta
        private bool IsBitmapEmpty(SKBitmap bmp)
        {
            if (bmp == null)
                return true;
            // Wersja z losowaniem pikseli
            int tested = 0, whiteChecked = 0;
            Random rand = new();
            for (int i = 0; i < 100; i++)
            {
                int x = rand.Next(bmp.Width);
                int y = rand.Next(bmp.Height);
                var px = bmp.GetPixel(x, y);
                tested++;
                if (px.Alpha == 0 || (px.Red == 255 && px.Green == 255 && px.Blue == 255))
                    whiteChecked++;
            }
            // Jeśli wszystkie sprawdzone to przezroczyste lub białe – traktujemy jako puste
            return tested == whiteChecked;
        }
    }
}
