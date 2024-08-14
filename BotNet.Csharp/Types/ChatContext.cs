namespace BotNet.Csharp.Types;

public sealed record ChatContext(IServiceProvider ServiceProvider, Chat Chat, User User);