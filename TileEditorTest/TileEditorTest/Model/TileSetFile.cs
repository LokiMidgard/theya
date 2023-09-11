// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Graphics;
using Windows.Storage;

namespace TileEditorTest.Model;

public partial class TileSetFile : JsonProjectItem<TileSetFile>, IProjectItemContentCreatable<TileSetFile> {
    public static ImmutableArray<Regex> SupportedFilePatterns { get; } = ImmutableArray.Create(TileSetExtensionRegex());
    static Task<TileSetFile> IProjectItemContent<TileSetFile>.Load(ProjectPath path, CoreViewModel project) => Load(path, project);

    public ProjectItem<ImageFile>? Image { get; set; }
    public static ProjectItemType Type => ProjectItemType.TileSet;

    public TileSize? TileSize { get; set; }
    public int Columns { get; set; }

    public TileData[] TileData { get; set; } = Array.Empty<TileData>();

    public static string Extension => ".tileset.json";

    [GeneratedRegex(".tileset.json$")]
    private static partial Regex TileSetExtensionRegex();

    public static async Task<TileSetFile> Create(ProjectPath path, CoreViewModel project) {
        var result = new TileSetFile() { TileSize = new(32, 32) };
        await result.Save(path, project);
        return result;
    }

}

public record struct TileSize(int Width, int Height);
public record struct TileData(TerrainIdData Terrain);
public record struct TerrainIdData(TerrainId? TopLeft, TerrainId? Top, TerrainId? TopRight, TerrainId? Left, TerrainId? Center, TerrainId? Right, TerrainId? BottomLeft, TerrainId? Bottom, TerrainId? BottomRight);
public record TerrainId(string Terrain, int Index, TerranType Type);
