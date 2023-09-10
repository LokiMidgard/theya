using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using TileEditorTest.Model;
using TileEditorTest.ViewModel;
using TileEditorTest.ViewModel.Controls;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View.Controls;
public sealed partial class TileImageSelector : UserControl {
    [Notify]
    private TileImageSelectorViewModel? viewModel;

    [Notify]
    private ReadOnlyObservableCollection<ProjectPath>? allTileSetItemPaths;



    private void OnViewModelChanged() {
        allTileSetItemPaths = viewModel?.Core.GetProjectItemCollectionOfType<TileSetFile>();
    }


    private double ConvertHeight(int y, int width) {
        return y * width;
    }


    public TileImageSelector() {
        this.InitializeComponent();
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e) {
        mouseOverRect.Visibility = Visibility.Visible;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e) {
        mouseOverRect.Visibility = Visibility.Collapsed;
    }

    private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e) {
        var point = e.GetCurrentPoint(canvas).Position;
        Canvas.SetTop(mouseOverRect, (int)(point.Y / mouseOverRect.Height) * mouseOverRect.Height);
        Canvas.SetLeft(mouseOverRect, (int)(point.X / mouseOverRect.Width) * mouseOverRect.Width);
    }

    private void Grid_Tapped(object sender, TappedRoutedEventArgs e) {
        if (ViewModel is null) {
            return;
        }
        var point = e.GetPosition(canvas);
        this.ViewModel.Y = (int)(point.Y / mouseOverRect.Height);
        this.ViewModel.X = (int)(point.X / mouseOverRect.Width);

    }
}
