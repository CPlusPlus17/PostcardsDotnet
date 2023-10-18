using PluginBase;
using PostcardsDotnet.Cli;

var timerSync = new PeriodicTimer(TimeSpan.FromSeconds(5));
Task taskSync;
Task taskSend = null;

try
{
    string[] pluginPaths = { Environment.GetEnvironmentVariable("PCDNCLI_PLUGINPATH") ?? string.Empty, };

    var command = pluginPaths.SelectMany(pluginPath =>
    {
        var pluginAssembly = Helper.LoadPlugin(pluginPath);
        return Helper.CreateCommands(pluginAssembly);
    }).ToList().First(); // Currently only one supported
    
    Console.WriteLine($"{command.Name}\t - {command.Description}");

    // Login
    await command.Login();
    
    // Do sync
    await command.Sync();

    // Registry sync task
    taskSync = HandleTimerSync(timerSync, command);
    
    // Wait forever
    Task.WaitAll(taskSync, taskSend);

}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

async Task HandleTimerSync(PeriodicTimer timer,ICommand command)
{
    while(await timer.WaitForNextTickAsync())
    {
       await Task.Run(command.Sync);
    }
}