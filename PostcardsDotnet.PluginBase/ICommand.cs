namespace PluginBase;

/// <summary>
/// 
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Command name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Command description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Login
    /// </summary>
    /// <returns></returns>
    public Task Login();
    
    /// <summary>
    /// Sync
    /// </summary>
    /// <returns></returns>
    public Task Sync();
    
    /// <summary>
    /// Get next photo
    /// </summary>
    /// <returns></returns>
    public Task<string> GetNextPhoto();
    
    /// <summary>
    /// Delete specific photo
    /// </summary>
    /// <param name="pathToPhoto"></param>
    /// <returns></returns>
    public Task DeleteCachedPhoto(string pathToPhoto);
}