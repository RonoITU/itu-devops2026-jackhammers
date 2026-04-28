using Chirp.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace Chirp.Core.Interfaces;

public interface ICheepService
{
    public Task<List<CheepDTO>> GetCheeps(int page);
    public Task<List<CheepDTO>> GetCheepsFromAuthor(string author, int page);
    
    public Task<int> GetTotalPageNumber(string authorName  = "");
    
    public Task<List<CheepDTO>> RetrieveAllCheeps();
    
    public Task CreateCheep(CheepDTO cheep);
    
    public Task<List<CheepDTO>> RetrieveAllCheepsFromAnAuthor(string authorName);
   
    public Task DeleteUserCheeps(AuthorDTO author);
    
    public Task HandleLike(string authorName, int cheepId, string? emoji);
    public Task HandleDislike(string authorName, int cheepId, string? emoji);
    public Task<List<CheepDTO>> GetPopularCheeps(int page);
    public Task<int> GetTotalPageNumberForPopular();
    public Task<string> HandleImageUpload(IFormFile image);
    
    Task<List<CheepDTO>> GetMessagesForSimulator(string? username = null, int count = 100);
    Task<CheepDTO?> GetCheepFromId(int cheepId);
    Task<List<CommentDTO>> GetCommentsByCheepId(int cheepId);
    Task AddCommentToCheep(CheepDTO cheepDto, string text, string author);
    Task DeleteComment(int commentId);
    Task<List<CheepDTO>> GetPrivateCheeps(int page, string username);
    Task<List<string>> GetTopReactions(int cheepId);
    Task<Dictionary<int, List<string>>> GetTopReactionsDictionary(int[] cheepIds);
    Task DeleteCheep(int cheepId);
    Task<List<CommentDTO>> RetrieveAllCommentsFromAnAuthor(string authorName);
    Task<long> TotalCheepsPosted();
}