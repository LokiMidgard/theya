// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.WinUI.Helpers;

using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.UI;

namespace TileEditorTest.Model;

internal partial class TerrainsFile : JsonProjectItem<TerrainsFile>, IProjectItemContent<TerrainsFile>, IProjectItemContentCreatable<TerrainsFile> {
    public static ImmutableArray<Regex> SupportedFilePatterns { get; } = ImmutableArray.Create(ProjectExtensionRegex());
    static Task<TerrainsFile> IProjectItemContent<TerrainsFile>.Load(ProjectPath path, CoreViewModel project) => Load(path, project);

    public required Terrain[] Terrains { get; set; }

    public static ProjectItemType Type => ProjectItemType.Other;

    public static string Extension => ".terrains.json";

    [GeneratedRegex(".terrains.json$")]
    private static partial Regex ProjectExtensionRegex();

    public static async Task<TerrainsFile> Create(ProjectPath file, CoreViewModel project) {
        TerrainsFile result = new() { Terrains = Array.Empty<Terrain>() };
        await result.Save(file, project);
        return result;
    }
}

internal record TileImage(string TileSetPath, int x, int y);

internal record Terrain(string Name, TerranType Type, TerrainColor Color, int Opacity, TileImage? Image);

internal record TerrainColor(string Color) {
    public static implicit operator Color(TerrainColor color) { return color.Color.ToColor(); }
    public static implicit operator TerrainColor(Color color) { return new(color.ToHex()); }
}