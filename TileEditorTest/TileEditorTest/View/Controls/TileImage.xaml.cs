using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using TileEditorTest.ViewModel.Controls;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View.Controls;
public sealed partial class TileImage : UserControl {

    [Notify]
    private TileImageSelectorViewModel? viewModel;

    private Rect ToRect(int x, int y, int tileWidth, int tileHeight) {
        return new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
    }

    private TranslateTransform ToTransform(int x, int y, int tileWidth, int tileHeight) {
        var rect = ToRect(x, y, tileWidth, tileHeight);
        return new TranslateTransform() { X = -rect.X, Y = -rect.Y };
    }
    private double ToHeight(int x, int y, int tileWidth, int tileHeight) {
        var rect = ToRect(x, y, tileWidth, tileHeight);
        return rect.Y + rect.Height;
    }
    private double ToWidth(int x, int y, int tileWidth, int tileHeight) {
        var rect = ToRect(x, y, tileWidth, tileHeight);
        return rect.X + rect.Width;
    }
    private Thickness ToMargin(int x, int y, int tileWidth, int tileHeight) {
        var rect = ToRect(x, y, tileWidth, tileHeight);
        return new Thickness(0, 0, -rect.X, -rect.Y);
    }

    public TileImage() {
        this.InitializeComponent();
    }
}
