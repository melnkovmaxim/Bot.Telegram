using BotNet.Csharp.Types;
using Microsoft.Extensions.DependencyInjection;

namespace BotNet.Csharp;

public static class Hook
{
    private static readonly AsyncLocal<ChatContext> _context = new();

    public static void SetContext(ChatContext context) 
        => _context.Value = context;

    public static T Resolve<T>() where T: notnull
        => _context.Value!.ServiceProvider.GetRequiredService<T>();

    public static Chat UseChat() => _context.Value!.Chat;
    public static User UseUser() => _context.Value!.User;
}