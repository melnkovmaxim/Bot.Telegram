using BotNet.Csharp.Views;

namespace BotNet.Csharp.Abstractions;

public interface IChatState
{
    public IEnumerable<View> GetView();
}