using BotNet.Csharp.Abstractions;
using BotNet.Csharp.Extensions;
using BotNet.Csharp.Types;
using BotNet.Csharp.Views;
using Serilog;

namespace BotNet.Csharp;

public sealed class BotProcessor<TUpdate>(
    IServiceProvider sp,
    IChatAdapter<TUpdate> chatAdapter,
    IChatStateStore stateStore)
{
    public async Task Handle(IChatState initialState, TUpdate update, Func<Exception, Task<IChatState>> onError)
    {
        var extractedUpdate = chatAdapter.ExtractUpdate(update);
        
        if (extractedUpdate is null)
        {
            return;
        }

        (Chat chat, User user, string text) = extractedUpdate.Value;
        
        Hook.SetContext(new ChatContext(sp, chat, user));

        IChatState currentState = await TryGetStateAsync(user.Id, initialState, onError);

        var views = currentState.GetView().ToArray();
        var textHandler = views.GetTextHandler();
        var isNumber = int.TryParse(text, out var index);
        var newState = currentState;

        if (!isNumber && textHandler is not null)
        {
            newState = await TryHandle(textHandler, text, onError);
        }
        else if (isNumber)
        {
            var btn = GetButton(views, index);

            if (btn is not null)
            {
                newState = await btn.Callback();
            }
        }

        if (newState == initialState || newState == currentState)
        {
            Log.Information("State no changed {ChatId}", chat.Id);
        }
        else
        {
            Log.Information("Changed state {ChatId} {FromState} -> {ToState}", chat.Id, currentState, newState);

            await chatAdapter.AdaptViewAsync(chat.Id, newState.GetView());
            await stateStore.SaveAsync(user.Id, newState);
        }
    }

    private async Task<IChatState> TryHandle(
        TextHandlerView view, 
        string update, 
        Func<Exception, Task<IChatState>> onError)
    {
        try
        {
            return await view.Handler.Invoke(update);
        }
        catch (Exception ex)
        {
            return await onError.Invoke(ex);
        }
    }

    private async Task<IChatState> TryGetStateAsync(
        string userId, 
        IChatState initialState,
        Func<Exception, Task<IChatState>> onError)
    {
        try
        {
            var currentState = await stateStore.GetAsync(userId);

            return currentState ?? initialState;
        }
        catch (Exception ex)
        {
            return await onError.Invoke(ex);
        }
    }
    
    public async Task SetStateAsync(string chatId, IChatState state)
    {
        await chatAdapter.ResetChatAsync(chatId);
        await chatAdapter.AdaptViewAsync(chatId, state.GetView());
        await stateStore.SaveAsync(chatId, state);
    }

    private ReplyButton? GetButton(IEnumerable<View> views, int index)
    {
        var buttons = views.GetButtons().ToArray();

        if (index >= 0 && index < buttons.Length)
        {
            return buttons[index];
        }

        return null;
    }
}