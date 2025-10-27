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
