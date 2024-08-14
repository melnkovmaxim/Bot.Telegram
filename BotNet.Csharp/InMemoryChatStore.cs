using System.Collections.Concurrent;
using BotNet.Csharp.Abstractions;

namespace BotNet.Csharp;

public sealed class InMemoryChatStore: IChatStateStore
{
    private readonly ConcurrentDictionary<string,IChatState> _dictionary = new();
    
    public Task SaveAsync(string userId, IChatState state)
    {
        _dictionary[userId] = state;

        return Task.CompletedTask;
    }

    public Task<IChatState?> GetAsync(string userId)
    {
        _ = _dictionary.TryGetValue(userId, out var state);

        return Task.FromResult(state);
    }
}