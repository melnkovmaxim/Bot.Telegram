using BotNet.Csharp.Abstractions;
using BotNet.Csharp.Types;

namespace BotNet.Csharp.Views;

public abstract record View
{
    public static View Text(string message)
    {
        return string.IsNullOrEmpty(message)
            ? new EmptyView()
            : new TextView(message);
    }

    public static View Buttons(
        string message, 
        Dictionary<string, Func<Task<IChatState>>> keyboard,
        bool isEditable = false)
    {
        var buttons = keyboard
            .Select(pair => new ReplyButton(pair.Key, pair.Value));

        return new ReplyView(message, buttons, isEditable);
    }
    
    public static View Buttons(
        IEnumerable<string> messages, 
        Dictionary<string, Func<Task<IChatState>>> keyboard,
        bool isEditable = false)
    {
        var message = string.Join("\n", messages
            .Where(m => !string.IsNullOrEmpty(m)));
            
        var buttons = keyboard
            .Select(pair => new ReplyButton(pair.Key, pair.Value));

        return new ReplyView(message, buttons, isEditable);
    }
    
    public static TextHandlerView? TextHandler(Func<string, Task<IChatState>> handler)
    {
        return new TextHandlerView(handler);
    }
    
    public static TextHandlerView TextHandler(Func<string, IChatState> handler)
    {
        return new TextHandlerView((text) => Task.FromResult(handler.Invoke(text)));
    }
}