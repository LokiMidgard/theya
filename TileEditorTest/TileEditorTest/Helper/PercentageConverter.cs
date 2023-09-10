using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;

using System;
using System.Collections.Generic;
using System.Linq;

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

[ContentProperty(Name = nameof(DataTemplate))]
internal class TemplateSelector {
    public Type? Type { get; set; }
    public DataTemplate? DataTemplate { get; set; }
}

[ContentProperty(Name = nameof(Items))]
internal class TreeDataTemplateSelector : DataTemplateSelector {

    public TreeDataTemplateSelector() {
        this.Items = new();
    }
    public List<TemplateSelector> Items { get; }



    protected override DataTemplate SelectTemplateCore(object item) {

        var template = Items.FirstOrDefault(x => x.Type?.IsAssignableFrom(item.GetType()) ?? false);
        return template?.DataTemplate is not null
            ? template.DataTemplate
            : base.SelectTemplateCore(item);
    }
}
