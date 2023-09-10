using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

using System;

namespace TileEditorTest.Helper;

internal class ReturnParameterOnTrueConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, string language) {


        if (value is bool b && b != true) {
            return new Style() {
                TargetType = typeof(MenuFlyoutPresenter),
                Setters = {
                    new Setter() {
                          Property= UIElement.VisibilityProperty,
                           Value= Visibility.Collapsed
                    }
                }
                
            };
        }

        return null!;
    }


    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}
