// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Graphics;
using Windows.Storage;

namespace TileEditorTest.Model;


internal class RootPlaceholderFile : ProjectItemContent, IProjectItemContent<RootPlaceholderFile> {
    public static ImmutableArray<Regex> SupportedFilePatterns { get; } = ImmutableArray<Regex>.Empty;
    static Task<RootPlaceholderFile> IProjectItemContent<RootPlaceholderFile>.Load(ProjectPath path, CoreViewModel project) => Task.FromResult(new RootPlaceholderFile());



    public static ProjectItemType Type => ProjectItemType.Other;

    public Task Save(ProjectPath path, CoreViewModel project) {
        return Task.CompletedTask;
    }


}
