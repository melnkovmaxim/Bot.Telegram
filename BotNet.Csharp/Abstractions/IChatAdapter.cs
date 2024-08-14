using BotNet.Csharp.Types;
using BotNet.Csharp.Views;

namespace BotNet.Csharp.Abstractions;

public interface IChatAdapter<TUpdate>
{
    // text - ChatUpdate
    public (Chat chat, User user, string text)? ExtractUpdate(TUpdate update);
    public Task AdaptViewAsync(string chatId, IEnumerable<View> view);
    public Task ResetChatAsync(string chatId);
}