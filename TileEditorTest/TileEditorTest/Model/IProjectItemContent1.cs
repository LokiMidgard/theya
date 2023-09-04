// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Storage;

namespace TileEditorTest.Model;

public interface IProjectItemContentCreatable<T> : IProjectItemContent<T> where T : class, IProjectItemContent<T> {
    static abstract Task<T> Create(ProjectPath file, CoreViewModel project);

    public abstract static string Extension { get; }

}
public interface IProjectItemContent<T> : IProjectItemContent where T : class, IProjectItemContent<T> {
    public Task Save(ProjectPath path, CoreViewModel project);

    static abstract Task<T> Load(ProjectPath path, CoreViewModel project);
    public abstract static ImmutableArray<Regex> SupportedFilePatterns { get; }
    public abstract static ProjectItemType Type { get; }

}
