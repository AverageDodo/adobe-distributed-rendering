using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Util;

namespace Testing;

public class ClientTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private const string c_FileDirPath = "../../../files/";

    [SetUp]
    public void Setup() { }

    [Test]
    public void TestMediaEncoderProcessStart()
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = @"C:\Program Files\Adobe\Adobe Media Encoder 2025\Adobe Media Encoder.exe"
        };

        startInfo.ArgumentList.Clear();
        startInfo.ArgumentList.Add("--console");
        startInfo.ArgumentList.Add("es.executeScript");
        startInfo.ArgumentList.Add(
            @"C:\Users\Dominik Lambrich\git\hsfl\thesis\Client\files\4bf36c75-838d-43c2-9deb-a3ad7bf90517.js"
        );

        using var process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;

        process.OutputDataReceived += (sender, args) => { Console.WriteLine($"Output received: '{args.Data}'."); };
        process.ErrorDataReceived += (sender, args) => { Console.WriteLine($"Error received: '{args.Data}'."); };
        process.Exited += (sender, args) =>
        {
            Console.WriteLine("Process exiting.");

            Console.WriteLine(JsonSerializer.Serialize(args, SerializerOptions));
        };

        if (!process.Start())
            Console.WriteLine("No new process was created.");

        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
            process.Kill(true);
    }

    [Test]
    public async Task TestJsScriptGeneration()
    {
        const string c_placeholderText = "/*{DATA}*/";

        RenderFragment data = new()
        {
            Guid = Guid.NewGuid(),
            RequestGuid = Guid.Empty,
            ProjectPath = "some path",
            PresetPath = "some path",
            DestinationPath = "some path",
            Index = 0,
            DurationInMilliseconds = 2000,
            StartTimeInMilliseconds = 1000
        };

        var fileDir = new DirectoryInfo(c_FileDirPath);
        if (!fileDir.Exists)
            Assert.Fail();

        var scriptFileTemplate = new FileInfo(Path.Combine(fileDir.FullName, "script.js"));
        if (!scriptFileTemplate.Exists)
            Assert.Fail();

        using StreamReader reader = scriptFileTemplate.OpenText();
        var scriptContent = await reader.ReadToEndAsync();
        reader.Close();

        var scriptWithContent = scriptContent.Replace(
            c_placeholderText,
            JsonSerializer.Serialize(data, JsonConfig.SerializerOptions)
        );

        await using StreamWriter newScript = File.CreateText(Path.Combine(c_FileDirPath, "generated-script.js"));
        await newScript.WriteAsync(scriptWithContent);
        await newScript.FlushAsync();
        newScript.Close();
    }

    [Test]
    public void TestProcessKillCallback()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName =
                    "C:\\Program Files\\WindowsApps\\Microsoft.WindowsNotepad_11.2410.21.0_x64__8wekyb3d8bbwe\\Notepad\\Notepad.exe"
            },
            EnableRaisingEvents = true
        };

        process.Exited += (sender, args) => { Console.WriteLine("Process exited."); };

        process.Start();
        Thread.Sleep(1000);
        process.Kill(true);

        var bag = new ConcurrentBag<RenderFragment>();
    }

    [Test]
    public void TestProcessOutput()
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = @"C:\Program Files\Adobe\Adobe Media Encoder 2025\Adobe Media Encoder.exe"
        };

        startInfo.ArgumentList.Clear();
        startInfo.ArgumentList.Add("--console");
        startInfo.ArgumentList.Add("es.executeScript");
        startInfo.ArgumentList.Add(
            @"C:\Users\Dominik Lambrich\git\hsfl\thesis\Client\files\output-test.js"
        );

        using var cts = new CancellationTokenSource();
        using var process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;

        process.OutputDataReceived += (sender, args) => { Console.WriteLine($"Output received: '{args.Data}'."); };
        process.ErrorDataReceived += (sender, args) => { Console.WriteLine($"Error received: '{args.Data}'."); };
        process.Exited += (sender, args) =>
        {
            cts.Cancel();
            Console.WriteLine("Process exiting.");

            Console.WriteLine(JsonSerializer.Serialize(args, SerializerOptions));
        };

        if (!process.Start())
            Console.WriteLine("No new process was created.");

        process.BeginOutputReadLine();

        Task.Run(
            async () =>
            {
                var outputString = await process.StandardOutput.ReadToEndAsync(cts.Token);
                Console.WriteLine(outputString);
            },
            cts.Token
        );

        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
            process.Kill(true);
    }

    [Test]
    public void TestMediaEncoderInstallSelector()
    {
        var adobeDirectory = new DirectoryInfo(@"C:\Program Files\Adobe\");

        if (!adobeDirectory.Exists) return;

        List<FileInfo> mediaEncoderInstalls =
            adobeDirectory
                .EnumerateDirectories()
                .Where(dirInfo => dirInfo.Name.Contains("Adobe Media Encoder"))
                .SelectMany(dirInfo => dirInfo.GetFiles().Where(file => file.Name.Equals("Adobe Media Encoder.exe")))
                .OrderBy(file => file.Directory?.Name)
                .ToList();

        var mediaEncoderPath = mediaEncoderInstalls.LastOrDefault()?.FullName;
        var oldestMediaEncoderPath = mediaEncoderInstalls.FirstOrDefault()?.FullName;

        Console.WriteLine($"Oldest install: {oldestMediaEncoderPath}");
        Console.WriteLine($"Most recent install: {mediaEncoderPath}");
    }
}