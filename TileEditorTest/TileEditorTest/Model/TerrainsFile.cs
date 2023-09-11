// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.WinUI.Helpers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
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

internal record Terrain(string Name, TileImage? Image, TerrainColor Color, TerrainForm? Floor, TerrainForm? Wall, TerrainForm? Cut) {
    private Guid? guid;
    /// <summary>
    /// Used to identify changes on a Model. If Every Value is changed and maybe the position, we still need to find the original file.
    /// </summary>
    [JsonIgnore]
    public Guid FileLoadGuid {
        get {
            guid ??= Guid.NewGuid();
            return guid.Value;
        }
        set => guid = value;
    }

    public static readonly IEqualityComparer<Terrain> IdEqualityComparer = new IdComparerImpl();

    private class IdComparerImpl : IEqualityComparer<Terrain> {
        public bool Equals(Terrain? x, Terrain? y) {
            if (ReferenceEquals(x, y)) {
                return true;
            }
            if (x is null || y is null) {
                return false;
            }
            return EqualityComparer<Guid?>.Default.Equals(x.FileLoadGuid, y.FileLoadGuid);
        }

        public int GetHashCode([DisallowNull] Terrain obj) {
            return obj.FileLoadGuid.GetHashCode();
        }
    }

}
internal record TerrainForm(int Opacity);
internal record TerrainColor(string Color) {
    public static implicit operator Color(TerrainColor color) { return color.Color.ToColor(); }
    public static implicit operator TerrainColor(Color color) { return new(color.ToHex()); }
}