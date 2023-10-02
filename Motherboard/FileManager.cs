using System.Reflection;
using System.Text.Json;

namespace Motherboard
{
    /// <summary>
    /// Class responsible for managment of files
    /// </summary>
    internal static class FileManager
    {
        /// <summary>
        /// Paths to directories that the bot uses to store different kinds of data
        /// </summary>
        internal readonly struct Paths
        {
            internal static readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;
            internal static readonly string channelSettings = $@"{basePath}/ChannelSettings";
        }

        /// <summary>
        /// Checks that the directory exists
        /// </summary>
        /// <returns>
        /// A list of all directories created
        /// </returns>
        internal static async Task<List<string>> DirCheck()
        {
            List<string> list = new List<string>();

            await Task.Run(() =>
            {
                Paths paths = new Paths();

                foreach (FieldInfo field in typeof(Paths).GetFields())
                {
                    string? path = field.GetValue(paths)?.ToString();

                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    DirectoryInfo directoryInfo = new DirectoryInfo(path);

                    if (!directoryInfo.Exists)
                    {
                        directoryInfo.Create();
                        list.Add(field.Name);
                    }
                }
            });

            return list;
        }

        /// <summary>
        /// Checks if file exists
        /// </summary>
        /// <param name="fileDir">File location</param>
        /// <returns>
        /// <list type="table">
        /// <item><c>True</c>: File exists</item>
        /// <item><c>False</c>: File doesn't exist</item>
        /// </list>
        /// </returns>
        private static bool FileExists(string fileDir)
        {
            FileInfo fileInfo = new FileInfo(fileDir);

            if (!fileInfo.Exists)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a file
        /// </summary>
        /// <param name="fileDir">Location to create the file at</param>
        private static void CreateFile(string fileDir)
        {
            FileInfo fileInfo = new FileInfo(fileDir);

            fileInfo.Create().Dispose();
        }

        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <returns>
        /// <list type="table">
        /// <item>True: Directory exists</item>
        /// <item>False: Directory doesn't exists</item>
        /// </list>
        /// </returns>
        internal static bool DirectoryExists(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            if (!directoryInfo.Exists)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a directory
        /// </summary>
        /// <param name="path">Path of the directory to create</param>
        internal static void CreateDirectory(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            directoryInfo.Create();
        }

        /// <summary>
        /// Sets of methods to manage channel settings
        /// </summary>
        internal static class ChannelManager
        {
            /// <summary>
            /// Channel specific settings
            /// </summary>
            internal struct Channel
            {
                internal bool AIIgnore { get; set; }
            }

            /// <summary>
            /// Converts given guildID and channelID to guild folder path and channel settings json path
            /// </summary>
            /// <param name="guildID">ID of the guild</param>
            /// <param name="channelID">ID of the channel</param>
            /// <returns>
            /// A tuple that contains two strings
            /// <list type="table">
            /// <item>Item 1: A path to the guild directory</item>
            /// <item>Item 2: A path to the channel json file</item>
            /// </list>
            /// </returns>
            private static Tuple<string, string> IDToPath(string guildID, string channelID)
            {
                string guildPath = $"{Paths.channelSettings}/{guildID}";
                string channelPath = $"{guildPath}/{channelID}.json";

                return Tuple.Create(guildPath, channelPath);
            }

            /// <summary>
            /// Reads the channel settings information
            /// </summary>
            /// <param name="guildID">ID of the guild the channel is in</param>
            /// <param name="channelID">ID of the channel</param>
            /// <returns>
            /// A channel struct
            /// </returns>
            internal static Channel ReadChannelInfo(string guildID, string channelID)
            {
                Tuple<string, string> paths = IDToPath(guildID, channelID);

                string guildPath = paths.Item1;
                string channelPath = paths.Item2;

                if (!DirectoryExists(guildPath))
                {
                    CreateDirectory(guildPath);
                }

                if (!FileExists(channelPath))
                {
                    CreateFile(channelPath);
                }

                string jsonString = File.ReadAllText(channelPath);

                if (string.IsNullOrEmpty(jsonString))
                {
                    Channel newChannel = new Channel()
                    {
                        AIIgnore = false,
                    };

                    WriteChannelInfo(newChannel, guildID, channelID, true);

                    return newChannel;
                }

                return JsonSerializer.Deserialize<Channel>(jsonString);
            }

            /// <summary>
            /// Writes channel settings information
            /// </summary>
            /// <param name="channel">Channel struct containing the settings you want to write</param>
            /// <param name="guildID">The ID of the guild the channel belongs to</param>
            /// <param name="channelID">The ID of the channel</param>
            /// <param name="overwrite">To overwrite the existing settings(if they exist) or not (default is false)</param>
            /// <exception cref="Exception">If channel settings exist, but overwrite is set to false</exception>
            internal static void WriteChannelInfo(Channel channel, string guildID, string channelID, bool overwrite = false)
            {
                Tuple<string, string> paths = IDToPath(guildID, channelID);

                string guildPath = paths.Item1;
                string channelPath = paths.Item2;

                if (!DirectoryExists(guildPath))
                {
                    CreateDirectory(guildPath);
                }

                if (FileExists(channelPath))
                {
                    if (overwrite)
                    {
                        FileInfo fileInfo = new FileInfo(channelPath);

                        fileInfo.Delete();
                        fileInfo.Create().Dispose();
                    }
                    else
                    {
                        throw new Exception("Channel settings entry already exists");
                    }
                }

                FileStream fileStream = File.OpenWrite(channelPath);

                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                JsonSerializer.Serialize(fileStream, channel, jsonSerializerOptions);

                fileStream.Close();
            }
        }
    }
}
