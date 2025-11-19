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
                if (vm.SelectedTool == "Eraser")
                {
                    vm.EraserPreviewPosition = e.Location; // wyświetlanie gumki na bieżąco 
                    switch (e.ActionType)
                    {
                        case SKTouchAction.Pressed:
                            vm.IsEraserActive = true;
                            vm.EraseOnSelectedLayer(e.Location.X, e.Location.Y);
                            vm.EraserPreviewPosition = e.Location;
                            MainCanvas.InvalidateSurface();
                            break;
                        case SKTouchAction.Moved:
                            if (vm.IsEraserActive) // usuwanie po wciśnięciu
                            {
                                vm.EraseOnSelectedLayer(e.Location.X, e.Location.Y);
                                vm.EraserPreviewPosition = e.Location;
                                MainCanvas.InvalidateSurface();
                            }
                            break;
                        case SKTouchAction.Released:
                        case SKTouchAction.Cancelled:
                            vm.IsEraserActive = false;
                            break;
                    }
                    MainCanvas.InvalidateSurface();
                    e.Handled = true;
                    return;
                }
                // Pędzel:
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
                    var tipType = vm.Brush.TipType;

                    for (int i = 1; i < stroke.Points.Count; i++)
                    {
                        using var paint = MakePaintForTip(stroke.StrokeColor, stroke.StrokeWidth, tipType);

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
        //--- Działanie Rysowania ---//

        // Obsługa końcówek //
        private SKPaint MakePaintForTip(SKColor color, float width, BrushSettings.BrushTipType tip)
        {
            var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = width,
                IsAntialias = true
            };

            switch (tip)
            {
                case BrushSettings.BrushTipType.Pencil:
                    // klasyczna, cienka linia
                    break;
                case BrushSettings.BrushTipType.Brush:
                    paint.StrokeWidth = width * 2; // grubsza
                    paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3); // "miękka" końcówka
                    break;
                case BrushSettings.BrushTipType.Crayon:
                    paint.IsAntialias = false;
                    paint.PathEffect = SKPathEffect.CreateDash(new float[] { 7, 3 }, 0); // efekt przerywanej lub ziarnistej
                    break;
            }
            return paint;
        }

        //--- Obsługa Końcówek ---//

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
