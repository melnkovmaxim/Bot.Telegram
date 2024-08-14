using BotNet.Csharp.Types;
using BotNet.Csharp.Views;

namespace BotNet.Csharp.Extensions;

internal static class EnumerableViewBaseExtensions
{
    public static TextHandlerView? GetTextHandler(this IEnumerable<View> views)
    {
        return views
            .OfType<TextHandlerView>()
            .FirstOrDefault();
    }

    public static IEnumerable<ReplyButton> GetButtons(this IEnumerable<View> views)
    {
        return views
            .OfType<ReplyView>()
            .SelectMany(v => v.Buttons2);
    }
}