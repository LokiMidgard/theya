// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using TileEditorTest.ViewModel;

namespace TileEditorTest.Model;

internal class ProjectItemConverter : JsonConverter<ProjectItem> {
    private ProjectViewModel? project;
    private ProjectPath? projectPath; // this is for relative files, not currently implemented

    public ProjectItemConverter(ProjectViewModel project, ProjectPath projectPath) {
        this.project = project;
        this.projectPath = projectPath;
    }

    public ProjectItemConverter() {

    }

    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert.IsAssignableTo(typeof(ProjectItem));
    }

    public override ProjectItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (project is null) {
            var actualConverter = options.Converters.OfType<ProjectItemConverter>().Where(x => x.project is not null).FirstOrDefault();
            if (actualConverter is null) {
                throw new InvalidOperationException($"The {nameof(ProjectItemConverter)} needs an project, you need to provide one that was instantiated with the project in the {nameof(JsonSerializerOptions)}.{nameof(JsonSerializerOptions.Converters)} ");
            }
            return actualConverter.Read(ref reader, typeToConvert, options);
        }
        var path = reader.GetString();
        if (!path?.StartsWith('/') ?? false) {
            throw new FormatException($"Failed to desterilize {path} to {nameof(ProjectItem)}. Non (project) absolute paths ar currently not supported.");
        }

        return path == null ? null : project.GetProjectItem(path[1..]);
    }

    public override void Write(Utf8JsonWriter writer, ProjectItem value, JsonSerializerOptions options) {
        writer.WriteStringValue("/" + value.Path);

    }
}
