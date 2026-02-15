namespace Chirp.Core.Interfaces;

public interface ILatestRepository
{
    Task<int> GetLatestAsync();
    Task UpdateLatestAsync(int latest);
}