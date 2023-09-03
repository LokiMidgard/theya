// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TileEditorTest.ViewModel;

using Windows.Gaming.Input;
using Windows.Graphics;
using Windows.Storage;

namespace TileEditorTest.Model;

public abstract class JsonProjectItem<T> : ProjectItemContent
    where T : JsonProjectItem<T>, IProjectItemContent<T> {

    public async Task Save(ProjectPath path, ProjectViewModel project) {
        var file = await path.ToStorageFile(project);
        var projectPath = ProjectPath.From(file, project);
        var converter = new ProjectItemConverter(project, projectPath);
        using var stream = await file.OpenStreamForWriteAsync();
        JsonSerializerOptions options = GenerateSterilizerOptions(converter);
        await JsonSerializer.SerializeAsync<T>(stream, (T)this, options);
    }


    protected static async Task<T> Load(ProjectPath path, ProjectViewModel project) {
        var file = await path.ToStorageFile(project);
        var projectPath = ProjectPath.From(file, project);
        var converter = new ProjectItemConverter(project, projectPath);
        using var stream = await file.OpenStreamForReadAsync();
        JsonSerializerOptions options = GenerateSterilizerOptions(converter);
        return await JsonSerializer.DeserializeAsync<T>(stream, options) ?? throw new IOException($"Failed to load file {file.Path} to {typeof(T)}");
    }

    private static JsonSerializerOptions GenerateSterilizerOptions(ProjectItemConverter converter) {
        JsonSerializerOptions subOptions = new() { Converters = { converter } };
        JsonSerializerOptions options = new() {
            Converters = { converter },
            TypeInfoResolver = JsonTypeInfoResolver.Combine(new ProjectItemJsonContext(subOptions),
                                                            new DefaultJsonTypeInfoResolver())
        };
        return options;
    }
}
#pragma warning disable SYSLIB1031 // Duplicate type name.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ProjectFile))]
[JsonSerializable(typeof(MapFile))]
[JsonSerializable(typeof(TileSetFile))]
internal partial class ProjectItemJsonContext : JsonSerializerContext { }
#pragma warning restore SYSLIB1031 // Duplicate type name.
