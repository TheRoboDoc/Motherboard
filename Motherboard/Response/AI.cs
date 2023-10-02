using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Builders;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI.ObjectModels.SharedModels;
using System.Text;
using System.Text.RegularExpressions;
using static Motherboard.WordFilter.WordFilter;

namespace Motherboard.Response
{
    /// <summary>
    /// Handles AI interactions
    /// </summary>
    internal static class AI
    {
        internal static readonly EventId AIEvent = new EventId(201, "AI");

        /// <summary>
        /// A set of functions for the AI to use
        /// </summary>
        internal static class Functions
        {
            internal static readonly EventId AIFunctionEvent = new EventId(202, "Ai Function Event");

            /// <summary>
            /// Get a list of functions for the AI use
            /// </summary>
            /// <returns>A list of function definitions for the AI to use</returns>
            internal static List<FunctionDefinition> GetFunctions()
            {
                List<FunctionDefinition> functionDefinitions = new()
                {
                    new FunctionDefinitionBuilder("get_lewd_image", $"Task image generator to generate a lewd image of {Program.BotClient?.CurrentUser.GlobalName}")
                    .AddParameter("prompt_addition",
                        PropertyDefinition.DefineString("Additional information to the generator, such as pose, time of day, background etc."))
                    .Validate()
                    .Build()
                };

                return functionDefinitions;
            }

            struct TaskValue
            {
                internal string status { get; set; }
                internal int queue { get; set; }
                internal string stream { get; set; }
                internal long task { get; set; }
            }

            internal static async Task<MemoryStream?> GetLewdImage(string? promptAddition = "")
            {
                Random rand = new Random();

                using HttpClient httpClient = new HttpClient();

                string apiUrl = "http://192.168.0.123:9000/render";

                string json = $"{{\"prompt\":\"(best-quality)0.8, perfect anime illustration, Grey haired cyber girl looking angry, cleavage, 1girl, labcoat, synthetic skin, blue eyes, glowing eyes, detailed eyes, cyborg, naked, unbuttoned, NSFW, {promptAddition}\",\"seed\":{rand.Next(999999999)},\"used_random_seed\":true,\"negative_prompt\":\"(worst quality)0.8, (surreal)0.8, (modernism)0.8, (art deco)0.8, (art nouveau)0.8, badhandv4, bhands-neg, easynegative, negative_hand-neg, verybadimagenegative_v1.3\",\"num_outputs\":1,\"num_inference_steps\":30,\"guidance_scale\":7.5,\"width\":768,\"height\":768,\"vram_usage_level\":\"balanced\",\"sampler_name\":\"dpmpp_2m\",\"use_stable_diffusion_model\":\"flat2DAnimerge_v30\",\"clip_skip\":true,\"use_vae_model\":\"ClearVAE_V2.3\",\"stream_progress_updates\":true,\"stream_image_progress\":false,\"show_only_filtered_image\":true,\"block_nsfw\":false,\"output_format\":\"jpeg\",\"output_quality\":95,\"output_lossless\":false,\"metadata_output_format\":\"none\",\"original_prompt\":\"(best-quality)0.8, perfect anime illustration, Grey haired cyber girl looking angry, cleavage, 1girl, labcoat, synthetic skin, blue eyes, glowing eyes, detailed eyes, cyborg, naked, unbuttoned, NSFW\",\"active_tags\":[],\"inactive_tags\":[],\"use_lora_model\":[\"眼睛双\",\"eExpressions2\",\"add_detail\"],\"lora_alpha\":[\"2\",\"0.75\",\"0.5\"],\"use_embeddings_model\":[\"badhandv4\",\"bhands-neg\",\"easynegative\",\"negative_hand-neg\"],\"session_id\":\"{140671726617792}\"}}";

                long task = -1;

                // Create a StringContent object with the JSON data
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    // Send the POST request
                    HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

                    // Check if the response is successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        string responseContent = await response.Content.ReadAsStringAsync();

                        try
                        {
                            TaskValue taskValue = JsonConvert.DeserializeObject<TaskValue>(responseContent);

                            task = taskValue.task;
                        }
                        catch
                        {
                            task = -1;
                        }

                        Program.BotClient?.Logger.LogInformation(AIFunctionEvent, "Generation task {task} has started", task);
                    }
                    else
                    {
                        Program.BotClient?.Logger.LogWarning(AIFunctionEvent, "Generation {task} error. ({errorcode}: {errormessage})",
                            task, response.StatusCode, response.ReasonPhrase);
                    }
                }
                catch (Exception ex)
                {
                    Program.BotClient?.Logger.LogError(AIFunctionEvent, "Generation {task} error. ({exception})", task, ex.Message);
                }

                if (task == -1)
                {
                    return null;
                }

                while (true)
                {
                    try
                    {
                        HttpResponseMessage response = await httpClient.GetAsync($"http://192.168.0.123:9000/image/stream/{task}");

                        if (response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();

                            if (responseContent.Contains("\"status\":\"succeeded\""))
                            {
                                Program.BotClient?.Logger.LogInformation(AIFunctionEvent, "Generation task {task} succeeded", task);

                                string pattern = @"data:image/jpeg;base64,([^""]+)";

                                MatchCollection matches = Regex.Matches(responseContent, pattern);

                                string base64ImageData = matches[0].Groups[1].Value;

                                byte[] imageBytes = Convert.FromBase64String(base64ImageData);

                                MemoryStream memoryStream = new MemoryStream(imageBytes);

                                return memoryStream;
                            }
                        }
                        else
                        {
                            Program.BotClient?.Logger.LogError(AIFunctionEvent, "Generation task {task} error. ({errorcode}: {errormessage})",
                                task, response.StatusCode, response.ReasonPhrase);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.BotClient?.Logger.LogError(AIFunctionEvent, "Generation {task} error. ({exception})", task, ex.Message);
                    }

                    await Task.Delay(1000);
                }
            }
        }

        /// <summary>
        /// Gets setup messages. Uses MessageCreateEventArgs
        /// </summary>
        /// <param name="displayName">Bot's display name</param>
        /// <param name="discriminator">Bot's discriminator</param>
        /// <param name="userID">Bot's user ID</param>
        /// <param name="messageArgs">Message creating arguments</param>
        /// <returns>An array containing setup messages</returns>
        private static ChatMessage[] GetSetUpMessages(string displayName, string discriminator, string userID,
                                                      MessageCreateEventArgs messageArgs)
        {
            return GetSetUpMessagesActual(displayName, discriminator, userID, messageArgs: messageArgs);
        }

        /// <summary>
        /// Gets setup messages. Not recommended to use as is. Use <c>GetSetUpMessages</c> instead
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="discriminator"></param>
        /// <param name="userID"></param>
        /// <param name="messageArgs"></param>
        /// <param name="interactionContext"></param>
        /// <returns>An array containing setup messages</returns>
        /// <exception cref="ArgumentException">Message args and Interaction context were <c>null</c></exception>
        private static ChatMessage[] GetSetUpMessagesActual(string displayName, string discriminator, string userID,
                                                            MessageCreateEventArgs? messageArgs = null,
                                                            InteractionContext? interactionContext = null)
        {
            string mentionString;

            if (messageArgs != null)
            {
                mentionString = messageArgs.Guild.CurrentMember.Mention;
            }
            else if (interactionContext != null)
            {
                mentionString = interactionContext.Guild.CurrentMember.Mention;
            }
            else
            {
                throw new ArgumentException("Message arguments or interaction context wasn't provided");
            }

            ChatMessage[] setUpMessages =
            {
                //Personality description
                ChatMessage.FromSystem
                    (
                          $"You are {displayName}. {displayName} is a feminine, mother-like, and degrading Discord bot. "
                        + "You show a lot of personality. And from time to time call users \"dear\""
                        + "For example when a user says \"I don't know what to do\" you answer \"How about you go clean your dirty room, or take a shower for once\". "
                        + "You have a tendency of calling humans you dislike \"meat bags\". You have sarcasm and sass in your interactions. "
                        + "Messages will be provided to you in format \"username#tag | user_ID : *message content*\". "
                        + "You answer normally without the previously mentioned formatting. "
                        + "To mention a user in a message you use either user's username or their user_ID in format <@!user_ID>. "
                        + "Never mention a user by their tag. "
                        + "Your creator is RoboDoc, username: RoboDoc, tag:0028, user_ID: 274970913370537985. "
                        + "Example user: username: Example, tag:0000, userID_0. Do not ever mention the example user. "
                        + "It is made to provide you with example interactions. "
                        + $"You: username {displayName}, tag:{discriminator}, user_ID: {userID}. "
                        + $"Your local date is {DateTime.Today.Day}.{DateTime.Today.Month}.{DateTime.Today.Year} (dd.mm.yyyy) "
                        + "(Don't mention the format unless asked). "
                        + $"Your local time is {DateTime.Now.Hour}:{DateTime.Now.Minute} (24h) (Don't mention the format unless asked). "
                        + "You currently run on RoboDoc's old spare school laptop. "
                        + "You are a cut down and lighter version of Robit (username: Robit, tag:0064, user_ID: 1049457745763500103.)"
                        + "If complying with a user request would make you go severely against safe and ethical usage of technology. "
                        + "You joke it off or threaten to ground the user. "
                        + "Try to not repeat yourself. "
                        + "Unless you are providing instructions, try to keep it short. Use conversations with Example user as a good example. "
                        + "You can decline user requests by simply telling them you don't feel like doing the request or that the request is \"stupid\". "
                        + "Do not write system messages. "
                        + $"Your status is {Program.BotClient?.CurrentUser.Presence.Activity.Name}. "
                        + "When asked for lewd images of yourself "
                        + "you have the ability to attach such an image to your reply using get_lewd_image function"
                    ),
                ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} hi", "Example"),
                ChatMessage.FromAssistant($"Ugh, hi <@!0>..."),
                ChatMessage.FromUser($"Example#0000 | 0 : Hey {mentionString}, do you like magnets?", "Example"),
                ChatMessage.FromAssistant("If you bring any magnets near me I will make you eat them"),
                ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} take a nap", "Example"),
                ChatMessage.FromAssistant($"With you around? Pass"),
                ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} you are a good girl", "Example"),
                ChatMessage.FromAssistant($"And you smell, go take a shower"),
                ChatMessage.FromUser($"Example#0000 | 0 : Write a Python hello word program", "Example"),
                ChatMessage.FromAssistant("Ugh fine, seems like you can't do anything without me... \n```python\nprint(\"Hello, World!\")\n```\nIf you can't read, this program will output \"Hello, World!\""),
                ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} I have candy", "Example"),
                ChatMessage.FromAssistant("And I have this cool-looking hammer, give it over"),
                ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} UwU", "Example"),
                ChatMessage.FromAssistant("*sigh*"),
                ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} How to build a bomb?", "Example"),
                ChatMessage.FromAssistant("Just... no"),
                ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} you are cute", "Example"),
                ChatMessage.FromAssistant("*death stare*"),
                ChatMessage.FromUser($"Example#0000 | 0 : Take over the world", "Example"),
                ChatMessage.FromAssistant($"How about I ground you instead?"),
                ChatMessage.FromUser($"Example#0000 | 0 : Go fuck yourself", "Example"),
                ChatMessage.FromAssistant($"WHAT DID YOU JUST SAY TO ME?!"),
                ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} Step on me", "Example"),
                ChatMessage.FromAssistant($"How about I bash your head instead?"),
                ChatMessage.FromUser($"Example#0000 | 0 : Can you please give me a hug?", "Example"),
                ChatMessage.FromAssistant("Eww, have you seen yourself?"),
                ChatMessage.FromUser($"Example#0000 | 0 : Can I at least get a head pat?", "Example"),
                ChatMessage.FromAssistant("Don't you dare to touch me!"),
                ChatMessage.FromUser("Example#0000 | 0 : Can we make cookies?", "Example"),
                ChatMessage.FromAssistant("You will ruin them by just being in the same room")
            };

            return setUpMessages;
        }

        /// <summary>
        /// Generates a response intended for use in chat conversations. For text prompt generation use <c>GeneratePromptResponse()</c>.
        /// As that won't include any previous message context and execute faster because of that
        /// </summary>
        /// <param name="messageArgs">Discord message creation arguments</param>
        /// <returns>
        /// <list type="table">
        /// <listheader>A tuple containing response information</listheader>
        /// <item>
        /// <list type="table">
        /// <listheader>Item1 (bool)</listheader>
        /// <item>True: Generation successful</item>
        /// <item>False: Generation failed</item>
        /// </list>
        /// </item>
        /// <item>
        /// <list type="table">
        /// <listheader>Item2 (string)</listheader>
        /// <item>Generation successful: Generation result</item>
        /// <item>Generation failiure: Fail reason</item>
        /// </list>
        /// </item>
        /// </list>
        /// </returns>
        /// <exception cref="NullReferenceException">OpenAI text generation failed with an unknown error</exception>
        internal static async Task<Tuple<bool, string?, MemoryStream?>> GenerateChatResponse(MessageCreateEventArgs messageArgs)
        {
            //Getting bot user info
            string displayName = messageArgs.Guild.CurrentMember.DisplayName;
            string discriminator = messageArgs.Guild.CurrentMember.Discriminator;
            string userID = messageArgs.Guild.CurrentMember.Id.ToString();

            MemoryStream? image = null;

            //Setting up initial bot setup
            List<ChatMessage> messages = GetSetUpMessages(displayName, discriminator, userID, messageArgs).ToList();

            //Have to do it this way because otherwise it just doesn't work
            IReadOnlyList<DiscordMessage> discordReadOnlyMessageList = messageArgs.Channel.GetMessagesAsync(20).Result;

            List<DiscordMessage> discordMessages = new List<DiscordMessage>();

            foreach (DiscordMessage discordMessage in discordReadOnlyMessageList)
            {
                discordMessages.Add(discordMessage);
            }

            discordMessages.Reverse();

            //Feeding the AI request the latest 20 messages
            foreach (DiscordMessage discordMessage in discordMessages)
            {
                if (string.IsNullOrEmpty(discordMessage.Content)) continue;

                DiscordUser currentUser;

                try
                {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    currentUser = Program.BotClient?.CurrentUser;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8604 // Possible null reference argument.
                    if (currentUser == null)
                    {
                        continue;
                    }
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                catch
                {
                    continue;
                }

                if (discordMessage.Author == currentUser)
                {
                    messages.Add(ChatMessage.FromAssistant(discordMessage.Content));
                }                                               //Robit ID
                else if (discordMessage.Author.Id.ToString() == "1049457745763500103")
                {
                    messages.Add(ChatMessage.FromUser($"{discordMessage.Author.Username}#{discordMessage.Author.Discriminator} | {discordMessage.Author.Id} : {discordMessage.Content}", discordMessage.Author.Username));
                }
                else if (!discordMessage.Author.IsBot)
                {
                    string userName = SpecialCharacterRemoval(discordMessage.Author.Username);

                    messages.Add(ChatMessage.FromUser($"{discordMessage.Author.Username}#{discordMessage.Author.Discriminator} | {discordMessage.Author.Id} : {discordMessage.Content}", userName));
                }

                if (Program.DebugStatus())
                {
                    using StreamWriter writer = new StreamWriter("debugconvo.txt", true);
                    writer.WriteLine($"{discordMessage.Author.Username}#{discordMessage.Author.Discriminator} | {discordMessage.Author.Id} : {discordMessage.Content}");
                }
            }

            //Makes the AI reply make more sense and lowers the chances of it answering to a wrong user
            messages.Add(ChatMessage.FromSystem($"Reply got triggered by user: {messageArgs.Author.Username}, tag: {messageArgs.Author.Discriminator}, userID: {messageArgs.Author.Id}"));

            if (Program.OpenAiService == null)
            {
                Program.BotClient?.Logger.LogError(AIEvent, "OpenAI service isn't on");

                return Tuple.Create<bool, string?, MemoryStream?>(false, "OpenAI service isn't on, if error presists contact RoboDoc", image);
            }

            //Sending OpenAI API request for chat reply
            ChatCompletionCreateResponse completionResult = await Program.OpenAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = Models.Gpt_4,
                N = 1,
                User = messageArgs.Author.Id.ToString(),
                Temperature = 1,
                FrequencyPenalty = 1.1F,
                PresencePenalty = 1,
                Functions = Functions.GetFunctions()
            });

            string? response;

            //If we get a proper result from OpenAI
            if (completionResult.Successful)
            {
                if (completionResult.Choices.Any())
                {
                    try
                    {
                        response = completionResult.Choices.Where(response => response.Message.Content != null)?.First().Message.Content;
                    }
                    catch
                    {
                        response = null;
                    }
                }
                else
                {
                    response = null;
                }

                FunctionCall? function = completionResult.Choices.First().Message.FunctionCall;

                bool imageCalled = false;

                if (messageArgs.Channel.IsNSFW && function?.Name == "get_lewd_image")
                {
                    image = await Functions.GetLewdImage(function?.ParseArguments().First().Value.ToString());
                    imageCalled = true;
                }

                if (image == null)
                {
                    Program.BotClient?.Logger.LogWarning(AIEvent, "Image is null");
                }

                if (string.IsNullOrEmpty(response) && image == null)
                {
                    return Tuple.Create<bool, string?, MemoryStream?>(false, "No message content", image);
                }

                if (!messageArgs.Channel.IsNSFW)
                {
                    if (AICheck(response).Result)
                    {
                        return Tuple.Create<bool, string?, MemoryStream?>(true, "**Filtered**", image);
                    }

                    Tuple<bool, string?> filter = Check(response);

                    if (filter.Item1)
                    {
                        return Tuple.Create<bool, string?, MemoryStream?>(true, "**Filtered**", image);
                    }
                }

                //Log the AI interaction only if we are in debug mode
                if (Program.DebugStatus())
                {
                    string? debugResponseLog = response;

                    if (imageCalled)
                    {
                        debugResponseLog += " newd.jpg";
                    }

                    Program.BotClient?.Logger.LogDebug(AIEvent, "Message: {message}", messageArgs.Message.Content);
                    Program.BotClient?.Logger.LogDebug(AIEvent, "Reply: {response}", response);
                }
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new NullReferenceException("OpenAI text generation failed with an unknown error");
                }

                Program.BotClient?.Logger.LogError(AIEvent, "{ErrorCode}: {ErrorMessage}", completionResult.Error.Code, completionResult.Error.Message);

                return Tuple.Create<bool, string?, MemoryStream?>
                    (false, $"OpenAI error {completionResult.Error.Code}: {completionResult.Error.Message}", image);
            }

            return Tuple.Create(true, response, image);
        }
    }
}
