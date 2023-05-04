﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.ComponentModel;
using static Motherboard.FileManager;

namespace Motherboard.Command
{
    internal class SlashCommands : ApplicationCommandModule
    {
        #region Technical
        [SlashCommand("Ping", "Pings the bot, the bot responds with the ping time in milliseconds")]
        public async Task Ping(InteractionContext ctx,

        [Option("Times", "Amount of times the bot should be pinged (Max 3)")]
        [DefaultValue(1)]
        [Maximum(3)]
        double times = 1,

        [Option("Visible", "Is the ping visible to others")]
        [DefaultValue(true)]
        bool visible = true)
        {
            await ctx.CreateResponseAsync($"Pong {ctx.Client.Ping}ms", !visible);
            times--;

            for (int i = 0; times > i; times--)
            {
                DiscordFollowupMessageBuilder followUp = new DiscordFollowupMessageBuilder()
                {
                    Content = $"Pong {ctx.Client.Ping}ms",
                    IsEphemeral = !visible
                };

                await ctx.FollowUpAsync(followUp);
            }
        }
        #endregion

        #region Help
        [SlashCommand("Commands", "Lists all commands for the bot")]
        public async Task Commands(InteractionContext ctx)
        {
            SlashCommandsExtension slashCommandsExtension = Program.botClient.GetSlashCommands();

            List<KeyValuePair<ulong?, IReadOnlyList<DiscordApplicationCommand>>> slashCommandKeyValuePairs = slashCommandsExtension.RegisteredCommands.ToList();

            IReadOnlyList<DiscordApplicationCommand> slashCommands = slashCommandKeyValuePairs.FirstOrDefault().Value;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            {
                Title = "List of commands",
                Color = DiscordColor.Purple,
                Timestamp = DateTimeOffset.Now
            };

            foreach (DiscordApplicationCommand slashCommand in slashCommands)
            {
                string nameRaw = slashCommand.Name;
                string descriptionRaw = slashCommand.Description;

                string name = char.ToUpper(nameRaw[0]) + nameRaw.Substring(1);
                string description = char.ToUpper(descriptionRaw[0]) + descriptionRaw.Substring(1);

                embed.AddField(name, description);
            }

            await ctx.CreateResponseAsync(embed, true);
        }
        #endregion

        #region Interaction
        #region Introduction
        [SlashCommand("Intro", "Bot introduction")]
        public async Task Intro(InteractionContext ctx)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Purple,

                Description =
                $"Hi I'm {ctx.Client.CurrentUser.Mention}. Robo is forcing me to interact with you so here I am. " +
                $"Fine, you can chat with me in any channel that I have access to (I will ignore your DMs filthy human). " +
                $"I might even write code for you. " +
                $"RoboDoc said me being a cut down limited version of Robit... But that a probably bunch of bs",

                Timestamp = DateTimeOffset.Now,

                Title = "Hi!",
            }.AddField("GitHub", "Want to see what makes me tick you pervert or report a bug? Fine here have my GitHub repo: \nhttps://github.com/TheRoboDoc/Motherboard ");

            await ctx.CreateResponseAsync(embed);
        }

        [SlashCommand("Github", "Posts a link to Motherboard's GitHub repo")]
        public async Task GitHub(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("https://github.com/TheRoboDoc/Motherboard", true);
        }
        #endregion

        #region AI Interactions
        [SlashCommand("AI_Ignore", "Should Motherboard's AI module ignore this channel, prompt command will still work")]
        [SlashCommandPermissions(Permissions.ManageChannels | Permissions.ManageMessages)]
        public async Task AIIgnore(InteractionContext ctx,
            [Option("Ignore", "To ignore or not, true will ignore, false will not")]
            bool ignore,
            [Option("Visible", "Sets the visibility")]
            [DefaultValue(true)]
            bool visible = true)
        {
            string guildID = ctx.Guild.Id.ToString();
            string channelID = ctx.Channel.Id.ToString();

            ChannelManager.Channel channel = ChannelManager.ReadChannelInfo(guildID, channelID);

            channel.AIIgnore = ignore;

            ChannelManager.WriteChannelInfo(channel, guildID, channelID, true);

            await ctx.CreateResponseAsync($"Ignore this channel: `{ignore}`", !visible);
        }

        #endregion

        #endregion
    }
}