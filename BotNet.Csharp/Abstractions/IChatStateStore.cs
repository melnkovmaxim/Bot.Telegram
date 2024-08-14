namespace BotNet.Csharp.Abstractions;

public interface IChatStateStore
{
    Task SaveAsync(string userId, IChatState state);
    Task<IChatState?> GetAsync(string userId);
}