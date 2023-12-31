﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TileEditorTest.Model;
using TileEditorTest.ViewModel;

namespace TileEditorTest.Helper;

internal class EmptyCollectionsHiddenConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, string language) {
        bool? result = value switch {
            ICollection list => list.Count > 0,
            IEnumerable enumerable => enumerable.OfType<object>().Any(),
            _ => null as bool?
        };
        return result.HasValue
            ? result.Value
                ? Visibility.Visible
                : Visibility.Collapsed
            : DependencyProperty.UnsetValue;
    }


    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}

public class ThisConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}