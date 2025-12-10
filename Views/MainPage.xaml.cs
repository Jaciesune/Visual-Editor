using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using VE.ViewModels;
using VE.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;


#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Windows.Storage.Pickers;
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


        public static SKPoint ToSKPoint(Microsoft.Maui.Graphics.Point p)=> new SKPoint((float)p.X, (float)p.Y);
        public SKPoint EraserPreviewPosition { get; set; }
        // Obsługa Narzędzi //
        private void MainCanvas_Touch(object sender, SKTouchEventArgs e)
        {
            if (BindingContext is VE.ViewModels.MainPageViewModel vm && vm.SelectedLayer != null)
            {
                // Skalowanie
                if (vm.SelectedLayer == null || vm.SelectedLayer.Bitmap == null)
                    return;
                var bmp = vm.SelectedLayer.Bitmap;
                double viewWidth = MainCanvas.CanvasSize.Width;
                double viewHeight = MainCanvas.CanvasSize.Height;
                double bmpWidth = bmp.Width;
                double bmpHeight = bmp.Height;
                double scale = Math.Min(viewWidth / bmpWidth, viewHeight / bmpHeight);
                double offsetX = (viewWidth - bmpWidth * scale) / 2.0;
                double offsetY = (viewHeight - bmpHeight * scale) / 2.0;

                double logicX = (e.Location.X - offsetX) / scale;
                double logicY = (e.Location.Y - offsetY) / scale;

                // Odrzuć, jeśli poza bitmapą!
                if (logicX < 0 || logicY < 0 || logicX >= bmpWidth || logicY >= bmpHeight)
                {
                    e.Handled = true;
                    return;
                }

                // --- GUMKA ---
                if (vm.SelectedTool == "Eraser")
                {
                    vm.EraserPreviewPosition = new SKPoint((float)logicX, (float)logicY);
                    switch (e.ActionType)
                    {
                        case SKTouchAction.Pressed:
                            vm.IsEraserActive = true;
                            vm.EraseOnSelectedLayer(logicX, logicY);
                            vm.EraserPreviewPosition = new SKPoint((float)logicX, (float)logicY);
                            MainCanvas.InvalidateSurface();
                            break;
                        case SKTouchAction.Moved:
                            if (vm.IsEraserActive)
                            {
                                vm.EraseOnSelectedLayer(logicX, logicY);
                                vm.EraserPreviewPosition = new SKPoint((float)logicX, (float)logicY);
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
                // --- WIADRO ---
                if (vm.SelectedTool == "Bucket")
                {
                    if (e.ActionType == SKTouchAction.Pressed)
                    {
                        var pt = new Microsoft.Maui.Graphics.Point(logicX, logicY);
                        vm.FillBucketCommand.Execute(pt);
                        MainCanvas.InvalidateSurface();
                        e.Handled = true;
                        return;
                    }
                }
                if (vm.SelectedTool == "Pipette")
                {
                    if (e.ActionType == SKTouchAction.Pressed)
                    {
                        int x = (int)logicX;
                        int y = (int)logicY;

                        // Pobór koloru z bitmapy
                        var skColor = bmp.GetPixel(x, y);
                        var pickedColor = Microsoft.Maui.Graphics.Color.FromRgba(
                            skColor.Red, skColor.Green, skColor.Blue, skColor.Alpha);

                        // dodaje slot
                        vm.AddEyedropperColorCommand.Execute(pickedColor);
                        // Ustawia kolor od razu
                        vm.Brush.R = skColor.Red;
                        vm.Brush.G = skColor.Green;
                        vm.Brush.B = skColor.Blue;

                        MainCanvas.InvalidateSurface();
                        e.Handled = true;
                        return;
                    }

                }
                // --- PĘDZEL ---
                switch (e.ActionType)
                {
                    case SKTouchAction.Pressed:
                        vm.StartStrokeOnSelectedLayer();
                        vm.AddStrokePointOnSelectedLayer(logicX, logicY);
                        break;
                    case SKTouchAction.Moved:
                        vm.AddStrokePointOnSelectedLayer(logicX, logicY);
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
            canvas.Clear(new SKColor(66, 66, 66));
            if (vm == null) return;

            // Rozmiar logiczny płótna (bitmapy)
            if (vm.Layers.Count == 0 || vm.Layers[0].Bitmap == null)
                return;
            int bmpWidth = vm.Layers[0].Bitmap.Width;
            int bmpHeight = vm.Layers[0].Bitmap.Height;
            int viewWidth = e.Info.Width;
            int viewHeight = e.Info.Height;

            // Skalowanie i wyśrodkowanie bitmapy
            float scale = Math.Min((float)viewWidth / bmpWidth, (float)viewHeight / bmpHeight);
            float offsetX = (viewWidth - bmpWidth * scale) / 2f;
            float offsetY = (viewHeight - bmpHeight * scale) / 2f;
            var destRect = new SKRect(offsetX, offsetY, offsetX + bmpWidth * scale, offsetY + bmpHeight * scale);

            using (var bgPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = false
            })
            {
                canvas.DrawRect(destRect, bgPaint);
            }

            foreach (var layer in vm.Layers.Where(l => l.IsVisible))
            {
                if (layer.Bitmap != null)
                    canvas.DrawBitmap(layer.Bitmap, destRect);
            }

            using (var borderPaint = new SKPaint
            {
                Color = SKColors.Gray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true
            })
            {
                canvas.DrawRect(destRect, borderPaint);
            }

            // Podgląd gumki (skalowany)
            if (vm.SelectedTool == "Eraser" && vm.EraserPreviewPosition != default)
            {
                // Przekształcanie pozycji gumki (bitmapa -> ekran):
                var pos = vm.EraserPreviewPosition;
                float guiX = (float)(pos.X * scale + offsetX);
                float guiY = (float)(pos.Y * scale + offsetY);
                int radius = vm.Eraser.Size;
                using var borderPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    IsAntialias = true
                };
                canvas.DrawCircle(guiX, guiY, radius * scale / 2f, borderPaint);
            }

            // Podgląd aktualnego rysowania (także skalowany)
            if (vm.SelectedTool == "Brush" && vm.CurrentStrokePoints != null && vm.CurrentStrokePoints.Count > 1)
            {
                var color = new SKColor((byte)vm.Brush.R, (byte)vm.Brush.G, (byte)vm.Brush.B);
                var tip = vm.Brush.TipType;
                using var paint = new SKPaint
                {
                    Color = tip == BrushSettings.BrushTipType.Marker ? color.WithAlpha(120) : color,
                    StrokeWidth = (tip == BrushSettings.BrushTipType.Brush ? vm.Brush.Size * 2f
                                : tip == BrushSettings.BrushTipType.Marker ? vm.Brush.Size * 3f
                                : vm.Brush.Size) * scale,
                    Style = tip == BrushSettings.BrushTipType.Spray ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = tip != BrushSettings.BrushTipType.Crayon,
                    PathEffect = tip == BrushSettings.BrushTipType.Crayon ? SKPathEffect.CreateDash(new float[] { 7, 3 }, 0) : null,
                    MaskFilter = tip == BrushSettings.BrushTipType.Brush ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3) : null
                };

                if (tip != BrushSettings.BrushTipType.Spray)
                {
                    for (int i = 1; i < vm.CurrentStrokePoints.Count; i++)
                    {
                        var p1 = vm.CurrentStrokePoints[i - 1];
                        var p2 = vm.CurrentStrokePoints[i];
                        // Przekształcanie punktów bitmapy na ekran:
                        float x1 = (float)(p1.X * scale + offsetX);
                        float y1 = (float)(p1.Y * scale + offsetY);
                        float x2 = (float)(p2.X * scale + offsetX);
                        float y2 = (float)(p2.Y * scale + offsetY);
                        canvas.DrawLine(x1, y1, x2, y2, paint);
                    }
                    if (tip == BrushSettings.BrushTipType.Marker)
                    {
                        using var circlePaint = new SKPaint { Color = color.WithAlpha(120), Style = SKPaintStyle.Fill };
                        var first = vm.CurrentStrokePoints.First();
                        var last = vm.CurrentStrokePoints.Last();
                        canvas.DrawCircle((float)(first.X * scale + offsetX), (float)(first.Y * scale + offsetY), vm.Brush.Size * 1.5f * scale, circlePaint);
                        canvas.DrawCircle((float)(last.X * scale + offsetX), (float)(last.Y * scale + offsetY), vm.Brush.Size * 1.5f * scale, circlePaint);
                    }
                }
                else // Spray
                {
                    var density = Math.Max(vm.Brush.SprayDensity, 1);
                    var rand = new Random();
                    foreach (var p in vm.CurrentStrokePoints)
                    {
                        for (int i = 0; i < density; i++)
                        {
                            float dx = (float)((rand.NextDouble() - 0.5) * vm.Brush.Size);
                            float dy = (float)((rand.NextDouble() - 0.5) * vm.Brush.Size);
                            float radius = 1.1f * scale;
                            float guiX = (float)((p.X + dx) * scale + offsetX);
                            float guiY = (float)((p.Y + dy) * scale + offsetY);
                            canvas.DrawCircle(guiX, guiY, radius, paint);
                        }
                    }
                }
            }
        }
        //--- Działanie Rysowania ---//

        // Obsługa SaveFile //

        public async Task<string> ShowSaveFileDialog(string suggestedFileName)
        {
            #if WINDOWS
                var picker = new FileSavePicker();
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

            if (BindingContext is MainPageViewModel vm)
                await vm.SaveImageToPath(filePath);
        }
        //------ Obsługa SaveFile ------//
    }
}
