using Microsoft.Graphics.Canvas;

using System.Threading.Tasks;

using TileEditorTest.Model;

using Windows.Foundation;
using Windows.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.ViewModel;

internal class TileSetForMapViewModel {

    private TileSetForMapViewModel(CanvasBitmap bitmap, TileSetFile fileModel) {
        Bitmap = bitmap;
        var tileSetModel = fileModel;
        TileSetModel = tileSetModel;
        TilesetSize = new SizeInt32((int)bitmap.Size.Width / tileSetModel.TileSize.Value.Width, (int)bitmap.Size.Height / tileSetModel.TileSize.Value.Height);
    }

    public CanvasBitmap Bitmap { get; }
    public TileSetFile TileSetModel { get; }
    public SizeInt32 TilesetSize { get; }

    public static async Task<TileSetForMapViewModel> LoadAsync(CanvasDevice device, TileSetFile tileSetModel, ProjectViewModel project) {
        var image = await tileSetModel.Image.Content;
        using var imageStream = await image.LoadAsync();
        var canvas = await CanvasBitmap.LoadAsync(device, imageStream);

        return new TileSetForMapViewModel(canvas, tileSetModel);
    }

    public void Draw(CanvasSpriteBatch spriteBatch, int tileId, int x, int y) {
        spriteBatch.DrawFromSpriteSheet(Bitmap, new Rect(x * TileSetModel.TileSize.Value.Width, y * TileSetModel.TileSize.Value.Height, TileSetModel.TileSize.Value.Width, TileSetModel.TileSize.Value.Height), GetSourceRect(tileId));
    }

    private Rect GetSourceRect(int tileId) {
        int row = tileId / TilesetSize.Width;
        int column = tileId % TilesetSize.Width;

        return new Rect(
            TileSetModel.TileSize.Value.Width * column,
            TileSetModel.TileSize.Value.Height * row,
            TileSetModel.TileSize.Value.Width,
            TileSetModel.TileSize.Value.Height);
    }

}

