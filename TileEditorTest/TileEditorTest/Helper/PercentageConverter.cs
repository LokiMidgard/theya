using Microsoft.UI.Xaml.Data;

using System;

namespace TileEditorTest.Helper;

internal class PercentageConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        if (value is double d) {
            return $"{(int)(d * 100)}%";
        }
        if (value is float f) {
            return $"{(int)(f * 100)}%";
        }
        return "???";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}