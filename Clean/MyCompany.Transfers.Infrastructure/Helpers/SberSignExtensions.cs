using System.Diagnostics;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace MyCompany.Transfers.Infrastructure.Helpers;

public static class SberSignExtensions
{
    public static (string CanonicalXml, string Signature) GenerateSberSign(
        this string textSend,
        string userId,
        string keyPath,
        string keyPass,
        string javaExecutablePath,
        string jarDirectory,
        ILogger? logger = null)
    {
        try
        {
            var xml = CanonicalizeXml(textSend).Replace("&#xD; ", "").Replace("&#xD;", "");
            if (string.IsNullOrEmpty(xml))
            {
                logger?.LogDebug("Empty data for Sber sign");
                return ("", "");
            }

            if (!File.Exists(javaExecutablePath) || !Directory.Exists(jarDirectory))
            {
                logger?.LogDebug("Java executable or jar dir not found");
                return ("", "");
            }

            var pathToJar = Path.Combine(jarDirectory, "sign-sandbox-1.0-SNAPSHOT.jar");
            if (!File.Exists(pathToJar))
            {
                logger?.LogDebug("Jar file not found: {Path}", pathToJar);
                return ("", "");
            }

            var runFileName = $"sign-sandbox-1.0-SNAPSHOT{Guid.NewGuid()}.jar";
            var runFile = Path.Combine(jarDirectory, runFileName);
            File.Copy(pathToJar, runFile);
            try
            {
                var jarDirFull = Path.GetFullPath(jarDirectory).Replace("\\", "/");
                var xmlArgument = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
                var psi = new ProcessStartInfo(javaExecutablePath)
                {
                    Arguments = $"-Dfile.encoding=UTF-8 -cp \"{Path.Combine(jarDirFull, runFileName)}\" tj.ibt.Main {xmlArgument} {keyPath} {keyPass}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = jarDirFull
                };

                using var process = Process.Start(psi);
                if (process is null)
                    return ("", "");
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(30000);

                var lines = output.Split("\r\n");
                if (lines.Length > 0 && lines[0].StartsWith("Error"))
                {
                    logger?.LogDebug("Sber sign error: {Msg}", lines[0]);
                    return (lines[0], "");
                }
                if (lines.Length < 2)
                {
                    logger?.LogDebug("Sber sign: insufficient output");
                    return ("", "");
                }
                return (lines[0], lines[1]);
            }
            finally
            {
                if (File.Exists(runFile))
                    File.Delete(runFile);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Sber sign generation error");
            return ("Error:", "");
        }
    }

    private static string CanonicalizeXml(string xmlContent)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xmlContent);
        var c14n = new XmlDsigC14NTransform();
        c14n.LoadInput(doc);
        var stream = (Stream)c14n.GetOutput(typeof(Stream))!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
