using System.Collections.Concurrent;
using BotNet.Csharp;
using BotNet.Csharp.Abstractions;
using BotNet.Csharp.Types;
using BotNet.Csharp.Views;
using RateLimiter;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Chat = BotNet.Csharp.Types.Chat;
using User = Telegram.Bot.Types.User;

namespace BotNet.Telegram.Csharp;

public sealed class TelegramChatAdapter(ITelegramBotClient client) : IChatAdapter<Update>
{
    private readonly ConcurrentDictionary<string, TimeLimiter> _limiter = new();
    private readonly ConcurrentDictionary<string, int> _lastMessageId = new();
    private readonly TimeLimiter _globalLimiter = TimeLimiter.GetFromMaxCountByInterval(30, TimeSpan.FromSeconds(1));

    public (Chat chat, BotNet.Csharp.Types.User user, string text)? ExtractUpdate(Update update)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
            {
                var msg = update.Message;
                var chat = new Chat(msg!.Chat.Id.ToString(), msg.Chat.Title!);
                var user = GetUser(msg.From!);

                if (msg.Type != MessageType.Text) return null;

                return (chat, user, FixStr(msg.Text))!;
                
                break;
            }
            case UpdateType.CallbackQuery:
            {
                var callback = update.CallbackQuery;
                var chat = new Chat(callback!.Message!.Chat.Id.ToString(), callback.Message!.Chat.Title!);
                var user = GetUser(callback.From);
                
                return (chat, user, FixStr(callback.Data))!;
            }
            default:
                return null;
        }

        BotNet.Csharp.Types.User GetUser(User user)
        {
            return new BotNet.Csharp.Types.User(user.Id.ToString(), FixStr(user.Username), FixStr(user.FirstName),
                FixStr(user.LastName));
        }
    }

    public async Task AdaptViewAsync(string chatId, IEnumerable<View> views)
    {
        async Task LogError(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while send telegram request");
            }
        }

        void AppendMessage(Message message)
        {
            var messageId = message.MessageId;
            
            _ = _lastMessageId.AddOrUpdate(chatId, messageId, (key, old) => messageId);
        }

        async Task SendTypingAsync(TimeSpan delay)
        {
            await WaitLimit(chatId);
            await client.SendChatActionAsync(chatId, ChatAction.Typing);
            await Task.Delay(delay);
        }
        
        Func<int> CreateIncrementalCounter()
        {
            var index = -1;

            return () => ++index;
        }

        var btnIndex = CreateIncrementalCounter();

        InlineKeyboardMarkup MakeButtons(IEnumerable<ReplyButton> buttons)
        {
            
            var rows = buttons
                .Select(btn => InlineKeyboardButton.WithCallbackData(btn.Text, $"{btnIndex()}"))
                .Chunk(3)
                .ToArray();

            return new InlineKeyboardMarkup(rows);
        }

        async Task SendMessageAsync(string text, IEnumerable<ReplyButton> buttons)
        {
            await WaitLimit(chatId);
            
            var message = await client.SendTextMessageAsync(chatId, text, replyMarkup: MakeButtons(buttons), parseMode: ParseMode.Html);
            
            AppendMessage(message);
        }

        async Task EditMessageAsync(int messageId, string text, IEnumerable<ReplyButton> buttons)
        {
            await WaitLimit(chatId);
            
            var message = await client.EditMessageTextAsync(chatId, messageId, text, replyMarkup: MakeButtons(buttons), parseMode: ParseMode.Html);
            
            AppendMessage(message);
        }

        async Task SendOrEditMessageAsync(string text, IEnumerable<ReplyButton> buttons)
        {
            var hasMessage = _lastMessageId.TryGetValue(chatId, out var lastMessageId);

            if (hasMessage)
            {
                try
                {
                    await EditMessageAsync(lastMessageId, text, buttons);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while edit message");
                }
            }
            else
            {
                await SendMessageAsync(text, buttons);
            }
        }
        
        foreach (var view in views)
        {
            var task = view switch
            {
                EmptyView _ => Task.CompletedTask,
                TextView textView => LogError(SendMessageAsync(textView.Text2, [])),
                ReplyView { IsEditable: false } replyView 
                    => LogError(SendMessageAsync(replyView.Message, replyView.Buttons2)),
                ReplyView { IsEditable: true } replyView 
                    => LogError(SendOrEditMessageAsync(replyView.Message, replyView.Buttons2)),
                TextHandlerView _ => Task.CompletedTask, 
                _ => Task.CompletedTask
            };

            await task;
        }
    }

    public Task ResetChatAsync(string chatId)
    {
        _lastMessageId.TryRemove(chatId, out _);

        return Task.CompletedTask;
    }

    private string FixStr(string? str)
    {
        return string.IsNullOrEmpty(str)
            ? string.Empty
            : str;
    }

    private TimeLimiter GetLimiterByChat(string chatId)
    {
        return _limiter.GetOrAdd(chatId, _ => TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromSeconds(1)));
    }

    private async Task WaitLimit(string chatId)
    {
        await _globalLimiter.Enqueue(() => { });
        await GetLimiterByChat(chatId).Enqueue(() => { });
    }
}