using System.Net;
using CasCap.Common.Extensions;
using CasCap.Models;
using CasCap.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PluginBase;
using Serilog;

namespace PostcardsDotnet.PluginGooglePhotos;

public class GooglePhotoCommand : ICommand
{
    public string Name => "GooglePhotos Plugin";
    public string Description => "Syncs all GooglePhotos in a specific album.";

    /// <summary>
    /// User for google photo api
    /// </summary>
    private readonly string _envUser = Environment.GetEnvironmentVariable("GPSC_USER") ?? string.Empty;
    
    /// <summary>
    /// Client id for google photo api
    /// </summary>
    private readonly string _envClientId = Environment.GetEnvironmentVariable("GPSC_CLIENTID") ?? string.Empty;
    
    /// <summary>
    /// Client secret for google photo api
    /// </summary>
    private readonly string _envClientSecret = Environment.GetEnvironmentVariable("GPSC_CLIENTSECRET") ?? string.Empty;
    
    /// <summary>
    /// Path to store downloaded media files
    /// </summary>
    private readonly string _envMediaFolderPath = Environment.GetEnvironmentVariable("GPSC_MEDIAFOLDERPATH") ?? string.Empty;
    
    /// <summary>
    /// Comma separated list of albums to sync
    /// </summary>
    private readonly string _envAlbumsToSync = Environment.GetEnvironmentVariable("GPSC_ALBUMSTOSYNC") ?? string.Empty;
    
    /// <summary>
    /// Path to store the synced ids file
    /// </summary>
    private readonly string _envSyncedIdsFilePath = Environment.GetEnvironmentVariable("GPSC_SYNCEDIDSFILEPATH") ?? string.Empty;
    
    /// <summary>
    /// Time between syncs in minutes
    /// </summary>
    private readonly string _envTimeBetweenSyncsInMinutes = Environment.GetEnvironmentVariable("GPSC_TIMEBETWEENMINUTES") ?? string.Empty;
    
    /// <summary>
    /// Path to store the config file
    /// </summary>
    private readonly string _envConfigPath = Environment.GetEnvironmentVariable("GPSC_CONFIGPATH") ?? string.Empty;
    
    /// <summary>
    /// List of synced ids
    /// </summary>
    private readonly List<string> _syncedIds;
    
    /// <summary>
    /// Google photo service
    /// </summary>
    private readonly GooglePhotosService _googlePhotosSvc;
    
    /// <summary>
    /// Logger for <see cref="GooglePhotoCommand"/>
    /// </summary>
    private readonly ILogger<GooglePhotoCommand> _logger;
    
    /// <summary>
    /// Constructor for <see cref="GooglePhotoCommand"/>
    /// </summary>
    /// <exception cref="Exception"></exception>
    public GooglePhotoCommand()
    {
        if(string.IsNullOrEmpty(_envUser)
           || string.IsNullOrEmpty(_envClientId) 
           || string.IsNullOrEmpty(_envClientSecret) 
           || string.IsNullOrEmpty(_envMediaFolderPath) 
           || string.IsNullOrEmpty(_envAlbumsToSync) 
           || string.IsNullOrEmpty(_envSyncedIdsFilePath) 
           || string.IsNullOrEmpty(_envTimeBetweenSyncsInMinutes) 
           || string.IsNullOrEmpty(_envConfigPath))
        {
            throw new($"Not all GPSC enviroment arguments are present GPSC_USER/{_envUser}, GPSC_CLIENTID/{_envClientId}, " +
                                $"GPSC_CLIENTSECRET/***, GPSC_MEDIAFOLDERPATH/{_envMediaFolderPath}, GPSC_ALBUMSTOSYNC/{_envAlbumsToSync}, " +
                                $"GPSC_SYNCEDIDSFILEPATH/{_envSyncedIdsFilePath}, GPSC_TIMEBETWEENMINUTES/{_envTimeBetweenSyncsInMinutes}, " +
                                $"GPSC_CONFIGPATH/{_envConfigPath}");
        }
        
        // Logging stuff
        var serilog =  new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate:"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Method} - {Message:l}{NewLine}{Exception}")
            .CreateLogger();
        
        var loggerFactory = new LoggerFactory().AddSerilog(serilog);
        
        _logger = loggerFactory.CreateLogger<GooglePhotoCommand>();
        var loggerGoogleSvc = loggerFactory.CreateLogger<GooglePhotosService>();
        
        // Create file if missing
        if (!File.Exists(_envSyncedIdsFilePath))
        {
            if (!Directory.Exists(Path.GetDirectoryName(_envSyncedIdsFilePath))) Directory.CreateDirectory(_envSyncedIdsFilePath);
            using (File.Create(_envSyncedIdsFilePath)) { }
        }
        
        // Get ids from file
        _syncedIds = (File.ReadAllLines(_envSyncedIdsFilePath)).ToList();
        
        // Check if media folder exists, else create
        if (!Directory.Exists(_envMediaFolderPath)) Directory.CreateDirectory(_envMediaFolderPath);

        // Create options for google service
        var googlePhotosOptions = new GooglePhotosOptions
        {
            User = _envUser,
            ClientId = _envClientId,
            ClientSecret = _envClientSecret,
            Scopes = new[] {GooglePhotosScope.Access},
            FileDataStoreFullPathOverride = _envConfigPath
        };

        // Http handler and client for service
        var httpClientHandler = new HttpClientHandler {AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate};
        var httpClient = new HttpClient(httpClientHandler) {BaseAddress = new Uri(googlePhotosOptions.BaseAddress)};

        // Google photo service
        _googlePhotosSvc = new GooglePhotosService(loggerGoogleSvc, Options.Create(googlePhotosOptions), httpClient);
    }
    
    /// <summary>
    /// Login to google photo api
    /// </summary>
    /// <exception cref="Exception"></exception>
    public async Task Login()
    {
        if (!await _googlePhotosSvc.LoginAsync()) throw new("login failed!");
    }

    /// <summary>
    ///  Syncs all photos in a specific album.
    /// </summary>
    public async Task Sync()
    {
        // For each album do sync
        var albumTitles = _envAlbumsToSync.Split(",");

        if (albumTitles.IsNullOrEmpty()) _logger.LogWarning("No albums founds");
            
        foreach (var albumTitle in albumTitles)
        {
            // Try to get album
            var album = await _googlePhotosSvc.GetAlbumByTitleAsync(albumTitle);
            if (album is null)
            {
                _logger.LogWarning("Album {AlbumTitle} not found, creating it", albumTitle);
                album = await _googlePhotosSvc.CreateAlbumAsync(albumTitle);

                if (album is null) continue;
            }

            // Sync content found in album
            await foreach (var item in _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id))
            {
                // Only sync new files
                if (_syncedIds.Contains(item.id))
                {
                    _logger.LogWarning("Item already synced {ItemName}", item.filename);
                    continue;
                }

                _logger.LogInformation("Downloading {ItemName}", item.filename);

                var bytes = await _googlePhotosSvc.DownloadBytes(item, 15360, 8640);
                if (bytes is null)
                {
                    _logger.LogError("Downloaded item has 0 bytes, skip saving it");
                    continue;
                }

                await File.WriteAllBytesAsync(Path.Combine(_envMediaFolderPath, item.filename), bytes);

                // Append new id
                _syncedIds.Add(item.id);
                await File.AppendAllLinesAsync(_envSyncedIdsFilePath, new[] {item.id});
            }
        }
    }

    /// <summary>
    /// Get next photo selected by last write time
    /// </summary>
    /// <returns></returns>
    public Task<string> GetNextPhoto()
    {
        return Task.FromResult(new DirectoryInfo(_envMediaFolderPath).GetFiles()
            .OrderBy(f => f.LastWriteTime)
            .Select(f => f.FullName)
            .ToList()
            .First());
    }

    /// <summary>
    /// Delete the specified cached photo
    /// </summary>
    /// <param name="pathToPhoto"></param>
    /// <returns></returns>
    public Task DeleteCachedPhoto(string pathToPhoto)
    {
        File.Delete(pathToPhoto);
        return Task.CompletedTask;
    }
}