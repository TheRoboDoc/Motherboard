using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using static Motherboard.FileManager;

namespace Motherboard.Response
{
    public static class Handler
    {
        public static readonly EventId HandlerEvent = new EventId(301, "Handler");

        /// <summary>
        /// Runs the response handler that determines to respond or not and how
        /// </summary>
        /// <param name="sender">Discord client</param>
        /// <param name="messageArgs">Discord message creation arguments</param>
        /// <returns>Completed task</returns>
        public static async Task Run(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {                                                                     //Motherboard ID
            if (messageArgs.Author.IsBot && messageArgs?.Author.Id.ToString() != "1103797730276548660") return;

            if (await DiscordNoobFailsafe(messageArgs)) return;

            // Checking if we need to respond at all depending on channel settings
            ChannelManager.Channel channelSettings = ChannelManager.ReadChannelInfo(messageArgs.Guild.Id.ToString(), messageArgs.Channel.Id.ToString());

            if (channelSettings.AIIgnore) return;

            await AIRespond(messageArgs);
        }

        /// <summary>
        /// Responses with AI answer to the message
        /// </summary>
        /// <param name="messageArgs">Discord message creation arguments</param>
        private static async Task AIRespond(MessageCreateEventArgs messageArgs)
        {
            DiscordChannel replyIn = messageArgs.Channel;

            if (!await CheckBotMention(messageArgs))
            {
                return;
            }

            bool typing = true;

            _ = Task.Run(async () =>
            {
                while (typing)
                {
                    await replyIn.TriggerTypingAsync();

                    await Task.Delay(3000);
                }
            });

            Tuple<bool, string?, MemoryStream?> AIGenerationResponse = await AI.GenerateChatResponse(messageArgs);

            typing = false;

            string? response = AIGenerationResponse.Item2;

            if (AIGenerationResponse.Item1)
            {
                DiscordMessageBuilder builder = new DiscordMessageBuilder();

                if (response != null)
                {
                    builder.WithContent(response);
                }

                if (messageArgs.Channel.IsNSFW)
                {
                    MemoryStream? memoryStream = AIGenerationResponse.Item3;

                    if (memoryStream != null)
                    {
                        builder.AddFile("newd.jpg", memoryStream);
                    }
                }

                await replyIn.SendMessageAsync(builder);
            }
            else
            {
                await replyIn.SendMessageAsync("**System:** " + response);
            }
        }

        /// <summary>
        /// A failsafe for when a user tries to execute a slash command but sends it as a plain message instead.
        /// Deletes the failed command message and after 10 seconds deletes the warning message.
        /// </summary>
        /// <param name="sender">Discord client that triggerd this task</param>
        /// <param name="messageArgs">Message creation event arguemnts</param>
        /// <returns>
        /// <list type="table">
        /// <item>True: Failsafe triggered</item>
        /// <item>False: Failsafe not triggered</item>
        /// </list>
        /// </returns>
        private static async Task<bool> DiscordNoobFailsafe(MessageCreateEventArgs messageArgs) //This is redundant as you need to fuck up pretty bad
        {
            if (messageArgs.Author.IsBot || messageArgs.Equals(null)) return false;

            try
            {
                if (messageArgs.Message.Content.First() != '/') return false;
            }
            catch
            {
                if (Program.DebugStatus())
                {
                    Program.BotClient?.Logger.LogInformation(HandlerEvent, "The message was empty");
                }
            }

            //Fetching every slash command the bot has
            SlashCommandsExtension? slashCommandsExtension = Program.BotClient?.GetSlashCommands();

            var slashCommandsList = slashCommandsExtension?.RegisteredCommands;
            List<DiscordApplicationCommand>? globalCommands =
                slashCommandsList?.Where(x => x.Key == null).SelectMany(x => x.Value).ToList(); //This is stupid, can't find a better way as of yet

            if (globalCommands == null)
            {
                Program.BotClient?.Logger.LogWarning(HandlerEvent, "Failed to fetch commands");

                return false;
            }

            List<string> commands = new List<string>();

            foreach (DiscordApplicationCommand globalCommand in globalCommands)
            {
                commands.Add(globalCommand.Name);
            }

            DiscordMessage? message = null;

            bool triggered = false;

            foreach (string command in commands)
            {
                if (messageArgs.Message.Content.Contains(command))
                {
                    await messageArgs.Message.DeleteAsync();

                    message = await messageArgs.Message.RespondAsync
                        ($"{messageArgs.Author.Mention} you tried running a {command} command, but instead send it as a plain message. " +
                        $"That doesn't look very nice for you. So I took the liberty to delete it");

                    triggered = true;

                    // Deletes message
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000);

                        try
                        {
                            await message.DeleteAsync();
                        }
                        catch
                        {
                            Program.BotClient?.Logger.LogWarning(HandlerEvent, "Couldn't delete message {messageID}", message?.Id);
                        }
                    });

                    break;
                }
            }

            return triggered;
        }

        /// <summary>
        /// Checks if the bot was mentioned in a message
        /// </summary>
        /// <param name="messageArgs">Arguments of the message to check</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><c>True</c>: Mentioned</item>
        /// <item><c>False</c>: Not mentioned</item>
        /// </list>
        /// </returns>
        private static async Task<bool> CheckBotMention(MessageCreateEventArgs messageArgs)
        {
            bool botMentioned = false;

            DiscordUser? botUser = Program.BotClient?.CurrentUser;

            await Task.Run(() =>
            {
                foreach (DiscordUser? mentionedUser in messageArgs.MentionedUsers)
                {
                    if (botUser?.Equals(mentionedUser) == new bool?(true))
                    {
                        botMentioned = true;
                        break;
                    }
                }
            });

            return botMentioned;

        }
    }
}
