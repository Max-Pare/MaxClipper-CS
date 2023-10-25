using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;
using System.Diagnostics;

namespace MaxClipper_C_
{
    public partial class DownloadForm : Form
    {
        public DownloadForm()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void confirmButton_Click(object sender, EventArgs e)
        {
            string ffmpegDir = ".\\FFmpeg";
            string handbrakeDir = ".\\HandBrake";
            string SevenZipEx = "./7-Zip/7z.exe";
            string handbrakeDownloadURL = "https://github.com/HandBrake/HandBrake/releases/download/1.6.1/HandBrakeCLI-1.6.1-win-x86_64.zip";
            string ffmpegDownloadURL = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z";
            using (var client = new WebClient())
            {
                int attemps = 0;
                while (true)
                {
                    if (attemps >= 5)
                    {
                        throw new Exception("Fatal exception: failed to download external resources.");
                    }
                    try
                    {
                        if (!File.Exists("_handbrake_.zip"))
                        {
                            Console.WriteLine("Started downloading Handbrake...");
                            client.DownloadFile(handbrakeDownloadURL, "_handbrake_.zip");
                            Console.WriteLine("Done.");
                        }
                        if (!File.Exists("_ffmpeg_essentials_.7z"))
                        {
                            Console.WriteLine("Started downloading FFmpeg...");
                            client.DownloadFile(ffmpegDownloadURL, "_ffmpeg_essentials_.7z");
                            Console.WriteLine("Done.");
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Error downloading resources.");
                        attemps++;
                        Thread.Sleep(3000);
                    }
                }
            }
            if (!Directory.Exists(ffmpegDir))
            {
                Directory.CreateDirectory(ffmpegDir);
                ProcessStartInfo pInfo = new ProcessStartInfo();
                pInfo.FileName = SevenZipEx;
                if (!File.Exists(SevenZipEx))
                {
                    throw new Exception("uuuuuuh");
                }
                pInfo.Arguments = $"x \".\\_ffmpeg_essentials_.7z\" -o\"{ffmpegDir}\\\" -r";
                pInfo.UseShellExecute = false;
                pInfo.RedirectStandardOutput = true;
                Process extrProc = new Process();
                extrProc.StartInfo = pInfo;
                extrProc.Start();
                extrProc.WaitForExit();
                foreach (string dir in Directory.GetDirectories(ffmpegDir))
                {
                    if (dir.Contains("ffmpeg"))
                    {
                        foreach(string subdir in Directory.GetDirectories(dir))
                        {
                            string dest = $"{ffmpegDir}\\{Path.GetFileNameWithoutExtension(subdir)}";
                            //Console.WriteLine(subdir + " ---> " + dest);
                            Directory.Move(subdir, dest);
                        }
                        foreach (string file in Directory.GetFiles(dir))
                        {
                            Console.WriteLine(file + " ---> " + ffmpegDir);
                            File.Move(file, $"{ffmpegDir}\\{Path.GetFileName(file)}");
                        }
                        break;
                    }
                }
            }

            if (!Directory.Exists(handbrakeDir))
            {
                Directory.CreateDirectory(handbrakeDir);
                ZipFile.ExtractToDirectory("_handbrake_.zip", handbrakeDir);
            }
            this.Close();
        }
    }
}
