using BotNet;
using BotNet.Csharp;
using BotNet.Csharp.Abstractions;
using BotNet.Csharp.Views;
using BotNet.Telegram;
using BotNet.Telegram.Csharp;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BotHost
{
    public record GetNameState() : IChatState
    {
        public IEnumerable<View> GetView()
        {
            yield return View.Text("What is your name?");
            yield return View.TextHandler(msg => new PrintState(msg));
        }
    }

    public record PrintState(string Text) : IChatState
    {
        public IEnumerable<View> GetView()
        {
            yield return View.Text(Text);
            yield return View.Text("Print any message for go to many keyboards state");
            yield return View.TextHandler(_ => new ManyKeyboards());
        }
    }

    public record CounterState(int Counter = 0, int Step = 1, string? Reply = null) : IChatState
    {
        public IEnumerable<View> GetView()
        {
            var lines = new[]
            {
                Reply,
                $"Counter: {Counter}"
            };
            
            yield return View.Buttons(lines,
                new()
                {
                    { $"Increment by {Step}", () => ChangeCounter(Step) },
                    { $"Decrement by {Step}", () => ChangeCounter(-Step) },
                    { $"Step = 1", () => SetStep(1) },
                    { $"Step = 5", () => SetStep(5) },
                    { $"Step = 10", () => SetStep(10) },
                    { "To GetName State", async () => new GetNameState() }
                }
            );
            
            yield return View.TextHandler(msg => this with { Reply = "Please use buttons" });
        }

        private async Task<IChatState> ChangeCounter(int delta)
            => this with { Counter = Counter + delta, Reply = $"Counter changed: {delta}" };

        private async Task<IChatState> SetStep(int step)
            => this with { Step = step, Reply = $"Step changed to: {step}" };
    }

    public record ManyKeyboards(string? Log = null) : IChatState
    {
        public IEnumerable<View> GetView()
        {
            yield return View.Text(Log);
            yield return View.Buttons("First", 
                new()
                {
                    { "Button 1", async () => this with { Log = "First:Button 1" }},
                    { "Button 2", async () => this with { Log = "First:Button 2" }},
                }
            );
            
            yield return View.Buttons("Second", 
                new()
                {
                    { "Button 1", async () => this with { Log = "Second:Button 1" }},
                    { "To ChatHook state", async () => new ChatHookState()},
                }
            );
        }
    }

    public record ChatHookState() : IChatState
    {
        public IEnumerable<View> GetView()
        {
            var chat = Hook.UseChat();

            yield return View.Text($"Hello {chat.Title}");
            yield return View.Text("Print any message for counter state");
            yield return View.TextHandler(_ => new CounterState());
        }
    }


    public class Program
    {
        [STAThread]
        public static void Main()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient("6804950275:AAGl9JtfOwUrtgy2wl2vRzDJjPJX2kLCJZM"));
            services.AddSingleton<IChatStateStore, InMemoryChatStore>();
            services.AddSingleton<IChatAdapter<Update>, TelegramChatAdapter>();
            services.AddTransient<BotProcessor<Update>>();
            
            var provider = services.BuildServiceProvider();

            var telegram = provider.GetRequiredService<ITelegramBotClient>();
            var processor = provider.GetRequiredService<BotProcessor<Update>>();

            var initState = new CounterState();
            
            telegram.StartReceiving(
                async (bot, update, token) =>
                {
                    await processor.Handle(initState, update, async ex => initState);
                },
                (bot, ex, token) => Task.CompletedTask);

            Console.WriteLine("Bot started");
            Console.ReadLine();
        }
    }
}