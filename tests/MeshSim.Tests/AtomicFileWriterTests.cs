using System.Text;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public class AtomicFileWriterTests
{
    [Fact]
    public void WriteAllTextCreatesUtf8FileWithoutByteOrderMark()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("data.txt");

        AtomicFileWriter.WriteAllText(path, "привіт");

        Assert.Equal(Encoding.UTF8.GetBytes("привіт"), File.ReadAllBytes(path));
    }

    [Fact]
    public void WriteAllBytesReplacesExistingFileAndLeavesNoTemporaryFiles()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("data.bin");
        AtomicFileWriter.WriteAllBytes(path, [1, 2, 3]);

        AtomicFileWriter.WriteAllBytes(path, [9, 8]);

        Assert.Equal(new byte[] { 9, 8 }, File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(temp.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public void WriteAllBytesCleansUpTemporaryFileWhenMoveFails()
    {
        using var temp = new TempDirectory();
        string occupied = temp.FilePath("occupied");
        Directory.CreateDirectory(occupied);

        // Цільовий шлях зайнятий каталогом: запис у .tmp вдається, а переміщення ні.
        Assert.ThrowsAny<Exception>(() => AtomicFileWriter.WriteAllBytes(occupied, [1, 2, 3]));

        Assert.True(Directory.Exists(occupied));
        Assert.Empty(Directory.GetFiles(temp.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public void WriteAllBytesThrowsWhenTargetDirectoryDoesNotExist()
    {
        using var temp = new TempDirectory();
        string path = Path.Combine(temp.DirectoryPath, "missing", "data.bin");

        Assert.Throws<DirectoryNotFoundException>(() => AtomicFileWriter.WriteAllBytes(path, [1]));
    }

    [Fact]
    public void WriteMethodsValidateArguments()
    {
        Assert.ThrowsAny<ArgumentException>(() => AtomicFileWriter.WriteAllBytes("  ", [1]));
        Assert.Throws<ArgumentNullException>(() => AtomicFileWriter.WriteAllBytes("file.bin", null!));
        Assert.Throws<ArgumentNullException>(() => AtomicFileWriter.WriteAllText("file.txt", null!));
        Assert.ThrowsAny<ArgumentException>(() => AtomicFileWriter.WriteAllText(string.Empty, "x"));
    }
}