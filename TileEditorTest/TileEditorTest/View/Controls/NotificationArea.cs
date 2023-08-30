using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileEditorTest.View.Controls;
internal class NotificationArea : StackPanel {
    public NotificationArea() {

        this.MaxWidth = 900;
        this.Margin = new(16, 0, 16, 8);
        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom;

        Microsoft.UI.Xaml.Media.Animation.TransitionCollection transitionCollection = new() {
            new Microsoft.UI.Xaml.Media.Animation.AddDeleteThemeTransition()
        };
        this.ChildrenTransitions = transitionCollection;

    }
}
