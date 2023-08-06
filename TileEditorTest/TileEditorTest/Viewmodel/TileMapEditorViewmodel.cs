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

namespace TileEditorTest.Viewmodel;

public class TileMapEditorViewmodel : DependencyObject
{

    //public ObservableCollection<TileMapTileInfo> Tiles { get; }




    public ImmutableList<TileSetModel> TileSets
    {
        get { return (ImmutableList<TileSetModel>)GetValue(TileSetsProperty); }
        set { SetValue(TileSetsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for TileSets.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TileSetsProperty =
        DependencyProperty.Register("TileSets", typeof(ImmutableList<TileSetModel>), typeof(TileMapEditorViewmodel), new PropertyMetadata(ImmutableList<TileSetViewmodel>.Empty));




    public int Width
    {
        get { return (int)GetValue(WidthProperty); }
        set { SetValue(WidthProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Width.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.Register("Width", typeof(int), typeof(TileMapEditorViewmodel), new PropertyMetadata(1024));



    public int Heigeht
    {
        get { return (int)GetValue(HeigehtProperty); }
        set { SetValue(HeigehtProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Heigeht.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty HeigehtProperty =
        DependencyProperty.Register("Heigeht", typeof(int), typeof(TileMapEditorViewmodel), new PropertyMetadata(1024));


    public SizeInt32 TileSize { get; }


    private TileSetViewmodel[] tileSetViewmodels;
    private int[,] Infos;

    public async Task Load(CanvasDevice device)
    {
        Infos = new int[Width, Heigeht];
        tileSetViewmodels = await Task.WhenAll(TileSets.Select(x => TileSetViewmodel.LoadAsync(device, x)));
    }

    public void Draw(CanvasSpriteBatch spriteBatch, RectInt32 drawingLocation)
    {
        if (tileSetViewmodels is null)
        {
            return;
        }
        var top = drawingLocation.Y - drawingLocation.Y % TileSize.Height;
        var left = drawingLocation.X - drawingLocation.X % TileSize.Width;
        var bottom = drawingLocation.Y + drawingLocation.Height + (drawingLocation.Y + drawingLocation.Height) % TileSize.Height;
        var right = drawingLocation.X + drawingLocation.Width + (drawingLocation.X + drawingLocation.Width) % TileSize.Height;

        bottom = Math.Min(bottom, Heigeht);
        right = Math.Min(right, Width);


        for (int x = left; x < right; x++)
            for (int y = top; y < bottom; y++)
                for (int z = 0; z < tileSetViewmodels.Length; z++)
                {
                    tileSetViewmodels[z].Draw(spriteBatch, Infos[x, y], x, y);
                }


    }



    public TileMapEditorViewmodel(SizeInt32 tileSize)
    {
        TileSize = tileSize;
    }

}

