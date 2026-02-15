using Microsoft.EntityFrameworkCore;
using Chirp.Core;
using Chirp.Core.Interfaces;

namespace Chirp.Infrastructure.Repositories;

/// <summary>
/// Repository for managing system configuration values in the database.
/// Handles data access for the SystemConfig entity.
/// </summary>
public class LatestRepository : ILatestRepository
{
    private readonly CheepDBContext _dbContext;
    
    public LatestRepository(CheepDBContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    /// <summary>
    /// Retrieves the latest simulator command ID from the database.
    /// </summary>
    /// <returns>The latest command ID, or -1 if not found.</returns>
    public async Task<int> GetLatestAsync()
    {
        // Find the config entry by key
        var config = await _dbContext.SystemConfig
            .FirstOrDefaultAsync(c => c.Key == "simulator_latest");
        
        // Return the value or -1 if not found
        return config?.IntValue ?? -1;
    }
    
    /// <summary>
    /// Updates the latest simulator command ID in the database.
    /// Creates a new entry if it doesn't exist.
    /// </summary>
    /// <param name="latest">The latest command ID to store.</param>
    public async Task UpdateLatestAsync(int latest)
    {
        // Find the config entry by key
        var config = await _dbContext.SystemConfig
            .FirstOrDefaultAsync(c => c.Key == "simulator_latest");
        
        if (config == null)
        {
            // Create a new SystemConfig entry if not found
            config = new SystemConfig
            {
                Key = "simulator_latest",
                IntValue = latest
            };
            await _dbContext.SystemConfig.AddAsync(config);
        }
        else
        {
            // Update the existing entry
            config.IntValue = latest;
            _dbContext.SystemConfig.Update(config);
        }
        
        // Persist the changes to the database
        await _dbContext.SaveChangesAsync();
    }
}
