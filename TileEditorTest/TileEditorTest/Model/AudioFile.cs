// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;

using Microsoft.UI.Xaml.Shapes;

using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Management.Deployment.Preview;
using Windows.Storage;

using WinRT;

namespace TileEditorTest.Model;

internal partial class AudioFile : ProjectItemContent, IProjectItemContent<AudioFile> {
    private AudioFile(TimeSpan duration) {
        Duration = duration;
    }
    public static ProjectItemType Type => ProjectItemType.Audio;

    public static ImmutableArray<Regex> SupportedFilePatterns { get; } = ImmutableArray.Create(PngExtension());
    public TimeSpan Duration { get; }

    public static async Task<AudioFile> Load(ProjectPath path, CoreViewModel project) {
        var file = await path.ToStorageFile(project);
        var props = await file.Properties.GetMusicPropertiesAsync();
        return new AudioFile(props.Duration);
    }

    [GeneratedRegex(".mp3")]
    private static partial Regex PngExtension();


    public Task Save(ProjectPath path, CoreViewModel project) {
        return Task.CompletedTask;
    }
}
