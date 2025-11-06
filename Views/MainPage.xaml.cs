using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using VE.ViewModels;
using VE.Models;
using System;
using System.Linq;
using System.Collections.Generic;

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
            if (BindingContext is VE.ViewModels.MainPageViewModel vm && vm.SelectedLayer != null)
            {
                // Gumka - wymazuje tylko ze stroke w aktywnej warstwie
                if (vm.SelectedTool == "Eraser")
                {
                    switch (e.ActionType)
                    {
                        case SKTouchAction.Pressed:
                        case SKTouchAction.Moved:
                            // Usuwaj punkty ze stroke, na które najeżdża gumka
                            RemovePointsUnderEraser(vm, e.Location.X, e.Location.Y, vm.Eraser.Size);
                            vm.IsEraserActive = true;
                            vm.EraserPreviewPosition = e.Location;
                            MainCanvas.InvalidateSurface();
                            break;
                        case SKTouchAction.Released:
                        case SKTouchAction.Cancelled:
                            vm.IsEraserActive = false;
                            break;
                    }
                    e.Handled = true;
                    return;
                }

                // Pędzel
                switch (e.ActionType)
                {
                    case SKTouchAction.Pressed:
                        vm.StartStrokeOnSelectedLayer();
                        vm.AddStrokePointOnSelectedLayer(e.Location.X, e.Location.Y);
                        break;
                    case SKTouchAction.Moved:
                        vm.AddStrokePointOnSelectedLayer(e.Location.X, e.Location.Y);
                        break;
                    case SKTouchAction.Released:
                    case SKTouchAction.Cancelled:
                        vm.EndStrokeOnSelectedLayer();
                        break;
                }
                MainCanvas.InvalidateSurface();
                e.Handled = true;
            }
        }

        // Funkcja – usuwa punkty w danym promieniu gumki w warstwie
        private void RemovePointsUnderEraser(VE.ViewModels.MainPageViewModel vm, double x, double y, double eraserRadius)
        {
            foreach (var stroke in vm.SelectedLayer.Strokes.Reverse())
            {
                for (int i = stroke.Points.Count - 1; i >= 0; i--)
                {
                    var p = stroke.Points[i];
                    var dx = x - p.X;
                    var dy = y - p.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist <= eraserRadius)
                    {
                        stroke.Points.RemoveAt(i);
                    }
                }
                // Jeśli stroke jest pusty – usuń cały
                if (stroke.Points.Count == 0)
                    vm.SelectedLayer.Strokes.Remove(stroke);
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

            foreach (var layer in vm.Layers.Where(l => l.IsVisible))
            {
                foreach (var stroke in layer.Strokes)
                {
                    if (stroke.Points.Count < 2)
                        continue;

                    using var paint = new SKPaint
                    {
                        Color = stroke.StrokeColor,
                        Style = SKPaintStyle.Stroke,
                        StrokeCap = SKStrokeCap.Round,
                        StrokeWidth = stroke.StrokeWidth > 0 ? stroke.StrokeWidth : 4, // umożliwia zmienność grubości
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
            // Podgląd gumki
            if (vm.SelectedTool == "Eraser")
            {
                var pos = vm.EraserPreviewPosition;
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

            foreach (var layer in vm.Layers.Where(l => l.IsVisible))
            {
                foreach (var stroke in layer.Strokes)
                {
                    if (stroke.Points.Count < 2)
                        continue;
                    using var paint = new SKPaint
                    {
                        Color = stroke.StrokeColor,
                        Style = SKPaintStyle.Stroke,
                        StrokeCap = SKStrokeCap.Round,
                        StrokeWidth = stroke.StrokeWidth > 0 ? stroke.StrokeWidth : 4,
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
        //------ Obsługa SaveFile ------//
    }
}
