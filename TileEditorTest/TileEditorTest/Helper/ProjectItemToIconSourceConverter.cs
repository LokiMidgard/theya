using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TileEditorTest.Model;
using TileEditorTest.ViewModel;

namespace TileEditorTest.Helper;

internal class ProjectItemToIconSourceConverter : IValueConverter {
    private static SymbolIconSource mapSymbol = new() { Symbol = Symbol.Map };
    private static SymbolIconSource picturesSymbol = new() { Symbol = Symbol.Pictures };
    private static SymbolIconSource audioSymbol = new() { Symbol = Symbol.Audio };
    private static SymbolIconSource folderSymbol = new() { Symbol = Symbol.Folder };
    private static SymbolIconSource tileSetSymbol = new() { Symbol = Symbol.ViewAll };
    private static SymbolIconSource otherSymbol = new() { Symbol = Symbol.Page };


    public object Convert(object value, Type targetType, object parameter, string language) {
        if (value is not ProjectItemType itemType) {
            return DependencyProperty.UnsetValue;
        }

        return Convert(itemType);
    }

    public static IconSource Convert(ProjectItemType itemType) {
        return itemType switch {
            ProjectItemType.Map => mapSymbol,
            ProjectItemType.Image => picturesSymbol,
            ProjectItemType.Audio => audioSymbol,
            ProjectItemType.Folder => folderSymbol,
            ProjectItemType.TileSet => tileSetSymbol,
            ProjectItemType.Other => otherSymbol,
            _ => throw new NotImplementedException($"Type {itemType} is not handled.")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}
