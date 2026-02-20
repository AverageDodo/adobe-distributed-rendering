using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Testing;

public class ServerTests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void TestFileSystemWatcher()
    {
        const string c_fileDirPath = "../../../files/";

        var watcher = new FileSystemWatcher(c_fileDirPath)
        {
            EnableRaisingEvents = true,
            Filter = "*.txt",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        watcher.Created += (sender, args) =>
        {
            if (sender is FileSystemWatcher w)
            {
                Console.WriteLine($"File created at {args.FullPath} in watched directory {w.Path}.");
            }
            else
            {
                Assert.Fail();
            }
        };

        var filename = $"{RandomNumberGenerator.GetHexString(4)}.txt";
        var path = Path.Combine(c_fileDirPath, filename);

        using var file = File.CreateText(path);
        file.Close();

        Thread.Sleep(TimeSpan.FromSeconds(5));

        File.Delete(path);
    }

    [Test]
    public void TestFileFiltering()
    {
        const string c_projectPath = @"\\HomeNAS\NAS\AME Render Hub Storage\Projects\Copied_D2_2025";

        var dirInfo = new DirectoryInfo(c_projectPath);

        FileInfo[] topDirFiles = dirInfo.GetFiles();

        Console.WriteLine("Top directory files:");
        foreach (FileInfo topDirFile in topDirFiles)
            Console.WriteLine($"  - {topDirFile.Name}");

        FileInfo? presetFile = dirInfo.GetFiles()
            .FirstOrDefault(file => file.Extension.Equals(".epr", StringComparison.InvariantCultureIgnoreCase));

        Console.WriteLine(
            presetFile?.Name ?? "No matching file found"
        );
    }

    [Test]
    public void TestFileMoving()
    {
        const string c_testFileDir = @"C:\Users\Dominik Lambrich\git\hsfl\thesis\Client\files";

        using var watcher = new FileSystemWatcher
        {
            Path = c_testFileDir,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName,
            Filter = "*"
        };

        watcher.Created += (sender, args) =>
        {
            Console.WriteLine($"File created callback triggered: {args.ChangeType}: {args.FullPath}");
        };

        watcher.Renamed += (sender, args) =>
        {
            Console.WriteLine($"File renamed callback triggered: {args.OldFullPath} -> {args.FullPath}");
        };

        var file1 = new FileInfo(Path.Combine(c_testFileDir, $"{RandomNumberGenerator.GetHexString(5)}.tmp"));
        Console.WriteLine($"First file: {file1.FullName}");

        var file2 = new FileInfo(Path.Combine(c_testFileDir, $"{RandomNumberGenerator.GetHexString(5)}.tmp"));
        Console.WriteLine($"Second file: {file2.FullName}");

        FileStream fs1 = File.Create(file1.FullName);
        fs1.Dispose();
        fs1.Close();

        File.Move(file1.FullName, file2.FullName);

        Thread.Sleep(2000);
    }
}