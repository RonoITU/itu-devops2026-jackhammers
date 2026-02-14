using Chirp.Infrastructure.Repositories;

namespace Chirp.Infrastructure.Services;

/// <summary>
/// Service for tracking the latest command ID from the simulator.
/// Provides business logic layer over LatestRepository.
/// </summary>
public class LatestService
{
    private readonly LatestRepository _latestRepository;
    
    public LatestService(LatestRepository latestRepository)
    {
        _latestRepository = latestRepository;
    }
    
    /// <summary>
    /// Gets the latest simulator command ID.
    /// </summary>
    /// <returns>The latest command ID, or -1 if not set.</returns>
    public async Task<int> GetLatestAsync()
    {
        return await _latestRepository.GetLatestAsync();
    }
    
    /// <summary>
    /// Updates the latest simulator command ID if a value is provided.
    /// </summary>
    /// <param name="latest">The latest command ID to update (optional).</param>
    public async Task UpdateLatestAsync(int? latest)
    {
        if (!latest.HasValue) return;
        
        await _latestRepository.UpdateLatestAsync(latest.Value);
    }
}