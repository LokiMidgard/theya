using Microsoft.Graphics.Canvas;

using System.Threading.Tasks;

using TileEditorTest.Model;

using Windows.Foundation;
using Windows.Graphics;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.Viewmodel;

public class TileSetViewmodel
{

    private TileSetViewmodel(CanvasBitmap bitmap, TileSetModel tileSetModel)
    {
        Bitmap = bitmap;
        TileSetModel = tileSetModel;
        TilesetSize = new SizeInt32((int)bitmap.Size.Width / tileSetModel.TileSize.Width, (int)bitmap.Size.Height / tileSetModel.TileSize.Height);
    }

    public CanvasBitmap Bitmap { get; }
    public TileSetModel TileSetModel { get; }
    public SizeInt32 TilesetSize { get; }

    public static async Task<TileSetViewmodel> LoadAsync(CanvasDevice device, TileSetModel tileSetModel)
    {
        var bitmap = await CanvasBitmap.LoadAsync(device, tileSetModel.Path);


        return new TileSetViewmodel(bitmap, tileSetModel);
    }

    public void Draw(CanvasSpriteBatch spriteBatch, int tileId, int x, int y)
    {
        spriteBatch.DrawFromSpriteSheet(Bitmap, new Rect(x * TileSetModel.TileSize.Width, y * TileSetModel.TileSize.Height, TileSetModel.TileSize.Width, TileSetModel.TileSize.Height), GetSourceRect(tileId));
    }

    private Rect GetSourceRect(int tileId)
    {
        int row = tileId / TilesetSize.Width;
        int column = tileId % TilesetSize.Width;

        return new Rect(
  TileSetModel.TileSize.Width * column,
  TileSetModel.TileSize.Height * row,
  TileSetModel.TileSize.Width,
  TileSetModel.TileSize.Height);
    }

}

