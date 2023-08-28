// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Storage;

namespace TileEditorTest.Model;

public abstract partial class ProjectItem {
    public ProjectPath Path { get; }
    public ProjectViewModel Project { get; }

    public abstract ProjectItemType Type { get; }

    static ProjectItem() {
        InitProjectItems();
    }
    public ProjectItem(ProjectPath path, ProjectViewModel project) {
        Path = path;
        Project = project ?? throw new ArgumentNullException(nameof(project));
    }

    public Task<IProjectItemContent> Content => GetContent();

    protected abstract Task<IProjectItemContent> GetContent();

    public static async Task<ProjectItem?> Create(ProjectPath path, ProjectViewModel project) {
        var item = await path.ToStorageItem(project);

        if (item is StorageFolder folder) { // special case folder
            return new ProjectItem<Folder>(path, project);
        }

        var generator = generators.Where(x => x.pattern.IsMatch(item.Name)).SingleOrDefault().generator;
        if (generator is null) {
            return null;
        }
        return generator(path, project);

    }
    private static List<(Regex pattern, Func<ProjectPath, ProjectViewModel, ProjectItem> generator)> generators = new();
    [AutoInvoke.FindAndInvoke]
    private static void InitProjectItems<T>() where T : class, IProjectItemContent<T> {
        foreach (var pattern in T.SupportedFilePatterns) {
            Func<ProjectPath, ProjectViewModel, ProjectItem<T>> generator = (ProjectPath path, ProjectViewModel project) => new ProjectItem<T>(path, project);
            generators.Add((pattern, generator));
        }
    }

}
