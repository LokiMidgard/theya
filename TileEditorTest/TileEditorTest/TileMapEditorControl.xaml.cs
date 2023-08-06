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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using TileEditorTest.Viewmodel;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest;
public sealed partial class TileMapEditorControl : UserControl
{

    private bool spriteBatchSupported;
    private TileSetViewmodel? tileSet;
    private CanvasControl? canvas;

    internal TileMapEditorViewmodel? Viewmodel => this.DataContext as TileMapEditorViewmodel;


    public TileMapEditorControl()
    {
        this.InitializeComponent();
        this.DataContextChanged += (FrameworkElement sender, DataContextChangedEventArgs args) =>
        {
            if (args.NewValue is TileMapEditorViewmodel vm)
            {
                InitNewCanvas();
            }
            else
            {
                CleanupCanvas();
            }
        };

        this.Unloaded += (s, e) =>
        {
            this.CleanupCanvas();
        };
    }


    private void InitNewCanvas()
    {

        if (this.canvas != null)
        {
            this.CleanupCanvas();
        }

        var canvas = new CanvasControl();
        canvas.Draw += this.CanvasControl_Draw;
        canvas.ClearColor = new Windows.UI.Color() { R = 100, G = 149, B = 237, A = 255 };
        canvas.CreateResources += this.CanvasControl_CreateResources;

        this.canvas = canvas;
        this.Content = canvas;
    }

    private void CleanupCanvas()
    {
        this.canvas?.RemoveFromVisualTree();
        this.canvas = null;
    }

    private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        if (Viewmodel is null)
        {
            return;
        }
        using var spriteBatch = args.DrawingSession.CreateSpriteBatch(Microsoft.Graphics.Canvas.CanvasSpriteSortMode.None, Microsoft.Graphics.Canvas.CanvasImageInterpolation.NearestNeighbor, Microsoft.Graphics.Canvas.CanvasSpriteOptions.ClampToSourceRect);

        var width = spriteBatch.ConvertDipsToPixels((float)sender.Size.Width, CanvasDpiRounding.Ceiling);
        var height = spriteBatch.ConvertDipsToPixels((float)sender.Size.Height, CanvasDpiRounding.Ceiling);
        this.Viewmodel.Draw(spriteBatch, new Windows.Graphics.RectInt32(0, 0, width, height));
    }

    private void CanvasControl_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {
        spriteBatchSupported = CanvasSpriteBatch.IsSupported(sender.Device);

        if (!spriteBatchSupported)
            return;

        args.TrackAsyncAction(LoadImages(sender.Device).AsAsyncAction());
    }

    async Task LoadImages(CanvasDevice device)
    {
        if (this.Viewmodel is null)
        {
            return;
        }

        await this.Viewmodel.Load(device);
    }
}
