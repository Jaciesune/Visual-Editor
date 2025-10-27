using Microsoft.Maui.Controls;

namespace VE.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            RValue.TextChanged += OnRgbChanged;
            GValue.TextChanged += OnRgbChanged;
            BValue.TextChanged += OnRgbChanged;
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

        private void OnRgbChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(RValue.Text, out int r) &&
                int.TryParse(GValue.Text, out int g) &&
                int.TryParse(BValue.Text, out int b))
            {
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);
                BrushPreview.BackgroundColor = Color.FromRgb(r, g, b);
            }
        }
    }
}
