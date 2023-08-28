using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest;
public sealed partial class TileMapEditorControl : UserControl {

    private bool spriteBatchSupported;
    private TileSetForMapViewModel? tileSet;
    private CanvasControl? canvas;

    internal TileMapEditorViewModel? Viewmodel => this.DataContext as TileMapEditorViewModel;

    public TileMapEditorControl() {
        this.InitializeComponent();
        this.DataContextChanged += (FrameworkElement sender, DataContextChangedEventArgs args) => {
            if (args.NewValue is TileMapEditorViewModel vm) {
                InitNewCanvas();
            } else {
                CleanupCanvas();
            }
        };

        this.Unloaded += (s, e) => {
            this.CleanupCanvas();
        };
    }

    private void InitNewCanvas() {

        if (this.canvas != null) {
            this.CleanupCanvas();
        }

        var canvas = new CanvasControl();
        canvas.Draw += this.CanvasControl_Draw;
        canvas.ClearColor = new Windows.UI.Color() { R = 100, G = 149, B = 237, A = 255 };
        canvas.CreateResources += this.CanvasControl_CreateResources;

        this.canvas = canvas;
        this.canvasHolder.Content = canvas;
    }

    private void CleanupCanvas() {
        this.canvas?.RemoveFromVisualTree();
        this.canvas = null;
    }

    private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args) {
        if (Viewmodel is null) {
            return;
        }

        var width = args.DrawingSession.ConvertDipsToPixels((float)sender.Size.Width, CanvasDpiRounding.Ceiling);
        var height = args.DrawingSession.ConvertDipsToPixels((float)sender.Size.Height, CanvasDpiRounding.Ceiling);

        var totalWidth = this.Viewmodel.Width * this.Viewmodel.TileSize.Width;
        var totalHeight = this.Viewmodel.Height * this.Viewmodel.TileSize.Height;

        var scrollHeight = totalHeight - height;
        var scrollWidth = totalWidth - width;
        if (scrollHeight > 0) {
            verticalScroll.Maximum = scrollHeight;
            verticalScroll.Visibility = Visibility.Visible;
        } else {
            verticalScroll.Visibility = Visibility.Collapsed;
            verticalScroll.Value = 0;
        }
        if (scrollWidth > 0) {
            horizontlScroll.Maximum = scrollWidth;
            horizontlScroll.Visibility = Visibility.Visible;
        } else {
            horizontlScroll.Visibility = Visibility.Collapsed;
            horizontlScroll.Value = 0;
        }
        var transformation = Matrix3x2.Identity;
        transformation.Translation = new((int)-horizontlScroll.Value, (int)-verticalScroll.Value);
        args.DrawingSession.Transform = transformation;
        using var spriteBatch = args.DrawingSession.CreateSpriteBatch(Microsoft.Graphics.Canvas.CanvasSpriteSortMode.None, Microsoft.Graphics.Canvas.CanvasImageInterpolation.NearestNeighbor, Microsoft.Graphics.Canvas.CanvasSpriteOptions.ClampToSourceRect);
        this.Viewmodel.Draw(spriteBatch, new Windows.Graphics.RectInt32((int)horizontlScroll.Value, (int)verticalScroll.Value, width, height));
    }

    private void CanvasControl_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args) {
        spriteBatchSupported = CanvasSpriteBatch.IsSupported(sender.Device);

        if (!spriteBatchSupported) {
            return;
        }
        
        args.TrackAsyncAction(LoadImages(sender.Device).AsAsyncAction());
    }

    private async Task LoadImages(CanvasDevice device) {
        if (this.Viewmodel is null) {
            return;
        }

        await this.Viewmodel.Load(device);
    }

    private void scrollChanged(object sender, ScrollEventArgs e) {
        canvas?.Invalidate();

    }
}
