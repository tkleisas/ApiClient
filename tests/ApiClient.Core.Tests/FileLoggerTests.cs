using System;
using System.IO;
using ApiClient.Core.Diagnostics;
using Xunit;

namespace ApiClient.Core.Tests;

public class FileLoggerTests
{
    private static string TempFile() => Path.Combine(
        Path.GetTempPath(), "ApiClientLogTests", Guid.NewGuid().ToString("N"), "log.txt");

    [Fact]
    public void Creates_the_directory_and_writes_an_info_line()
    {
        var path = TempFile();
        try
        {
            new FileLogger(path).Info("hello");

            var text = File.ReadAllText(path);
            Assert.Contains("[INFO]", text);
            Assert.Contains("hello", text);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void Error_includes_the_exception_details_and_appends()
    {
        var path = TempFile();
        try
        {
            var logger = new FileLogger(path);
            logger.Info("first");
            logger.Error("boom", new InvalidOperationException("bad state"));

            var text = File.ReadAllText(path);
            Assert.Contains("first", text);
            Assert.Contains("[ERROR] boom", text);
            Assert.Contains("InvalidOperationException", text);
            Assert.Contains("bad state", text);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
