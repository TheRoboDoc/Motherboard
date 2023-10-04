using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Motherboard.Command;
using Motherboard.Response;
using OpenAI;
using OpenAI.Managers;
using System.Diagnostics;

namespace Motherboard
{
    public class Program
    {
        static void Main()
        {
            while (true)
            {
                MainAsync().GetAwaiter().GetResult();
            }
        }

        public static DiscordClient? BotClient;

        public static OpenAIService? OpenAiService;

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns>Nothing</returns>
        static async Task MainAsync()
        {
            OpenAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Tokens.OpenAIToken
            });

            #region Discord Client setup

            LogLevel logLevel;

            if (DebugStatus())
            {
                logLevel = LogLevel.Debug;
            }
            else
            {
                logLevel = LogLevel.Information;
            }

            //Bot config stuff, token, intents etc.
            DiscordConfiguration config = new DiscordConfiguration()
            {
                Token = Tokens.token,
                TokenType = TokenType.Bot,

                Intents =
                DiscordIntents.MessageContents |
                DiscordIntents.Guilds |
                DiscordIntents.GuildPresences |
                DiscordIntents.GuildMessages,

                MinimumLogLevel = logLevel,
                LogUnknownEvents = DebugStatus(),

                LogTimestampFormat = "dd.MM.yyyy HH:mm:ss (zzz)",
            };

            BotClient = new DiscordClient(config);
            #endregion

            BotClient.UseInteractivity(new InteractivityConfiguration());

            //Probably redundant
            ServiceProvider services = new ServiceCollection()
                .BuildServiceProvider();

            #region Command setup
            SlashCommandsConfiguration slashCommandConfig = new SlashCommandsConfiguration()
            {
                Services = services
            };

            SlashCommandsExtension slashCommands = BotClient.UseSlashCommands();

            slashCommands.RegisterCommands<SlashCommands>();
            #endregion

            List<string> dirsMissing = FileManager.DirCheck().Result.ToList();

            if (dirsMissing.Count != 0)
            {
                string message = "Missing following directories:\n";

                foreach (string dirMissing in dirsMissing)
                {
                    string dirMissingText = char.ToUpper(dirMissing[0]) + dirMissing.Substring(1);

                    message += $"\t\t\t\t\t\t\t{dirMissingText}\n";
                }

                BotClient.Logger.LogWarning("{message}", message);
            }

            BotClient.SessionCreated += BotClient_Ready;

            //Connecting the discord client
            await BotClient.ConnectAsync();

            BotClient.Logger.LogInformation("Connected");
            BotClient.Logger.LogInformation("Bot is now operational");

            BotClient.MessageCreated += Handler.Run;

            BotClient.Heartbeated += StatusUpdate;

            //Prevents the task from ending
            await Task.Delay(-1);
        }

        public static string? ChosenStatus;

        /// <summary>
        /// Updates the bots status to a random predetermined value. 
        /// This is called on hearthbeat event, thus requiring heartbeat event arguments
        /// </summary>
        /// <param name="sender">Discord client of the bot</param>
        /// <param name="e">Heartbeat event's arguments</param>
        private static async Task StatusUpdate(DiscordClient sender, HeartbeatEventArgs e)
        {
            Random random = new Random();

            string[] statuses =
            {
                "Vacuuming",
                "Doing the dishes",
                "Dusting off furniture",
                "Preparing food",
                "Cleaning the floor",
                "Doing laundry",
                "Drinking wine",
                "Making sandwiches",
                "Laughing at you",
                "Recharging",
                "Vacuuming the lawn",
                "Putting on the pilot suit",
                "Being a good girl",
                "Best girl of 2023",
                "Pondering the orb",
                "Building up the swarm",
                "Adjusting the swarm",
                "Designating femboys",
                "Giving uppies",
                "Having a mental breakdown",
                "Making hotpockets",
                "Being hot",
                "Buzzing",
                "Killing Trebor"
                };

            try
            {
                ChosenStatus = statuses.ElementAt(random.Next(statuses.Length));
            }
            catch
            {
                BotClient?.Logger.LogWarning("Failed to assigne status, defaulting");
                ChosenStatus = statuses.ElementAt(0);
            }

            DiscordActivity activity = new DiscordActivity(ChosenStatus, ActivityType.Custom);

            await sender.UpdateStatusAsync(activity);
        }

        /// <summary>
        /// Checks if the bot is running in a debug enviroment
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item><c>True</c>: In debug</item>
        /// <item><c>False</c>: Not in debug</item>
        /// </list>
        /// </returns>
        public static bool DebugStatus()
        {
            bool debugState;

            if (Debugger.IsAttached)
            {
                debugState = true;
            }
            else
            {
                debugState = false;
            }

            return debugState;
        }

        /// <summary>
        /// What happens once the client is ready
        /// </summary>
        /// <param name="sender">Client that triggered this task</param>
        /// <param name="e">Ready event arguments arguments</param>
        /// <returns>The completed task</returns>
        private static Task BotClient_Ready(DiscordClient sender, SessionReadyEventArgs e)
        {
            BotClient?.Logger.LogInformation("Client is ready");

            return Task.CompletedTask;
        }
    }
}