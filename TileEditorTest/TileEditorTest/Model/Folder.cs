// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Storage;

namespace TileEditorTest.Model;

internal partial class Folder : ProjectItemContent, IProjectItemContentCreatable<Folder> {
    public static ImmutableArray<Regex> SupportedFilePatterns { get; } = ImmutableArray.Create<Regex>();

    public static ProjectItemType Type => ProjectItemType.Folder;

    public static string Extension => "";

    public static async Task<Folder> Create(ProjectPath file, CoreViewModel project) {
        await file.ToStorageFolder(project);
        return new Folder();
    }

    public static async Task<Folder> Load(ProjectPath path, CoreViewModel project) {
        await path.ToStorageFolder(project);
        return new Folder();
    }

    public Task Save(ProjectPath path, CoreViewModel project) {
        throw new NotImplementedException();
    }
}
