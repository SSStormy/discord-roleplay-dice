using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Newtonsoft.Json;

namespace droll
{
    class Config
    {
        public Config(string token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public string Token { get; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(() => Entry(JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"))).ContinueWith((t)
                =>
            {
                if (t.Exception == null) return;

                foreach (var ex in t.Exception.Flatten().InnerExceptions)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            }));

            while (true)
            {
                Console.WriteLine("Waiting for roll...");
                var input = Console.ReadLine();
                try
                {
                    Console.WriteLine($"Rolling {input}");
                    foreach (var result in Roll.Execute(input))
                    {
                        Console.Write($">> {result.Dice.Times}d{result.Dice.Sides} ");
                        if(result.Rolls.Count > 1)
                            Console.Write("--> ");
                            foreach (var roll in result.Rolls)
                                Console.Write($"{roll} ");

                        Console.Write($"--> {result.Rolls.Select(e => e.Result).Sum()}{Environment.NewLine}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                }

            }
        }

        private static async Task Entry(Config cfg)
        {
            var client = new DiscordClient();

            client.Log.Message += (s, m) =>
            {
                Console.WriteLine(m.Message);
            };

            await client.Connect(cfg.Token, TokenType.Bot);

            client.UsingModules();

            client.UsingCommands(c =>
            {
                c.PrefixChar = '.';
                c.ErrorHandler += (s, e) =>
                {
                    Console.WriteLine($"Cmd error: {e.ErrorType}");
                    if(e.Exception != null)
                        ExceptionDispatchInfo.Capture(e.Exception).Throw();
                };
                c.HelpMode = HelpMode.Public;
            });

            client.AddModule<RollModule>();

            await Task.Delay(-1);
        }

        private class RollView
        {
            private readonly List<Roll.DiceResult> _rolls;

            public bool IsDone => _idx >= maxRolls || maxRolls == 1;

            private readonly int maxRolls = 0;
            private readonly int timePad = 0;
            private readonly int sidePad = 0;

            private int _idx;
            private readonly bool[] _shown;

            public RollView(Random rng, IEnumerable<Roll.Expr> expr)
            {
                _rolls = new List<Roll.DiceResult>();

                foreach (var ex in expr)
                {
                    var roll = ex.Execute(rng);

                    var diceResults = roll as Roll.DiceResult[] ?? roll.ToArray();

                    // figure out padding
                    timePad = Math.Max(diceResults.Select(s => s.Dice.Times.ToString().Length).Max(), timePad);
                    sidePad = Math.Max(diceResults.Select(s => s.Dice.Sides.ToString().Length).Max(), sidePad);

                    maxRolls = Math.Max(diceResults.Select(s => s.Rolls.Count).Max(), maxRolls);

                    foreach(var r in diceResults)
                        _rolls.Add(r);
                }

                _shown = new bool[maxRolls];
            }

            public void Next()
            {
                _shown[_idx++] = true;
            }

            public string Draw()
            {
                var sb = new StringBuilder("```\r\n");
                const string arrow = "-->";

                foreach (var roll in _rolls)
                {
                    sb.Append($"{roll.Dice.Times.ToString().PadLeft(timePad)}d{roll.Dice.Sides.ToString().PadRight(sidePad)} ");

                    if(roll.Rolls.Count > 1)
                    {
                        sb.Append($"{arrow} ");
                        for (var i = 0; i < maxRolls; i++)
                        {
                            if (!_shown[i])
                                break;

                            if (roll.Rolls.Count > i)
                                sb.Append($"{roll.Rolls[i]} ");
                        }
                    }

                    if (roll.Rolls.Count == 1 || _idx >= roll.Rolls.Count)
                        sb.Append($"{arrow} {roll.Rolls.Select(s => s.Result).Sum()}");

                    sb.Append(Environment.NewLine);
                }

                return sb + "```";
            }
        }

        public class RollModule : IModule
        {
            private readonly Random _rng = new Random();

            public void Install(ModuleManager manager)
            {
                manager.CreateCommands("", builder =>
                {
                    builder.CreateCommand("r")
                        .Description("Rolls dice. Example formats: d20 d8 1d6 10d20 5*d100 8*2d20")
                        .Parameter("roll", ParameterType.Unparsed)
                        .Do(async e =>
                        {
                            const int delayMs = 850;

                            var data = e.GetArg("roll");
                            if (string.IsNullOrEmpty(data))
                            {
                                await e.Channel.SendMessage("No roll given.");
                                return;
                            }

                            // roll & display
                            var view = new RollView(_rng, Roll.Parse(data));

                            var msg = await e.Channel.SendMessage(view.Draw());

                            while (!view.IsDone)
                            {
                                await Task.Delay(delayMs);
                                view.Next();
                                await msg.Edit(view.Draw());
                            }
                        });
                });
            }
        }
    }
}