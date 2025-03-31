namespace ApiApplication.Services;

/// <summary>
/// Interface for cache service operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or default if not found</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Sets a value in the cache
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expirationTime">Optional expiration time</param>
    /// <returns>Task</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null);

    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Task</returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// Gets a value from cache or sets it if it doesn't exist
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Function to create value if not in cache</param>
    /// <param name="expirationTime">Optional expiration time</param>
    /// <returns>The cached or newly created value</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expirationTime = null);
}
