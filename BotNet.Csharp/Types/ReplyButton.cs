using BotNet.Csharp.Abstractions;

namespace BotNet.Csharp.Types;

public sealed record ReplyButton(string Text, Func<Task<IChatState>> Callback);