using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using System;

namespace TileEditorTest.Helper;

internal class BoolVisibilityConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, string language) {

        if (value is bool b) {
            if (parameter is not Visibility trueVisibility) {
                trueVisibility = Visibility.Visible;
            }
            var falseVisibility = trueVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            return b ? trueVisibility : falseVisibility;
        }
        return DependencyProperty.UnsetValue;

    }


    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}
