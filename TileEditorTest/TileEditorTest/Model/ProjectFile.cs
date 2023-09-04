// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;


namespace TileEditorTest.Model;

internal partial class ProjectFile : JsonProjectItem<ProjectFile>, IProjectItemContent<ProjectFile> {
    public static ImmutableArray<Regex> SupportedFilePatterns { get; } = ImmutableArray.Create(ProjectExtensionRegex());
    static Task<ProjectFile> IProjectItemContent<ProjectFile>.Load(ProjectPath path, CoreViewModel project) => Load(path, project);

    public string? Name { get; set; }
    public Version? Version { get; set; }

    public static ProjectItemType Type => ProjectItemType.Other;

    [GeneratedRegex(".project.json$")]
    private static partial Regex ProjectExtensionRegex();
}
