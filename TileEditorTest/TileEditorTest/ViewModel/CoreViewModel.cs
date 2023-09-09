using EnumFastToStringGenerated;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.Model;

using Windows.Storage;

namespace TileEditorTest.ViewModel;



public partial class CoreViewModel {
    private record ProjectStruct {

        public string? Name { get; set; }
        public Version? Version { get; set; }
    }
    public string Name { get; }
    public StorageFolder RootFolder { get; }

    public ProjectTreeElementViewModel Root { get; private set; }
    public IEnumerable<ProjectTreeElementViewModel> RootItemsSource => Enumerable.Repeat(Root, 1);

    private ImmutableDictionary<ProjectItemType, ObservableCollection<ProjectPath>> pathByType;

    private static readonly List<(Regex pattern, Func<ProjectPath, CoreViewModel, Task<IProjectItemContent>> loders)> loaders = new();

    public event Func<ProjectItemType, Predicate<string>?, Task<string?>>? OnShowNewFileDialog;
    public event Func<ProjectItem, Task>? OnOpenFile;


    internal Task OpenFile(ProjectItem type) {
        return OnOpenFile?.Invoke(type) ?? Task.CompletedTask;
    }
    internal Task<string?> ShowNewFileDialog(ProjectItemType type, Predicate<string>? isAllowed = null) {
        return OnShowNewFileDialog?.Invoke(type, isAllowed) ?? Task.FromResult<string?>(null);
    }


    static CoreViewModel() {
        InitProjectItemContentLoader();
    }
    private CoreViewModel(Func<CoreViewModel, Task<ProjectTreeElementViewModel>> root, ProjectStruct projectData, StorageFolder rootFolder, TaskCompletionSource waitForLoadReady) {
        this.pathByType = ProjectItemTypeEnumExtensions.GetValuesFast().ToImmutableDictionary(x => x, x => new ObservableCollection<ProjectPath>());
        this.Name = projectData.Name ?? "Unbenannt";
        this.RootFolder = rootFolder;
        Root = null!;// This will be set before the create method returns
        root(this).ContinueWith(t => Root = t.Result).ContinueWith(x => waitForLoadReady.SetResult());
    }


    [AutoInvoke.FindAndInvoke]
    private static void InitProjectItemContentLoader<T>() where T : class, IProjectItemContent<T> {
        foreach (var extension in T.SupportedFilePatterns) {
            loaders.Add((extension, async (file, project) => await T.Load(file, project)));
        }
    }

    public static async Task<CoreViewModel> Load(StorageFile projectFile) {

        var rootFolder = (await projectFile.GetParentAsync()) ?? throw new ArgumentException($"Could not obtain Parent folder of {projectFile}");

        ProjectStruct projectStruct;
        using (var projectStream = await projectFile.OpenStreamForReadAsync()) {
            projectStruct = await JsonSerializer.DeserializeAsync<ProjectStruct>(projectStream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? throw new IOException($"Project {projectFile} was not readable");
        }

        Func<CoreViewModel, Task<ProjectTreeElementViewModel>> root = async (project) => {
            var root = new ProjectTreeElementViewModel(project, new ProjectItem<Folder>("", project));

            async Task InitFolder(CoreViewModel project, ProjectTreeElementViewModel vm, StorageFolder directory) {
                //vm.Content = new FolderModel() { Path = new FileInfo(directory.Path) };

                var files = await directory.GetFilesAsync();
                await files.Select(async file => {
                    var loader = loaders.Where(x => x.pattern.IsMatch(file.Name)).FirstOrDefault().loders;

                    if (loader is null) {
                        return null;
                    }

                    var projectPath = ProjectPath.From(file, project);
                    var model = await ProjectItem.Create(projectPath, project);

                    return model;
                }).NotNull().ForEachAsync(content => {
                    var subVM = new ProjectTreeElementViewModel(project, content);
                    subVM.SetParent(vm);
                });

                var folders = await directory.GetFoldersAsync();
                await folders.Select(async x => await ProjectItem<Folder>.Create(ProjectPath.From(x, project), project))
                    .AsAsyncEnumerable()
                     .NotNull()
                      .ForEachAsync(async content => {
                          var subVM = new ProjectTreeElementViewModel(project, content);
                          subVM.SetParent(vm);
                          await InitFolder(project, subVM, await subVM.Content.Path.ToStorageFolder(project));
                      });
            }

            await InitFolder(project, root, rootFolder);
            return root;
        };
        TaskCompletionSource waitForLoadReady = new();
        CoreViewModel projectViewModel = new(root, projectStruct, rootFolder, waitForLoadReady);
        await waitForLoadReady.Task;
        return projectViewModel;
    }

    public ReadOnlyObservableCollection<ProjectPath> GetProjectItemCollectionOfType<T>()
        where T : class, IProjectItemContent<T> {
        return new(this.pathByType[T.Type]);
    }

    internal ProjectItem<T>? GetProjectItem<T>(ProjectPath path) where T : class, IProjectItemContent<T> {
        return (ProjectItem<T>?)GetProjectItem(path);
    }
    internal ProjectItem? GetProjectItem(ProjectPath path) {
        var current = this.Root;
        foreach (var part in path.Value.AsSpan().Spliterator("/")) {

            ProjectTreeElementViewModel? child = null;
            foreach (var c in current.Children) {
                if (c.Name.AsSpan().Equals(part, StringComparison.OrdinalIgnoreCase)) {
                    child = c;
                    break;
                }
            }
            if (child is null) {
                return null;
            }
            current = child;
        }
        return current?.Content;
    }

    internal void RegisterTreeElement(ProjectTreeElementViewModel projectTreeElementViewModel) {
        if (projectTreeElementViewModel.Content is ProjectItem item) {
            this.pathByType[item.Type].Add(item.Path);
        }
    }
    internal void UnRegisterTreeElement(ProjectTreeElementViewModel projectTreeElementViewModel) {
        if (projectTreeElementViewModel.Content is ProjectItem item) {
            this.pathByType[item.Type].Remove(item.Path);
        }
    }
}
