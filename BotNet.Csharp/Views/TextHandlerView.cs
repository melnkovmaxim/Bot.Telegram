using BotNet.Csharp.Abstractions;

namespace BotNet.Csharp.Views;

public sealed record TextHandlerView(Func<string, Task<IChatState>> Handler): View;