// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

namespace TileEditorTest.Model;

[JsonConverter(typeof(ProjectItemConverter))]
internal class ProjectItem<T> : ProjectItem where T : class, IProjectItemContent<T> {
    private WeakReference<T>? oldReference;

    private Task<T>? ongoingTask;

    public ProjectItem(ProjectPath path, ProjectViewModel project) : base(path, project) {
    }

    public new Task<T> Content {
        get {
            if (oldReference is not null && oldReference.TryGetTarget(out var old)) {
                return Task.FromResult(old);
            } else if (ongoingTask is not null) {
                return ongoingTask;
            } else {
                var task = T.Load(Path, Project)
                    .ContinueWith(c => {
                        var newContent = c.Result;
                        oldReference = new WeakReference<T>(newContent);
                        ongoingTask = null;
                        return newContent;
                    });
                ongoingTask = task;
                return task;
            }
        }
    }

    public override ProjectItemType Type => T.Type;

    protected override async Task<IProjectItemContent> GetContent() {
        return await Content;
    }
}


