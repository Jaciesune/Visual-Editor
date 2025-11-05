using Microsoft.Maui.Controls;

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
    }
}
