using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using System;

namespace TileEditorTest.Helper;

internal class NullToHiddenConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, string language) {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }


    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}
