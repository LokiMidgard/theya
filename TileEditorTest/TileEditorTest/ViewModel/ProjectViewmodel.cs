using ABI.Windows.AI.MachineLearning;

using EnumFastToStringGenerated;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.Helper;
using TileEditorTest.Helper.MemoryExtension;
using TileEditorTest.Model;
using TileEditorTest.View;
using TileEditorTest.View.Dialogs;

using Windows.ApplicationModel.Chat;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.WebUI;

namespace TileEditorTest.ViewModel;

internal record ProjectStruct {

    public string? Name { get; set; }
    public Version? Version { get; set; }
}

public readonly struct ProjectPath {
    public readonly string Value;
    public ProjectPath(string path) => Value = path ?? throw new ArgumentNullException(nameof(path));
    public override string ToString() => Value;
    public static ProjectPath Parse(string path) => new(path);

    public string Name => Path.GetFileName(Value);

    public static implicit operator ProjectPath(string path) => new(path);
    public static implicit operator string(ProjectPath path) => path.Value;

    public FileInfo ToFileinfo(ProjectViewModel project) {
        return new FileInfo(Path.Combine(project.RootFolder.Path, this.Value));
    }
    public async Task<StorageFile> ToStorageFile(ProjectViewModel project) {
        string path = Path.Combine(project.RootFolder.Path, this.Value);

        var parts = this.Value.Split('/');

        var currentFolder = project.RootFolder;
        for (int i = 0; i < parts.Length; i++) {
            var part = parts[i];
            if (i != parts.Length - 1) {

                currentFolder = await currentFolder.CreateFolderAsync(part, CreationCollisionOption.OpenIfExists);
            } else {
                return await currentFolder.CreateFileAsync(part, CreationCollisionOption.OpenIfExists);
            }

        }
        throw new InvalidOperationException();


        //var file = await StorageFile.GetFileFromPathAsync(path).AsTask();
        //return file;
    }
    public async Task<StorageFolder> ToStorageFolder(ProjectViewModel project) {
        string path = Path.Combine(project.RootFolder.Path, this.Value);

        var parts = this.Value.Split('/');

        var currentFolder = project.RootFolder;
        for (int i = 0; i < parts.Length; i++) {
            var part = parts[i];
            if (i != parts.Length - 1) {

                currentFolder = await currentFolder.CreateFolderAsync(part, CreationCollisionOption.OpenIfExists);
            } else {
                return await currentFolder.CreateFolderAsync(part, CreationCollisionOption.OpenIfExists);
            }

        }
        throw new InvalidOperationException();

    }
    public async Task<IStorageItem2> ToStorageItem(ProjectViewModel project) {
        var path = Path.Combine(project.RootFolder.Path, this.Value);
        if (Directory.Exists(path)) {
            return await ToStorageFolder(project);
        } else if (File.Exists(path)) {
            return await ToStorageFile(project);
        } else {
            throw new FileNotFoundException("Could not find the file", path);
        }
    }

    public static ProjectPath From(IStorageItem file, ProjectViewModel project) {
        var filePath = file.Path;
        var rootPath = project.RootFolder.Path;
        return Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
    }

    internal ProjectPath AddSegment(string result) {
        return Path.Combine(this.Value, result);
    }

    internal string SystemPath(ProjectViewModel project) {
        return Path.Combine(project.RootFolder.Path, this.Value);
    }
}

public partial class ProjectViewModel {
    public string Name { get; }
    public StorageFolder RootFolder { get; }

    public ProjectTreeElementViewModel Root { get; private set; }
    public IEnumerable<ProjectTreeElementViewModel> RootItemsSource => Enumerable.Repeat(Root, 1);

    private ImmutableDictionary<ProjectItemType, MultiThreadObservableCollection<ProjectPath>> pathByType;

    private static readonly List<(Regex pattern, Func<ProjectPath, ProjectViewModel, Task<IProjectItemContent>> loders)> loaders = new();

    public event Func<ProjectItemType, Predicate<string>?, Task<string?>> OnShowNewFileDialog;
    public event Func<ProjectItem, Task> OnOpenFile;


    internal Task OpenFile(ProjectItem type) {
        return OnOpenFile?.Invoke(type) ?? Task.CompletedTask;
    }
    internal Task<string?> ShowNewFileDialog(ProjectItemType type, Predicate<string>? isAllowed = null) {
        return OnShowNewFileDialog?.Invoke(type, isAllowed) ?? Task.FromResult<string?>(null);
    }


    static ProjectViewModel() {
        InitProjectItemContentLoader();
    }
    private ProjectViewModel(Func<ProjectViewModel, Task<ProjectTreeElementViewModel>> root, ProjectStruct projectData, StorageFolder rootFolder, TaskCompletionSource waitForLoadReady) {
        this.pathByType = ProjectItemTypeEnumExtensions.GetValuesFast().ToImmutableDictionary(x => x, x => new MultiThreadObservableCollection<ProjectPath>());
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

    public static async Task<ProjectViewModel> Load(StorageFile projectFile) {

        var rootFolder = (await projectFile.GetParentAsync()) ?? throw new ArgumentException($"Could not obtain Parent folder of {projectFile}");

        ProjectStruct projectStruct;
        using (var projectStream = await projectFile.OpenStreamForReadAsync()) {
            projectStruct = await JsonSerializer.DeserializeAsync<ProjectStruct>(projectStream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? throw new IOException($"Project {projectFile} was not readable");
        }

        Func<ProjectViewModel, Task<ProjectTreeElementViewModel>> root = async (project) => {
            var root = new ProjectTreeElementViewModel(project, new ProjectItem<Folder>("", project));

            async Task InitFolder(ProjectViewModel project, ProjectTreeElementViewModel vm, StorageFolder directory) {
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
        ProjectViewModel projectViewModel = new(root, projectStruct, rootFolder, waitForLoadReady);
        await waitForLoadReady.Task;
        return projectViewModel;
    }

    public MultiThreadObservableCollection<ProjectPath> GetProjectItemCollectionOfType<T>()
        where T : class, IProjectItemContent<T> {
        return this.pathByType[T.Type];
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

public partial class ProjectTreeElementViewModel {

    [Notify(global::PropertyChanged.SourceGenerator.Setter.Private)]
    private ProjectTreeElementViewModel? parent;

    private readonly MultiThreadObservableCollection<ProjectTreeElementViewModel> children = new();
    public ReadOnlyObservableCollection<ProjectTreeElementViewModel> Children { get; }

    public ProjectItemType Type { get; }

    public bool HasCommands => OpenCommand != null || CreateCommands.Length > 0;

    public string Name { get; }
    public XamlUICommand? OpenCommand { get; }
    public ImmutableArray<XamlUICommand> CreateCommands { get; }

    public ProjectItem Content { get; }


    public ProjectViewModel Project { get; }



    // Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...

    public ProjectTreeElementViewModel(ProjectViewModel project, ProjectItem content) {
        Children = new(children);
        Project = project;
        Content = content;
        this.Type = content.Type;
        this.Name = content.Path.Name;
        this.CreateCommands = content.Type == ProjectItemType.Folder
            ? CreateCommand().ToImmutableArray()
            : ImmutableArray<XamlUICommand>.Empty;
        if (ViewLoader.IsSupported(content)) {

            StandardUICommand openCommand = new(StandardUICommandKind.Open);
            openCommand.ExecuteRequested += async (sender, e) => {
                await Project.OpenFile(this.Content);
            };
            this.OpenCommand = openCommand;
        }
    }

    [AutoInvoke.FindAndInvoke]
    private XamlUICommand CreateCommand<T>() where T : class, IProjectItemContentCreatable<T> {

        var command = new XamlUICommand() {
            IconSource = ProjectItemToIconSourceConverter.Convert(T.Type),
            Label = T.Type.ToDisplayFast(),
            Command = new DelegateCommand(async () => {
                var fileName = await Project.ShowNewFileDialog(T.Type, (file) => (!children.Any(x => string.Equals(x.Name, file + T.Extension, StringComparison.OrdinalIgnoreCase))) && !Path.GetInvalidFileNameChars().Any(file.Contains));
                if (fileName is not null) {
                    ProjectPath subPath = this.Content.Path.AddSegment(fileName + T.Extension);
                    await T.Create(subPath, Project);
                    var newItem = await ProjectItem<T>.Create(subPath, this.Project) ?? throw new ArgumentException("Unknown extension");
                    this.AddChild(new ProjectTreeElementViewModel(Project, newItem));
                }
            })
        };


        return command;

    }


    public void AddChild(ProjectTreeElementViewModel child) {
        child.SetParent(this);
    }
    public void SetParent(ProjectTreeElementViewModel? parent) {
        if (this.Parent == parent) {
            return;
        }
        bool wasAdded = true;
        if (this.Parent is not null) {
            this.Parent.children.Remove(this);
            wasAdded = false;
        }
        this.Parent = parent;
        parent?.children.Add(this);
        if (parent is null) {
            Project.UnRegisterTreeElement(this);
        } else if (wasAdded) {
            Project.RegisterTreeElement(this);

        }
    }

}
[EnumFastToStringGenerated.EnumGenerator]
public enum ProjectItemType {

    Other,
    Folder,
    Map,
    TileSet,
    Image,
    Audio,
}