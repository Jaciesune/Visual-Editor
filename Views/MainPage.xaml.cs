using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace VE.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            BindingContext = new VE.ViewModels.MainPageViewModel();
        }

        private bool _brushOpen = false;
        private async void ShowBrushOptions(object sender, EventArgs e)
        {
            if (_brushOpen)
            {
                await BrushOptionsPanel.TranslateTo(-200, 0, 170, Easing.CubicIn); // chowamy w lewo
                BrushOptionsPanel.IsVisible = false;
                _brushOpen = false;
            }
            else
            {
                BrushOptionsPanel.TranslationX = -200;
                BrushOptionsPanel.IsVisible = true;
                await BrushOptionsPanel.TranslateTo(0, 0, 230, Easing.CubicOut); // wjeżdża z lewej
                _brushOpen = true;
            }
        }
        private void MainCanvas_Touch(object sender, SKTouchEventArgs e)
        {
            if (BindingContext is VE.ViewModels.MainPageViewModel vm)
            {
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

        private void MainCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var vm = BindingContext as VE.ViewModels.MainPageViewModel;
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            if (vm == null) return;

            foreach (var stroke in vm.Strokes)
            {
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
        }
    }
}
