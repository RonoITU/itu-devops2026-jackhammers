using Chirp.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace Chirp.Core.Interfaces;

public interface ICheepRepository
{
    Task<List<CheepDTO>> ReadCheepsFromAuthor(string userName, int page);
    Task<List<CheepDTO>> ReadAllCheeps(int page);
    Task<List<CheepDTO>> ReadPrivateCheeps(int page, string username);
    Task CreateCheep(CheepDTO newCheep);
    Task UpdateCheep(CheepDTO alteredCheep);
    Task<int> GetTotalPages(string authorName);
    Task DeleteCheep(int cheepId);
    Task DeleteUserCheeps(AuthorDTO author);
    public Task<List<CheepDTO>> GetPopularCheeps(int page);
    Task<int> GetTotalPageNumberForPopular();
    Task<string> HandleImageUpload(IFormFile image);
    Task<List<CheepDTO>> GetMessagesForSimulator(string? username = null, int count = 100);
    Task<List<CheepDTO>> RetrieveAllCheepsForEndPoint();
    Task<List<CheepDTO>> RetrieveAllCheepsFromAnAuthor(string Username);
    Task DeleteComment(int commentId);
    Task HandleLike(string authorName, int cheepId, string? emoji = null);
    Task HandleDislike(string authorName, int cheepId, string? emoji = null);
    Task<List<CommentDTO>> GetCommentsByCheepId(int cheepId);
    Task AddCommentToCheep(CheepDTO cheepDto, string text, string author);
    Task<CheepDTO> GetCheepFromId(int cheepId);
    Task<List<String>> GetTopReactions(int cheepId, int topN = 3);
    Task<List<CommentDTO>> RetriveAllCommentsFromAnAuthor(string Username);
}