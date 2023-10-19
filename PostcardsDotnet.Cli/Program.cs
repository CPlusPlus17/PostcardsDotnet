using Microsoft.Extensions.Logging;
using PluginBase;
using PostcardDotnet.Services;
using PostcardsDotnet.API;
using PostcardsDotnet.Cli;
using Serilog;

// Logging stuff
var serilog =  new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Method} - {Message:l}{NewLine}{Exception}")
    .CreateLogger();

var loggerFactory = new LoggerFactory().AddSerilog(serilog);

var logger = loggerFactory.CreateLogger<Program>();

var timerSync = new PeriodicTimer(TimeSpan.FromMinutes(int.Parse(Environment.GetEnvironmentVariable("PCDNCLI_PLUGINSYNCTIME") ?? "600")));
var timerSend = new PeriodicTimer(TimeSpan.FromHours(24));

try
{
    string[] pluginPaths = { Environment.GetEnvironmentVariable("PCDNCLI_PLUGINPATH") ?? string.Empty, };

    var command = pluginPaths.SelectMany(pluginPath =>
    {
        var pluginAssembly = Helper.LoadPlugin(pluginPath);
        return Helper.CreateCommands(pluginAssembly);
    }).ToList().First(); // Currently only one supported

    logger.LogInformation("Found plugin: {CommandName}\t {CommandDescription}", command.Name, command.Description);

    // Login
    await command.Login();

    // Do sync
    await command.Sync();

    // Login swiss post
    var postcardsTokenService = new SwissIdLoginService();
    var postcardsApi = new SwissPostcardCreatorApi(postcardsTokenService);

    // Todo: Login

    // Try to send
    var nextPhoto = await command.GetNextPhoto();
    if (await postcardsApi.SendPostcard(File.ReadAllBytes(nextPhoto))) await command.DeleteCachedPhoto(nextPhoto);

    // Registry sync task
    var taskSync = HandleTimerSync(timerSync, command);

    // Register send task
    var taskSend = HandleTimerSend(timerSend, postcardsApi, command);

    // Todo: Add Token refresh swiss post

    // Wait forever
    Task.WaitAll(taskSync, taskSend);

}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

async Task HandleTimerSync(PeriodicTimer timer, ICommand command)
{
    while(await timer.WaitForNextTickAsync())
    {
       await Task.Run(command.Sync);
    }
}

async Task HandleTimerSend(PeriodicTimer timer, SwissPostcardCreatorApi api, ICommand command)
{
    while(await timer.WaitForNextTickAsync())
    {
        await Task.Run( async () =>
        {
            var nextPhoto = await command.GetNextPhoto();
            if (await api.SendPostcard(File.ReadAllBytes(nextPhoto))) await command.DeleteCachedPhoto(nextPhoto);
        });
    }
}
