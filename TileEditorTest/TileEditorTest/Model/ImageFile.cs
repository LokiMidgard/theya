// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Storage;
using Windows.Storage.Streams;

namespace TileEditorTest.Model;

public partial class ImageFile : ProjectItemContent, IProjectItemContent<ImageFile> {
    private ImageFile(uint width, uint height, StorageFile file) {
        Width = width;
        Height = height;
        this.file = file;
    }
    public static ProjectItemType Type => ProjectItemType.Image;

    public uint Width { get; }
    public uint Height { get; }

    private readonly StorageFile file;

    public async Task<IRandomAccessStream> LoadAsync() {
        return await file.OpenReadAsync();
    }

    public static ImmutableArray<Regex> SupportedFilePatterns { get; } = ImmutableArray.Create(PngExtension());
    public static async Task<ImageFile> Load(ProjectPath path, ProjectViewModel project) {
        var file = await path.ToStorageFile(project);

        var props = await file.Properties.GetImagePropertiesAsync();
        return new ImageFile(props.Width, props.Height, file);
    }

    public Task Save(ProjectPath path, ProjectViewModel project) {
        return Task.CompletedTask;
    }


    [GeneratedRegex(".png$")]
    private static partial Regex PngExtension();
}
