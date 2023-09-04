// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Storage;

namespace TileEditorTest.Model;

internal partial class MapFile : JsonProjectItem<MapFile>, IProjectItemContent<MapFile> {

    public int Width { get; set; }
    public int Height { get; set; }
    public int Depth { get; set; }

    public List<ProjectItem<TileSetFile>> TileSets { get; set; } = new();

    public static ImmutableArray<Regex> SupportedFilePatterns { get; } = ImmutableArray.Create(MapExtensionRegex());
    static Task<MapFile> IProjectItemContent<MapFile>.Load(ProjectPath path, CoreViewModel project) => Load(path, project);
    public static ProjectItemType Type => ProjectItemType.Map;
    [GeneratedRegex(".map.json$")]
    private static partial Regex MapExtensionRegex();
}
