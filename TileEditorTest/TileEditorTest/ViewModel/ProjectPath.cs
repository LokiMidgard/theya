using System;
using System.IO;
using System.Threading.Tasks;

using Windows.Storage;

namespace TileEditorTest.ViewModel;

public readonly struct ProjectPath : IEquatable<ProjectPath> {
    public readonly string Value;
    public ProjectPath(string path) => Value = path ?? throw new ArgumentNullException(nameof(path));
    public override string ToString() => Value;
    public static ProjectPath Parse(string path) => new(path);

    public string Name => Path.GetFileName(Value);

    public static implicit operator ProjectPath(string path) => new(path);
    public static implicit operator string(ProjectPath path) => path.Value;

    public static bool operator ==(ProjectPath left, ProjectPath right) {
        return left.Equals(right);
    }

    public static bool operator !=(ProjectPath left, ProjectPath right) {
        return !(left == right);
    }

    public FileInfo ToFileinfo(CoreViewModel project) {
        return new FileInfo(Path.Combine(project.RootFolder.Path, this.Value));
    }
    public async Task<StorageFile> ToStorageFile(CoreViewModel project) {
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
    public async Task<StorageFolder> ToStorageFolder(CoreViewModel project) {
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
    public async Task<IStorageItem2> ToStorageItem(CoreViewModel project) {
        var path = Path.Combine(project.RootFolder.Path, this.Value);
        if (Directory.Exists(path)) {
            return await ToStorageFolder(project);
        } else if (File.Exists(path)) {
            return await ToStorageFile(project);
        } else {
            throw new FileNotFoundException("Could not find the file", path);
        }
    }

    public static ProjectPath From(IStorageItem file, CoreViewModel project) {
        var filePath = file.Path;
        var rootPath = project.RootFolder.Path;
        return Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
    }

    internal ProjectPath AddSegment(string result) {
        return Path.Combine(this.Value, result);
    }

    internal string SystemPath(CoreViewModel project) {
        return Path.Combine(project.RootFolder.Path, this.Value);
    }

    public override bool Equals(object? obj) {
        return obj is ProjectPath path && Equals(path);
    }

    public bool Equals(ProjectPath other) {
        return Value == other.Value;
    }

    public override int GetHashCode() {
        return HashCode.Combine(Value);
    }

   
}
