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
        /// <summary>
        /// Runs the response handler that determines to respond or not and how
        /// </summary>
        /// <param name="sender">Discord client</param>
        /// <param name="messageArgs">Discord message creation arguments</param>
        /// <returns>Completed task</returns>
        public static async Task Run(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {                                                                        //Robit ID
            if (messageArgs.Author.IsBot && messageArgs?.Author.Id.ToString() != "1049457745763500103") return;

            if (await DiscordNoobFailsafe(messageArgs)) return;

            // Checking if we need to respond at all depending on channel settings
            ChannelManager.Channel channelSettings = ChannelManager.ReadChannelInfo(messageArgs.Guild.Id.ToString(), messageArgs.Channel.Id.ToString());

            if (channelSettings.AIIgnore) return;

            DiscordChannel replyIn = messageArgs.Channel;

            if (!await CheckBotMention(messageArgs))
            {
                return;
            }

            await replyIn.TriggerTypingAsync();

            Tuple<bool, string> AIGenerationResponse = await AI.GenerateChatResponse(messageArgs);

            string response = AIGenerationResponse.Item2;

            if (AIGenerationResponse.Item1)
            {
                await replyIn.SendMessageAsync(response);
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
        private static async Task<bool> DiscordNoobFailsafe(MessageCreateEventArgs messageArgs)
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
                    Program.BotClient?.Logger.LogInformation("The message was empty");
                }
            }

            SlashCommandsExtension slashCommandsExtension = Program.BotClient.GetSlashCommands();

            var slashCommandsList = slashCommandsExtension.RegisteredCommands;
            List<DiscordApplicationCommand> globalCommands =
                slashCommandsList.Where(x => x.Key == null).SelectMany(x => x.Value).ToList();

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
                    break;
                }
            }

            // Delets message
            _ = Task.Run(async () =>
            {
                if (message != null)
                {
                    await Task.Delay(10000);
                    await message.DeleteAsync();
                }
            });

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

            await Task.Run(() =>
            {
                foreach (DiscordUser mentionedUser in messageArgs.MentionedUsers)
                {
                    if (mentionedUser == Program.BotClient?.CurrentUser)
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
