using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;

namespace SeliwareLauncher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "Nucleus Launcher";
            if (!Directory.Exists(Program.miscDir))
            {
                Directory.CreateDirectory(Program.miscDir);
            }
            if (!File.Exists(Program.versionDir))
            {
                File.Create(Program.versionDir).Close();
            }
            if (!Directory.Exists(Program.cacheDir))
            {
                Directory.CreateDirectory(Program.cacheDir);
            }
            if ("sigma".Length == 3)
            {
                new Thread(new ThreadStart(Program.FunnyBeepStuff)).Start();
            }
            Program.FetchRobloxVersion();
            if (Program.IsRobloxWrong())
            {
                Console.WriteLine("Installing Roblox...");
                Program.DownloadAndExtractFiles("");
                string installedRobloxVersion = Program.GetInstalledRobloxVersion();
                File.WriteAllText(Program.versionDir, Program.robloxVersion);
                if (Program.IsRobloxWrong())
                {
                    File.WriteAllText(Program.versionDir, installedRobloxVersion);
                    Program.ThrowError("Failed to install Roblox. Please contact support team.");
                }
                Console.WriteLine("Installed!");
            }
            foreach (string text in Program.miscFiles.Keys)
            {
                File.WriteAllText(Program.robloxDir + "/" + text, Program.miscFiles[text]);
            }
            string[] array = args;
            if (array.Length != 1)
            {
                array = array.Append("-isInstallerLaunch").ToArray();
            }
            else if (array.Length == 1)
            {
                List<string[]> list = Program.parseGayArgs(array);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i][0] == "channel")
                    {
                        list[i][1] = "zlive";
                    }
                }
                array = Program.serializeIntoGayArgs(list);
            }
            try
            {
                Program.ReplaceInRegistry();
            }
            catch
            {
                Console.WriteLine("Registry replacement failed (Starting from browser will not work)");
                Thread.Sleep(1500);
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = AppContext.BaseDirectory + "\\roblox\\RobloxPlayerBeta.exe",
                WorkingDirectory = AppContext.BaseDirectory + "\\roblox",
                Arguments = string.Join(" ", array),
                UseShellExecute = true
            });
        }

        private static string[] serializeIntoGayArgs(List<string[]> args)
        {
            string[] array = new string[]
            {
                ""
            };
            foreach (string[] array2 in args)
            {
                string str = array2[0];
                string str2 = array2[1];
                string str3 = str + ":" + str2;
                if (array[0].Length != 0)
                {
                    str3 = "+" + str3;
                }
                string[] array3 = array;
                int num = 0;
                array3[num] += str3;
            }
            return array;
        }

        private static List<string[]> parseGayArgs(string[] gayArgs)
        {
            List<string[]> list = new List<string[]>();
            if (gayArgs.Length == 0 || gayArgs.Length > 1)
            {
                return list;
            }
            string[] array = gayArgs[0].Split(new char[]
            {
                '+'
            });
            for (int i = 0; i < array.Length; i++)
            {
                string[] array2 = array[i].Split(new char[]
                {
                    ':'
                });
                string text = array2[0];
                string text2 = array2[1];
                list.Add(new string[]
                {
                    text,
                    text2
                });
            }
            return list;
        }

        private static void FunnyBeepStuff()
        {
            Console.Beep(300, 200);
            Console.Beep(500, 200);
            Console.Beep(1000, 200);
        }

        private static void ThrowError(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
            Process.GetCurrentProcess().Kill();
        }

        private static bool IsRobloxWrong()
        {
            Console.WriteLine("Validating Roblox...");
            foreach (string str in Program.importantDirs)
            {
                string text = Program.robloxDir + "/" + str;
                if (!File.Exists(text) && !Directory.Exists(text))
                {
                    return true;
                }
            }
            return Program.robloxVersion != Program.GetInstalledRobloxVersion();
        }

		private static void FetchRobloxVersion()
		{
			Console.WriteLine("Fetching Roblox version...");
			using (WebClient client = new WebClient())
			{
				string json = client.DownloadString("https://nucleus.rip/info");
				int versionsIndex = json.IndexOf("\"Versions\"");
				if (versionsIndex != -1)
				{
					int robloxIndex = json.IndexOf("\"Roblox\"", versionsIndex);
					if (robloxIndex != -1)
					{
						int valueStart = json.IndexOf(':', robloxIndex) + 1;
						int valueEnd = json.IndexOfAny(new char[] { ',', '}' }, valueStart);
						if (valueEnd != -1)
						{
							Program.robloxVersion = json.Substring(valueStart, valueEnd - valueStart)
							.Trim().Trim('"');
						}
					}

				}
			}
		}

                

        private static string GetInstalledRobloxVersion()
		{
			return File.ReadAllText(Program.versionDir);
		}

        private static void DownloadAndExtractFiles(string version = "")
        {
            if (version == "")
            {
                version = Program.robloxVersion;
            }
            RobloxVersion robloxVersion = Program.ParseVersionData(version);
            Random random = new Random();
            
            using (WebClient client = new WebClient())
            {
                foreach (RobloxFile robloxFile in robloxVersion.Files)
                {
                    if (!(robloxFile.Name == "RobloxPlayerInstaller.exe"))
                    {
                        Console.WriteLine("Downloading {0}...", robloxFile.Name);
                        string str = random.Next(100000000, 999999999).ToString();
                        string text = Program.cacheDir + "/" + str;
                        try
                        {
                            client.DownloadFile($"https://setup.rbxcdn.com/{version}-{robloxFile.Name}", text);
                        }
                        catch
                        {
                            Console.WriteLine("Failed to download {0}!", robloxFile.Name);
                            continue;
                        }
                        if (!File.Exists(text))
                        {
                            Program.ThrowError(string.Format("Failed to download one of the dependencies: {0}", robloxFile.Name));
                        }
                        FastZip fastZip = new FastZip();
                        if (Enumerable.LastOrDefault<string>(robloxFile.Name.Split(new char[]
                        {
                            '.'
                        })) == "zip")
                        {
                            string[] array = robloxFile.Name.Split(new string[]
                            {
                                ".zip"
                            }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new string[]
                            {
                                "-"
                            }, StringSplitOptions.RemoveEmptyEntries);
                            string text2 = Program.robloxDir;
                            if (array.Length != 0 && robloxFile.Name != "RobloxApp.zip" && robloxFile.Name != "WebView2.zip" && robloxFile.Name != "WebView2RuntimeInstaller.zip" && robloxFile.Name != "content-platform-fonts.zip" && robloxFile.Name != "content-textures3.zip" && robloxFile.Name != "content-terrain.zip" && robloxFile.Name != "redist.zip")
                            {
                                int num = 0;
                                foreach (string text3 in array)
                                {
                                    string modifiedText3 = text3;
                                    if (modifiedText3 == "extracontent")
                                    {
                                        modifiedText3 = "ExtraContent";
                                    }
                                    if (modifiedText3 == "textures2")
                                    {
                                        modifiedText3 = "textures";
                                    }
                                    if (num++ != array.Length)
                                    {
                                        text2 = text2 + "/" + modifiedText3;
                                    }
                                }
                            }
                            else if (robloxFile.Name == "content-platform-fonts.zip")
                            {
                                text2 = Program.robloxDir + "/PlatformContent/pc/fonts";
                            }
                            else if (robloxFile.Name == "content-textures3.zip")
                            {
                                text2 = Program.robloxDir + "/PlatformContent/pc/textures";
                            }
                            else if (robloxFile.Name == "content-terrain.zip")
                            {
                                text2 = Program.robloxDir + "/PlatformContent/pc/terrain";
                            }
                            else if (robloxFile.Name == "WebView2RuntimeInstaller.zip")
                            {
                                text2 =Program.robloxDir + "/WebView2RuntimeInstaller";
                            }
                            fastZip.ExtractZip(text, text2, FastZip.Overwrite.Always, (string g) => true, null, null, false, false);
                            File.Delete(text);
                        }
                        else
                        {
                            string text4 = Program.robloxDir + "/" + robloxFile.Name;
                            if (File.Exists(text4))
                            {
                                File.Delete(text4);
                            }
                            File.Move(text, text4);
                        }
                    }
                }
            }
        }

        private static void ReplaceInRegistry()
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
            foreach (string text in registryKey.GetSubKeyNames())
            {
                if (text.Contains("_Classes"))
                {
                    RegistryKey registryKey2 = registryKey.OpenSubKey(text, true);
                    RegistryKey registryKey3 = registryKey2.OpenSubKey("roblox-player", true);
                    if (registryKey3 == null)
                    {
                        registryKey3 = registryKey2.CreateSubKey("roblox-player", true);
                    }
                    RegistryKey registryKey4 = registryKey3.OpenSubKey("shell", true);
                    if (registryKey4 == null)
                    {
                        registryKey4 = registryKey3.CreateSubKey("shell", true);
                    }
                    RegistryKey registryKey5 = registryKey4.OpenSubKey("open", true);
                    if (registryKey5 == null)
                    {
                        registryKey5 = registryKey4.CreateSubKey("open", true);
                    }
                    RegistryKey registryKey6 = registryKey5.OpenSubKey("command", true);
                    if (registryKey6 == null)
                    {
                        registryKey6 = registryKey5.CreateSubKey("command", true);
                    }
                    registryKey6.SetValue(null, string.Format("\"{0}\" %1", AppContext.BaseDirectory + "\\NucleusLauncher.exe"));
                }
            }
        }

        private static RobloxVersion ParseVersionData(string version = "")
        {
            Console.WriteLine("Fetching version data...");
            if (string.IsNullOrEmpty(version))
            {
                version = Program.robloxVersion;
            }
            
            RobloxVersion robloxVersion = new RobloxVersion
            {
                Hash = version
            };
            
            using (WebClient client = new WebClient())
            {
                string manifestUrl = $"https://setup.rbxcdn.com/{version}-rbxPkgManifest.txt";
                string manifestContent = client.DownloadString(manifestUrl);
                
                string[] lines = manifestContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length > 1)
                {
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string currentLine = lines[i].Trim();
                        if (string.IsNullOrEmpty(currentLine))
                            continue;
                            
                        if ((currentLine.EndsWith(".zip") || currentLine.EndsWith(".exe")) && i + 1 < lines.Length)
                        {
                            string nextLine = lines[i + 1].Trim();
                            if (!string.IsNullOrEmpty(nextLine))
                            {
                                robloxVersion.Files.Add(new RobloxFile
                                {
                                    Name = currentLine,
                                    Hash = nextLine
                                });
                                i++;
                            }
                        }
                    }
                }
            }
            
            return robloxVersion;
        }

        static Program()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary["AppSettings.xml"] = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<Settings>\r\n\t<ContentFolder>content</ContentFolder>\r\n\t<BaseUrl>http://www.roblox.com</BaseUrl>\r\n</Settings>\r\n";
            Program.miscFiles = dictionary;
        }

        private static string miscDir = AppContext.BaseDirectory + "\\misc";
        private static string versionDir = Program.miscDir + "\\version.txt";
        private static string cacheDir = Program.miscDir + "\\cache";
        private static string robloxDir = AppContext.BaseDirectory + "\\roblox";
        private static string robloxVersion = "";
        private static List<string> importantDirs = new List<string>
        {
            "RobloxPlayerBeta.exe",
            "RobloxCrashHandler.exe",
            "RobloxPlayerBeta.dll",
            "content",
            "ExtraContent",
            "shaders",
            "ssl",
            "PlatformContent"
        };
        private static Dictionary<string, string> miscFiles;

        private class JObject
        {
            internal static object Parse(string json)
            {
                throw new NotImplementedException();
            }
        }
    }

    internal class RobloxVersion
    {
        public string Hash { get; set; }
        public List<RobloxFile> Files { get; set; } = new List<RobloxFile>();
    }

    internal class RobloxFile
    {
        public string Name { get; set; }
        public string Hash { get; set; }
    }
}
