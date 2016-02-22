using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;

using DiscordSharp;
using DiscordSharp.Objects;
using FragLabs.Audio.Codecs;
using System.IO;
using System.Net;
using NAudio.Wave;
using MediaToolkit.Model;
using MediaToolkit;
using YoutubeExtractor;

using ChatterBotAPI;

/* StyroBot
* by Cody Carlson
*
* Custom DiscordBot written using DiscordSharp! That badass C# library was written by Luigifan here: https://github.com/Luigifan/DiscordSharp
**/

namespace DiscordBot
{
    class Program
    {
        static DiscordChannel mainText;
        static DiscordChannel quoteMuseum;
        static ChatterBotSession chatBot;
        static AudioPlayer audio;

        static void Main(string[] args)
        {
            ChatterBotFactory f = new ChatterBotFactory();
            ChatterBot bot = f.Create(ChatterBotType.CLEVERBOT);
            chatBot = bot.CreateSession();

            DiscordClient client = new DiscordClient();
            using (StreamReader sr = new StreamReader("userinfo.txt"))
            {
                client.ClientPrivateInformation = new DiscordUserInformation();
                client.ClientPrivateInformation.email = sr.ReadLine();
                client.ClientPrivateInformation.password = sr.ReadLine();
            }

            client.SendLoginRequest();
            Thread t = new Thread(client.Connect);
            t.Start();

            Console.ReadLine();
        }

        static Task ClientTask(DiscordClient client)
        {
            return Task.Run(() =>
            {
                Console.WriteLine("WELCOME TO DISCORD BOT" + "\n=============================");

                // voice messages
                client.MessageReceived += (sender, e) =>
                {
                    // Joining a voice channel
                    if (e.message.content.StartsWith("!joinvoice"))
                    {
                        string[] split = e.message.content.Split(new char[] { ' ' }, 2);
                        if (!String.IsNullOrEmpty(split[1]))
                        {
                            DiscordChannel toJoin = e.Channel.parent.channels.Find(x => (x.Name.ToLower() == split[1].ToLower()) && (x.Type == ChannelType.Voice));
                            if (toJoin != null)
                            {
                                DiscordVoiceConfig voiceCfg = new DiscordVoiceConfig() { Bitrate = null, Channels = 1, FrameLengthMs = 60, OpusMode = Discord.Audio.Opus.OpusApplication.LowLatency, SendOnly = false };
                                audio = new AudioPlayer(voiceCfg);
                                client.ConnectToVoiceChannel(toJoin);
                            }
                        }
                    }
                    else if (e.message.content.StartsWith("!addsong"))
                    {
                        string[] split = e.message.content.Split(new char[] { ' ' }, 3);

                        if (split.Count() >= 3 && !String.IsNullOrEmpty(split[1]) && !String.IsNullOrEmpty(split[2]))
                            DoVoiceURL(client.GetVoiceClient(), split[1], split[2]);
                        else client.SendMessageToChannel("Incorrect add song syntax.", e.Channel);
                    }
                    else if (e.message.content.StartsWith("!play"))
                    {
                        string[] split = e.message.content.Split(new char[] { ' ' }, 2);

                        if (File.Exists(split[1]))
                            DoVoiceMP3(client.GetVoiceClient(), split[1]);
                        else client.SendMessageToChannel("Song does not exist.", e.Channel);
                    }
                    else if (e.message.content.StartsWith("!disconnect"))
                    {
                        client.DisconnectFromVoice();
                    }
                };

                // other messages
                client.MessageReceived += (sender, e) =>
                {
                    Console.WriteLine($"[" + e.Channel.Name.ToString() +  "] " + e.message.author.Username + ": " + e.message.content.ToString());

                    if (e.Channel.Name == "thatbelikeitis" || e.Channel.Name == "newtestchannel")
                    {
                        mainText = e.Channel;
                        quoteMuseum = client.GetChannelByName("quotes");
                    }   

                    if (e.message.content.StartsWith("!help"))
                    {
                        string helpMsg = "Welcome to DiscordBot! The following commands are available:\n"
                        +                   "help, hello, joinvoice [channelname], play [mp3 path], addsong [youtube url] [song name], roll [XdX] [+/-] [mod]";
                        client.SendMessageToChannel(helpMsg, e.Channel);
                    }

                    // Text detection
                    if (e.message.content.Contains("Kappa") || e.message.content.Contains("kappa"))
                    {
                        client.SendMessageToChannel("We don't use that word here.", e.Channel);
                    }
                    else if (e.message.content.Contains("I'm back") || e.message.content.Contains("im back"))
                        client.SendMessageToChannel("I'm front", e.Channel);
                    else if (e.message.content.Contains("ryan") || e.message.content.Contains("Ryan")
                    || e.message.content.Contains("jimmy"))
                    {
                        client.AttachFile(e.Channel, "", "jimmyneukong.jpg");
                        //client.SendMessageToChannel("ryan", e.Channel);
                    }
                    else if (e.message.content.Contains("f14"))
                    {
                        client.AttachFile(e.Channel, "", "f14.jpg");
                    }

                    // Commands!
                    if (e.message.content.StartsWith("!hello"))
                        client.SendMessageToChannel("Hello World!", e.Channel);    
                    else if (e.message.content.StartsWith("!quote"))
                    {
                        string quote = GetRandomQuote(client);
                        client.SendMessageToChannel(quote, e.Channel);
                    }
                    else if (e.message.content.StartsWith("!addquote"))
                    {
                        char[] newQuote = e.message.content.Skip(9).ToArray();
                        client.SendMessageToChannel(new string(newQuote), quoteMuseum);
                    }
                    else if (e.message.content.StartsWith("!roll"))
                    {
                        string[] split = e.message.content.Split(new char[] { ' ' }, 4);

                        if (split.Count() >= 2 && !String.IsNullOrEmpty(split[1]))
                        {
                            string[] split2 = split[1].Split(new char[] { 'd' }, 2);

                            int roll = 0;
                            if (split.Count() >= 4 && !String.IsNullOrEmpty(split[2]) && !String.IsNullOrEmpty(split[3]))
                            {
                                if (split[2] == "+")
                                {
                                    roll = Roll(Int32.Parse(split2[0]), Int32.Parse(split2[1]), Int32.Parse(split[3]));
                                }
                                else if(split[2] == "-")
                                {
                                    roll = Roll(Int32.Parse(split2[0]), Int32.Parse(split2[1]), Int32.Parse(split[3]) * -1);
                                }  
                                else client.SendMessageToChannel("Can only mod by + or -! Result invalid.", e.Channel);                              
                            }
                            else
                            {
                                roll = Roll(Int32.Parse(split2[0]), Int32.Parse(split2[1]), 0);
                            }
                            string msg = split2[0] + "d" + split2[1] + ": " + roll;
                            client.SendMessageToChannel(msg, e.Channel);
                        }
                        else client.SendMessageToChannel("Missing arguments!", e.Channel);                       
                    }
                    else if (e.message.content.StartsWith("!chat"))
                    {
                        char[] chatMsg = e.message.content.Skip(6).ToArray();
                        string reply = chatBot.Think(new string(chatMsg));

                        Console.WriteLine(new string(chatMsg));
                        Console.WriteLine(reply);

                        client.SendMessageToChannel(reply, e.Channel);                     
                    }
                    else if (e.message.content.StartsWith("!supersecretshutdowncommand"))
                    {
                        System.Environment.Exit(0);
                    }
                };

                client.Connected += (sender, e) =>
                {
                    Console.WriteLine($"Connected! User: {e.user.Username}");
                };
                client.Connect();
            });
        }

        static Task DoVoiceURL(DiscordVoiceClient vc, string url, string name)
        {
            return Task.Run(() =>
            {
                Console.WriteLine("Beginning URL Play...");
                IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);

                VideoInfo video = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);
                
                if (video.RequiresDecryption)
                {
                    DownloadUrlResolver.DecryptDownloadUrl(video);
                }

                var videoDownloader = new VideoDownloader(video, Path.Combine("./downloads/", "temp" + name + video.VideoExtension));

                videoDownloader.DownloadProgressChanged += (sender, args) => Console.WriteLine(args.ProgressPercentage);
                videoDownloader.DownloadFinished += (sender, args) =>
                {
                    Console.WriteLine("Beginning conversion...");
                    var inputFile = new MediaFile { Filename = @"./downloads/temp" + name + video.VideoExtension };
                    var outputFile = new MediaFile { Filename = @"./music/" + name + ".mp3" };

                    using (var engine = new Engine())
                    {
                        Console.WriteLine("Converting...");
                        engine.Convert(inputFile, outputFile);
                        System.Threading.Thread.Sleep(2000);

                        Console.WriteLine("Done.");
                        File.Delete("downloads/temp" + name + video.VideoExtension);
                    }           
                };

                Console.WriteLine("Downloading youtube video...");
                videoDownloader.Execute();
            });
        }

        // Adapted from a DiscordSharp sample project
        static Task DoVoiceMP3(DiscordVoiceClient vc, string file)
        {
            return Task.Run(() =>
            {
                var bytes = audio.GetBytesFromMP3(file);
                audio.EnqueueBytes(bytes);
                audio.PlayAudio();
            });
        }

        static string GetRandomQuote(DiscordClient client)
        {
            List<DiscordMessage> quotes = client.GetMessageHistory(client.GetChannelByName("quotes"), 100, "", "");
            Random r = new Random();

            int i = r.Next(0, (quotes.Count - 1));

            string msg = quotes[i].content.ToString();

            return msg;
        }

        static int Roll(int numDice, int numSides, int mod)
        {
            Random r = new Random();
            int rollSum = 0;

            for (int i = 0; i < numDice; i++)
            {
                int x = r.Next(1, numSides);
                rollSum += x;
            }

            return rollSum + mod;
        }
    }
}
