using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileEditorTest.Helper;
public class MenuBinding {



    public static object GetItemsSource(MenuFlyoutSubItem obj) {
        return (IEnumerable<object>)obj.GetValue(ItemsSourceProperty);
    }

    public static void SetItemsSource(MenuFlyoutSubItem obj, object value) {
        obj.SetValue(ItemsSourceProperty, value);
    }

    // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.RegisterAttached("ItemsSource", typeof(object), typeof(MenuBinding), new PropertyMetadata(Enumerable.Empty<object>(), (sender, e) => {
            var me = (MenuFlyoutSubItem)sender;
            me.Items.Clear();

            if (e.NewValue is not IEnumerable<object> enumerable) {
                enumerable = Enumerable.Empty<object>();
            }

            var dataTemplate = GetItemsTemplate(me);
            foreach (var item in enumerable) {

                object element;
                if (dataTemplate is not null) {
                    element = dataTemplate.GetElement(new ElementFactoryGetArgs() { Data = item, Parent = me });
                    if (element is FrameworkElement frameworkElement) {
                        frameworkElement.DataContext = item;
                    }

                } else {
                    element = item;
                }

                if (element is not MenuFlyoutItemBase subItem) {
                    subItem = new MenuFlyoutItem() { Text = element.ToString() };
                }

                me.Items.Add(subItem);
            }

        }));




    public static DataTemplate GetItemsTemplate(MenuFlyoutSubItem obj) {
        return (DataTemplate)obj.GetValue(ItemsTemplateProperty);
    }

    public static void SetItemsTemplate(MenuFlyoutSubItem obj, DataTemplate value) {
        obj.SetValue(ItemsTemplateProperty, value);
    }

    // Using a DependencyProperty as the backing store for ItemsTemplate.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ItemsTemplateProperty =
        DependencyProperty.RegisterAttached("ItemsTemplate", typeof(DataTemplate), typeof(MenuBinding), new PropertyMetadata(null));




}
