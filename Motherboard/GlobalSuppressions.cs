﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Style choice I want to keep", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Will break the handler if applied", Scope = "member", Target = "~M:Motherboard.Response.Handler.Run(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.MessageCreateEventArgs)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Will break if applied", Scope = "member", Target = "~F:Motherboard.Program.botClient")]
[assembly: SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Will break if applied", Scope = "member", Target = "~F:Motherboard.Program.openAiService")]
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "No", Scope = "member", Target = "~M:Motherboard.Program.MainAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "No", Scope = "member", Target = "~M:Motherboard.Command.SlashCommands.Commands(DSharpPlus.SlashCommands.InteractionContext)~System.Threading.Tasks.Task")]
