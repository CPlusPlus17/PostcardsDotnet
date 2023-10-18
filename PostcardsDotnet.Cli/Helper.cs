using System.Reflection;
using PluginBase;

namespace PostcardsDotnet.Cli;

public static class Helper
{
    /// <summary>
    /// Load plugin
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public static Assembly LoadPlugin(string relativePath)
    {
        // Navigate up to the solution root
        var root = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(typeof(Program).Assembly.Location))))) ?? string.Empty));

        var pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        Console.WriteLine($"Loading commands from: {pluginLocation}");
        var loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
    }

    /// <summary>
    /// Create commands from assembly
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static IEnumerable<ICommand> CreateCommands(Assembly assembly)
    {
        var count = 0;

        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(ICommand).IsAssignableFrom(type)) continue;
            if (Activator.CreateInstance(type) is not ICommand result) continue;
        
            count++;
            yield return result;
        }

        if (count != 0) yield break;
        var availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
        throw new ApplicationException(
            $"Can't find any type which implements ICommand in {assembly} from {assembly.Location}.\n" +
            $"Available types: {availableTypes}");
    }
}