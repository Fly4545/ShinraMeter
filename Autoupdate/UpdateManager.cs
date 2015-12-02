﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Autoupdate
{
    public class UpdateManager
    {

        public static readonly string Version = "0.24";

        public static async Task<bool> Update()
        {
            var isUpToDate = await IsUpToDate().ConfigureAwait(false);
            if (isUpToDate) return false;
            Download();
            return true;
        }

        public static async Task<bool> IsUpToDate()
        {
            var latestVersion = await LatestVersion().ConfigureAwait(false);
            Console.WriteLine("Current version = "+Version);
            Console.WriteLine("Latest version = "+latestVersion);
            return latestVersion == Version;
        }

        private static void Decompress(string latestVersion)
        {
            // Get the stream of the source file.
            ZipFile.ExtractToDirectory(ExecutableDirectory+ @"\tmp\" +latestVersion, ExecutableDirectory + @"\tmp\");
            ZipFile.ExtractToDirectory(ExecutableDirectory + @"\tmp\" + latestVersion, ExecutableDirectory + @"\tmp\release\");
        }


        private static void Download()
        {
            DestroyDownloadDirectory();
            Directory.CreateDirectory(ExecutableDirectory + @"\tmp\release\");

            
            var latestVersion = "ShinraMeterV"+ LatestVersion().Result;
            Console.WriteLine("Downloading latest version");
            SetCertificate();
            using (var client = new WebClient())
            {
                client.DownloadFile("https://cloud.neowutran.ovh/index.php/s/e7arRRxkHEIkzU1/download?path=%2F&files=" + latestVersion+".zip", ExecutableDirectory+@"\tmp\" +latestVersion+".zip");
            }
            Console.WriteLine("Latest version downloaded");
            Console.WriteLine("Decompressing");
            Decompress(latestVersion + ".zip");
            Console.WriteLine("Decompressed");
            Process.Start("Explorer.exe", ExecutableDirectory + @"\tmp\" + latestVersion+ @"\Autoupdate.exe");
            Console.WriteLine("Start upgrading");
        }

        public static string ExecutableDirectory => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static string ResourcesDirectory
        {
            get
            {
                var directory = Path.GetDirectoryName(typeof(UpdateManager).Assembly.Location);
                while (directory != null)
                {
                    var resourceDirectory = Path.Combine(directory, @"resources\");
                    if (Directory.Exists(resourceDirectory))
                        return resourceDirectory;
                    directory = Path.GetDirectoryName(directory);
                }
                throw new InvalidOperationException("Could not find the resource directory");
            }
        }

        private static void DestroyDownloadDirectory()
        {
            if (!Directory.Exists(ExecutableDirectory + @"\tmp\")) return;
            Directory.Delete(ExecutableDirectory+@"\tmp\", true);
        }

        public static void Copy(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)),true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
            }
        }

        public static void DestroyRelease()
        {
        
            if (!Directory.Exists(ExecutableDirectory + @"\..\..\resources\")) return;
            Directory.Delete(ExecutableDirectory + @"\..\..\resources\", true);
            Console.WriteLine("Resources directory destroyed");
        }


        private static async Task<string> LatestVersion()
        {
            
            var version = await GetResponseText("https://cloud.neowutran.ovh/index.php/s/muOLoJjP8JJfqFR/download").ConfigureAwait(false);
            version = Regex.Replace(version, @"\r\n?|\n", "");
            return version;
        }

        private static void SetCertificate()
        {
            var cloudCertificate = new X509Certificate2(ResourcesDirectory + @"cloud.neowutran.ovh.der");
            ServicePointManager.ServerCertificateValidationCallback =
            (sender, certificate, chain, sslPolicyErrors) => sslPolicyErrors == SslPolicyErrors.None || certificate.Equals(cloudCertificate);
        }

        private static async Task<string> GetResponseText(string address)
        {
            SetCertificate();
            using (var client = new HttpClient())
            {
                return await client.GetStringAsync(new Uri(address));
            }

        }
    }
}
