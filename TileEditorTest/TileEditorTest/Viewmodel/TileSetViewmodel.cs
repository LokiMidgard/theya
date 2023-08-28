using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using TileEditorTest.Model;
using TileEditorTest.View;

using Windows.System.Threading;

namespace TileEditorTest.ViewModel;


internal interface IViewModel : INotifyPropertyChanged {
    public ProjectViewModel Project { get; }
    public ICommand Save { get; }
    public bool HasChanges { get; }

}
internal interface IViewModel<OfFile, ViewModel> : IViewModel
    where OfFile : class, IProjectItemContent<OfFile>
    where ViewModel : IViewModel<OfFile, ViewModel> {
    public static abstract Task<ViewModel> Create(ProjectItem<OfFile> file, ProjectViewModel project);
}
internal interface IView {
    public IViewModel ViewModel { get; }
}
internal interface IView<OfFile, VM, View> : IView
where OfFile : class, IProjectItemContent<OfFile>
where VM : IViewModel<OfFile, VM>
where View : Control, IView<OfFile, VM, View> {

    public new VM ViewModel { get; }
    IViewModel IView.ViewModel => this.ViewModel;
    public static abstract View Create(VM viewModel);

}

internal partial class TileSetViewModel : DependencyObject, IViewModel<TileSetFile, TileSetViewModel> {


    public ICommand Save { get; }

    [Notify]
    private bool hasChanges;
    private void OnHasChangesChanged() {
        ((StandardUICommand)Save).NotifyCanExecuteChanged();
    }

    [Notify]
    private int tileHeight;

    [Notify]
    private int tileWidth;

    [Notify]
    private ProjectItem<ImageFile>? selectedImage;

    private void OnSelectedImageChanged() {
        if (SelectedImage is null) {
            this.ImageSource = null;
        } else {
            BitmapImage image = new() {
                UriSource = new Uri(SelectedImage.Path.SystemPath(Project)),
            };
            this.ImageSource = image;
        }
    }


    [Notify]
    private ImageSource? imageSource;


    private void OnAnyPropertyChanged(string propertyName) {
        if (propertyName is not nameof(HasChanges) or nameof(ImageSource)) {
            this.UpdateHasChanges();
        }
    }



    private readonly TileSetFile tileSet;
    private readonly ProjectItem<TileSetFile> projectItem;

    public TileSetViewModel(TileSetFile tileSet, ProjectItem<TileSetFile> projectItem, ProjectViewModel project, ObservableCollection<ProjectItem<ImageFile>> allImages) {
        this.tileSet = tileSet;
        this.projectItem = projectItem;
        Project = project;
        this.AllImages = new ReadOnlyObservableCollection<ProjectItem<ImageFile>>(allImages);


        StandardUICommand save = new(StandardUICommandKind.Save);
        save.ExecuteRequested += (sender, e) => SaveValuesToModel();
        save.CanExecuteRequested += (sender, e) => e.CanExecute = HasChanges;
        this.Save = save;
        RefreshFromModel();
    }

    private void RefreshFromModel() {
        this.SelectedImage = tileSet.Image;
        this.TileWidth = tileSet.TileSize?.Width ?? 32;
        this.TileHeight = tileSet.TileSize?.Height ?? 32;
        UpdateHasChanges();
    }

    private Task SaveValuesToModel() {
        tileSet.Image = SelectedImage;
        tileSet.TileSize = new() { Width = TileWidth, Height = TileHeight };
        UpdateHasChanges();
        return tileSet.Save(projectItem.Path, Project);
    }

    private void UpdateHasChanges() {
        this.HasChanges = this.SelectedImage != tileSet.Image ||
        this.TileWidth != (tileSet.TileSize?.Width ?? 32) ||
        this.TileHeight != (tileSet.TileSize?.Height ?? 32);
    }

    public ProjectViewModel Project { get; }

    public ReadOnlyObservableCollection<ProjectItem<ImageFile>> AllImages { get; }



    public static async Task<TileSetViewModel> Create(ProjectItem<TileSetFile> projectItem, ProjectViewModel project) {
        var tileSet = await projectItem.Content;
        var files = project.GetProjectItemCollectionOfType<ImageFile>();
        ObservableCollection<ProjectItem<ImageFile>> allImages = new();
        foreach (var file in files) {
            var imageItems = project.GetProjectItem<ImageFile>(file);
            if (imageItems is not null) {
                allImages.Add(imageItems);
            }
        }

        return new TileSetViewModel(tileSet, projectItem, project, allImages);
    }


}
