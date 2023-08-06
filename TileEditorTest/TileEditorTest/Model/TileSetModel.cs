using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.Model;

public class TileSetModel
{
    public required string Path { get; init; }
    public required SizeInt32 TileSize { get; init; }
}

