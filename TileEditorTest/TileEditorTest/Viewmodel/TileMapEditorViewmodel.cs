using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using TileEditorTest.Model;

using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.ViewModel;

internal class TileMapEditorViewModel : DependencyObject {




    public MapFile? Model {
        get { return (MapFile?)GetValue(ModelProperty); }
        set { SetValue(ModelProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register("Model", typeof(MapFile), typeof(TileMapEditorViewModel), new PropertyMetadata(null, ModelChanged));

    private void ModelChanged(MapFile? oldModel, MapFile? newModel) {

    }
    private static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var me = (TileMapEditorViewModel)d;
        me.ModelChanged(e.OldValue as MapFile, e.NewValue as MapFile);
    }

    public ImmutableList<TileSetFile> TileSets {
        get { return (ImmutableList<TileSetFile>)GetValue(TileSetsProperty); }
        set { SetValue(TileSetsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for TileSets.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TileSetsProperty =
        DependencyProperty.Register("TileSets", typeof(ImmutableList<TileSetFile>), typeof(TileMapEditorViewModel), new PropertyMetadata(ImmutableList<TileSetFile>.Empty));

    public int Width {
        get { return (int)GetValue(WidthProperty); }
        set { SetValue(WidthProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Width.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.Register("Width", typeof(int), typeof(TileMapEditorViewModel), new PropertyMetadata(1024));

    public int Height {
        get { return (int)GetValue(HeigehtProperty); }
        set { SetValue(HeigehtProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Heigeht.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty HeigehtProperty =
        DependencyProperty.Register("Height", typeof(int), typeof(TileMapEditorViewModel), new PropertyMetadata(1024));

    public SizeInt32 TileSize { get; }

    private TileSetForMapViewModel[] tileSetViewmodels;
    private int[,] Infos;

    public async Task Load(CanvasDevice device) {
        Infos = new int[Width, Height];
        Infos[4, 4] = 3;
        Infos[1, 0] = 39;
        tileSetViewmodels = await Task.WhenAll(TileSets.Select(x => TileSetForMapViewModel.LoadAsync(device, x, null)));
    }

    public void Draw(CanvasSpriteBatch spriteBatch, RectInt32 drawingLocation) {
        if (tileSetViewmodels is null) {
            return;
        }

        // Get the to drawing tiles
        var top = (drawingLocation.Y - drawingLocation.Y % TileSize.Height) / TileSize.Height;
        var left = (drawingLocation.X - drawingLocation.X % TileSize.Width) / TileSize.Width;
        var bottom = (drawingLocation.Y + drawingLocation.Height + (drawingLocation.Y + drawingLocation.Height) % TileSize.Height) / TileSize.Height;
        var right = (drawingLocation.X + drawingLocation.Width + (drawingLocation.X + drawingLocation.Width) % TileSize.Width) / TileSize.Width;

        bottom = Math.Min(bottom, Height);
        right = Math.Min(right, Width);

        for (int x = left; x < right; x++) {
            for (int y = top; y < bottom; y++) {
                for (int z = 0; z < tileSetViewmodels.Length; z++) {
                    tileSetViewmodels[z].Draw(spriteBatch, Infos[x, y], x, y);
                }
            }
        }
    }

    public TileMapEditorViewModel(SizeInt32 tileSize) {
        TileSize = tileSize;
    }

}

