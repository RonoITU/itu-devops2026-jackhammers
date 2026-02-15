namespace Chirp.Core.Interfaces;

public interface ILatestService
{
    Task<int> GetLatestAsync();
    Task UpdateLatestAsync(int? latest);
}