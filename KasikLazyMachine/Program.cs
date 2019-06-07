using System;
using System.Net;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;

namespace KasikLazyMachine
{
    class Program
    {
        static class Urls
        {
            const string UrlBase = "https://www.deltaconnected.com/arcdps/x64/";
            public const string arcdps_ini = UrlBase + @"arcdps.ini";
            public const string d3d9 = UrlBase + @"d3d9.dll";
            public const string d3d9_md5sum = UrlBase + @"d3d9.dll.md5sum";
            public const string d3d9_arcdps_buildtemplates = UrlBase + @"buildtemplates/d3d9_arcdps_buildtemplates.dll";
            public const string d3d9_arcdps_extras = UrlBase + @"extras/d3d9_arcdps_extras.dll";
        }

        static byte[] TryGetFileMD5(string fileName)
        {
            if (!File.Exists(fileName)) return null;
            try
            {
                using (var fs  = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var md5 = MD5.Create())
                {
                    return md5.ComputeHash(fs);
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        static bool AreEqual(byte[] a, byte[] b)
        {
            if (a == null) return b == null;
            if (b == null) return false;
            if (a.Length != b.Length) return false;
            for(int i = 0; i < a.Length; ++i)
            {
                if(a[i] != b[i]) return false;
            }
            return true;
        }

        static byte FromHex(char value)
        {
            if (value >= '0' && value <= '9') return (byte)(value - '0');
            if (value >= 'a' && value <= 'f') return (byte)(10 + (value - 'a'));
            if (value >= 'A' && value <= 'F') return (byte)(10 + (value - 'A'));
            return 255;
        }

        static byte[] TryParseMD5(string value)
        {
            byte[] result = new byte[16];
            for(int i = 0; i < value.Length; i += 2)
            {
                var v1 = FromHex(value[i + 0]);
                var v2 = FromHex(value[i + 1]);
                if (v1 == 255 || v2 == 255)
                {
                    return null;
                }
                result[i / 2] = (byte)((v1 << 4) | v2);
            }
            return result;
        }

        static byte[] TryDownloadLatestMD5(WebClient client)
        {
            var result = client.DownloadString(Urls.d3d9_md5sum);
            if (result.Length < 16 * 2)
            {
                return null;
            }
            result = result.Substring(0, 16 * 2);
            return TryParseMD5(result);
        }

        static bool IsTrue(string value)
        {
            return value == "1"
                || StringComparer.OrdinalIgnoreCase.Equals("true", value)
                || StringComparer.OrdinalIgnoreCase.Equals("t", value)
                || StringComparer.OrdinalIgnoreCase.Equals("yes", value)
                || StringComparer.OrdinalIgnoreCase.Equals("y", value);
        }

        static void Uninstall(string gw2dir)
        {
            var fileName = Path.Combine(gw2dir, @"d3d9.dll");
            if (!File.Exists(fileName))
            {
                Console.WriteLine("You don't have ArcDPS installed.");
                return;
            }
            try
            {
                File.Delete(fileName);
            }
            catch
            {
                Console.WriteLine("Failed to uninstall ArcDPS.");
                return;
            }
            Console.WriteLine("ArcDPS uninstalled successfully.");
        }

        static void WaitForUserInput()
        {
            Console.WriteLine("\nPress any key to close this window."); 
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            var gw2dir = ConfigurationManager.AppSettings["gw2dir"];

            if (string.IsNullOrWhiteSpace(gw2dir) || !Directory.Exists(gw2dir))
            {
                Console.WriteLine("Make sure the .xml included with this program has the right address to your GW2\ninstallation folder.");
                return;
            }

            if (Array.IndexOf(args, "--uninstall") >= 0)
            {
                Uninstall(gw2dir);
                WaitForUserInput();
                return;
            }

            Console.WriteLine("Welcome to the Kasik Lazy Machine, this program will download Arcdps into your\ncomputer."
                +$" The current download location is at:\n{ gw2dir }\n"+
                "If you wish to change this please edit the .xml file included with this program.");

            using (var webClient = new WebClient())
            {
                if (Array.IndexOf(args, "--force") < 0)
                {
                    var localMD5 = TryGetFileMD5(Path.Combine(gw2dir, @"d3d9.dll"));
                    if (localMD5 != null || Array.IndexOf(args, "--force") >= 0)
                    {
                        var remoteMD5 = TryDownloadLatestMD5(webClient);
                        if (AreEqual(localMD5, remoteMD5))
                        {
                            Console.WriteLine("You have latest ArcDPS installed already.");
                            WaitForUserInput();
                            return;
                        }
                    }
                }

                try
                {
                    Console.WriteLine("Now connecting to deltaconnected.com and trying to retrieve arcdps...\n");
                    webClient.DownloadFile(Urls.arcdps_ini, Path.Combine(gw2dir, @"arcdps.ini"));
                    webClient.DownloadFile(Urls.d3d9, Path.Combine(gw2dir, @"d3d9.dll"));
                    if (IsTrue(ConfigurationManager.AppSettings["install_buildtemplates"]))
                    {
                        webClient.DownloadFile(Urls.d3d9_arcdps_buildtemplates, Path.Combine(gw2dir, @"d3d9_arcdps_buildtemplates.dll"));
                    }
                    if (IsTrue(ConfigurationManager.AppSettings["install_extras"]))
                    {
                        webClient.DownloadFile(Urls.d3d9_arcdps_extras, Path.Combine(gw2dir, @"d3d9_arcdps_extras.dll"));
                    }
                    Console.WriteLine("\n\nArcdps has been downloaded. Thanks for using the Kasik Lazy Machine.\n" +
                        "If you have any questions about this program \nfeel free to message me ingame to Bruno.8937");
                }
                catch (WebException ex)
                {
                    Console.WriteLine("\n\nThe program has encountered an error. (1)\n\n" +
                        "This error can occur if the specified Guild Wars 2 folder doesn't exist.\n" +
                        "Make sure the .xml included with this program has the right address to your GW2\ninstallation folder." +
                        "\nFeel free to message me ingame to Bruno.8937\nif you're still having issues!\n\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n\nThe program has encountered an error. (2)" +
                        "\nI'm not sure what went wrong here, so..." +
                        "\nIf the following message doesn't give you enough information\nas to what went wrong" +
                        $"\nfeel free to message me ingame to Bruno.8937\n\n{ex.Message}");
                }
            }
            WaitForUserInput();
        }
    }
}
