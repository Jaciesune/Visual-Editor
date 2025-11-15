using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VE.Tools
{
    public class SelectedLayerColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var currentLayer = value as VE.Models.Layer;
            // parameter tu to x:Reference do MainPageRoot
            var mainPageRoot = parameter as VE.Views.MainPage;
            var mainPageVM = mainPageRoot?.BindingContext as VE.ViewModels.MainPageViewModel;

            if (mainPageVM != null && mainPageVM.SelectedLayer == currentLayer)
                return "#616161"; // zaznaczone tło
            return "#3D3D3D";   // normalne tło
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
