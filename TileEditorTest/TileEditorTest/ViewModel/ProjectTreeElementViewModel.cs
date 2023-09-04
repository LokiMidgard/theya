using EnumFastToStringGenerated;

using Microsoft.UI.Xaml.Input;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using TileEditorTest.Model;
using TileEditorTest.View;

namespace TileEditorTest.ViewModel;

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


    public CoreViewModel Project { get; }



    // Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...

    public ProjectTreeElementViewModel(CoreViewModel project, ProjectItem content) {
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
