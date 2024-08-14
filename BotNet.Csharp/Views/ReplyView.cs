using BotNet.Csharp.Types;

namespace BotNet.Csharp.Views;

public sealed record ReplyView(string Message, IEnumerable<ReplyButton> Buttons2, bool IsEditable): View;