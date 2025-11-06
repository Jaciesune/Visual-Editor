using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using VE.ViewModels;
using VE.Models;


#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Microsoft.UI.Windowing;
#endif

namespace VE.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            BindingContext = new VE.ViewModels.MainPageViewModel();
        }

        // Obsługa Narzędzi //
        private void MainCanvas_Touch(object sender, SKTouchEventArgs e)
        {
            if (BindingContext is VE.ViewModels.MainPageViewModel vm)
            {
                // Gumka
                if (vm.SelectedTool == "Eraser")
                {
                    switch (e.ActionType)
                    {
                        case SKTouchAction.Pressed:
                            vm.IsEraserActive = true;
                            vm.Strokes.Add(new BrushStroke
                            {
                                StrokeColor = SKColors.White,
                                Points = new List<Point> { new Point(e.Location.X, e.Location.Y) },
                                EraserSize = vm.Eraser.Size
                            });
                            break;
                        case SKTouchAction.Moved:
                            if (vm.IsEraserActive)
                                vm.Strokes.Add(new BrushStroke
                                {
                                    StrokeColor = SKColors.White,
                                    Points = new List<Point> { new Point(e.Location.X, e.Location.Y) },
                                    EraserSize = vm.Eraser.Size
                                });
                            break;
                        case SKTouchAction.Released:
                        case SKTouchAction.Cancelled:
                            vm.IsEraserActive = false;
                            break;
                    }
                    vm.EraserPreviewPosition = e.Location; // do podglądu!
                    MainCanvas.InvalidateSurface();
                    e.Handled = true;
                    return;
                }

                // Pędzel:
                switch (e.ActionType)
                {
                    case SKTouchAction.Pressed:
                        vm.StartStroke();
                        vm.AddStrokePoint(e.Location.X, e.Location.Y);
                        break;
                    case SKTouchAction.Moved:
                        vm.AddStrokePoint(e.Location.X, e.Location.Y);
                        break;
                    case SKTouchAction.Released:
                    case SKTouchAction.Cancelled:
                        vm.EndStroke();
                        break;
                }
                MainCanvas.InvalidateSurface();
                e.Handled = true;
            }
        }

        //------ Obsługa Narzędzi ------//

        // Działanie Rysowania //
        private void MainCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var vm = BindingContext as VE.ViewModels.MainPageViewModel;
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            if (vm == null) return;

            foreach (var stroke in vm.Strokes)
            {
                if (stroke.EraserSize > 0)
                {
                    using var eraserPaint = new SKPaint
                    {
                        Color = SKColors.White,
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true
                    };

                    foreach (var p in stroke.Points)
                    {
                        canvas.DrawCircle((float)p.X, (float)p.Y, stroke.EraserSize, eraserPaint);
                    }
                    continue;
                }

                if (stroke.Points.Count < 2)
                    continue;

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
            if (vm.SelectedTool == "Eraser")
            {
                var pos = vm.EraserPreviewPosition; // aktualna pozycja kursora
                int radius = vm.Eraser.Size;
                using var borderPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    IsAntialias = true
                };
                canvas.DrawCircle((float)pos.X, (float)pos.Y, radius, borderPaint);
            }
        }

        //------ Działanie rysowania ------//

        // Obsługa SaveFile //

        public async Task<string> ShowSaveFileDialog(string suggestedFileName)
        {
        #if WINDOWS
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            var window = (Microsoft.Maui.Controls.Application.Current.Windows[0].Handler.PlatformView as Microsoft.UI.Xaml.Window);
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedFileName = suggestedFileName;
            picker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
            picker.FileTypeChoices.Add("JPG", new List<string>() { ".jpg" });

            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        #else
                // ewentualne ustawienie zapisania dla innych sys.
                return null;
        #endif
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            var filePath = await ShowSaveFileDialog("obraz.png");
            if (filePath == null) return;

            var vm = (MainPageViewModel)BindingContext;

            int width = 700;
            int height = 450;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            foreach (var stroke in vm.Strokes)
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

        //------ Obsługa SaveFile ------//
    }
}
