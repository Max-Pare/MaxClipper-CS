using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Windows.Forms.VisualStyles;
using System.Runtime.InteropServices.ComTypes;
using MaxClipper;
using Emgu.CV;
using System.Text.RegularExpressions;
using Emgu.CV.Structure;
using Emgu.CV.XImgproc;
using System.Reflection;
using System.Drawing.Imaging;
using System.CodeDom;
using Microsoft.WindowsAPICodePack.Dialogs;
using Emgu.CV.VideoStab;

namespace MaxClipper //192; 200; 225
{

    public partial class MainWindow : Form
    {

        public string currentDir = System.IO.Directory.GetCurrentDirectory();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        Dictionary<string, float> segmentFrames = new Dictionary<string, float>();
        private void handleScroll(float value, string direction)
        {
            if (videoObj == null) { return; }
            segmentFrames[direction] = (float)(value * (1 / videoObj.frameRate));
            //float remappedVal = value.Remap(hScrollBar1.Minimum, hScrollBar1.Maximum, 0, 1);
            //int frameToGrab = (int)Math.Floor(videoObj.frameCount * remappedVal);
            var frameToShow = videoObj.GetFrame((int)Math.Floor(value));
            if(frameToShow == null) { return; }
            pictureBox1.Image = frameToShow;
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            handleScroll(hScrollBar1.Value, "start");

        }

        private void hScrollBar2_Scroll(object sender, EventArgs e)
        {
            handleScroll(hScrollBar2.Value, "end");
        }
        string videoDir = null;

        private void explorerButton(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filePath.Text = openFileDialog1.FileName;
                videoDir = openFileDialog1.FileName;
            }
        }

        private void openFileDialog1_FileOk(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void filePathLabel(object sender, EventArgs e)
        {

        }
        VideoTool videoObj = null;
        private void openFileButton(object sender, EventArgs e)
        {
            if (videoDir == null || !videoDir.EndsWith(".mp4") || !File.Exists(videoDir))
            {
                LogToConsole("ERROR: file is not a valid video file (only mp4 accepted.)");
                return;
            }
            videoObj = new VideoTool(videoDir);
            hScrollBar1.Maximum = videoObj.frameCount;
            hScrollBar2.Maximum = videoObj.frameCount;
            segmentFrames["start"] = 0;
            segmentFrames["end"] = (float)(videoObj.frameCount * videoObj.frameRate);
            LogToConsole("Frame count: " + videoObj.frameCount.ToString());
            LogToConsole("Framerate: " + videoObj.frameRate.ToString());
            pictureBox1.Image = videoObj.GetFrame(0);
        }

        int MaxLines = 18;
        public void LogToConsole(string message)
        {
            consoleBox.AppendText(message + Environment.NewLine);
            if (consoleBox.Lines.Length > MaxLines)
            {
                consoleBox.Lines = consoleBox.Lines.Skip(1).ToArray();
            }
            consoleBox.SelectionStart = consoleBox.Text.Length;
            consoleBox.ScrollToCaret();
        }

        private void consoleLogger(object sender, EventArgs e)
        {

        }

        public void InvokeConsoleLog(string message) //used to use console box from thread other than main
        {
            consoleBox.Invoke(new Action(() => LogToConsole(message)));
        }

        private void MainLoad(object sender, EventArgs e)
        {
            consoleBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void clipButton(object sender, EventArgs e)
        {
            if (videoObj == null)
            {
                LogToConsole("No video loaded!");
                return;
            }
            if (segmentFrames == null)
            {
                LogToConsole("Internal error: segmentFrames is null. Try moving both scroll bars again.");
                return;
            }
            LogToConsole(segmentFrames["start"].ToString() + " " +  segmentFrames["end"].ToString());
            string clippedVideo = VideoTool.FFmpegUtils.extract_clip(videoDir, segmentFrames);
        }
        public string outputFolder = "./";
        private void setOutputFolder_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                LogToConsole("Selected output directory: \"" + dialog.FileName + "\"");
                outputFolder = dialog.FileName;
            }
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }
    }

    public static class ExtensionMethods
    {

        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

    }


    class VideoTool
    {
        public VideoCapture video;
        public int frameCount;
        public double frameRate;
        public string videoName;
        private int lastFrame = 0;
        public VideoTool(string videoPath)
        {
            this.video = new VideoCapture(videoPath);
            this.frameCount = GetFrameCount();
            this.frameRate = GetFrameRate();
            this.videoName = Path.GetFileNameWithoutExtension(videoPath);
        }

        public Bitmap GetFrame(int frameIndex)
        {
            this.video.Set(Emgu.CV.CvEnum.CapProp.PosFrames, frameIndex);
            Mat matImage = video.QuerySmallFrame();
            if(matImage == null) { return null; }
            Bitmap frame = matImage.ToImage<Bgr, byte>().AsBitmap<Bgr, byte>();
            return frame;
        }


        public int GetFrameCount()
        {
            return (int)this.video.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
        }

        public double GetFrameRate()
        {
            return this.video.Get(Emgu.CV.CvEnum.CapProp.Fps);
        }

        [Obsolete("this is catastrophyically bad and unbelievably slow, do not use.", true)]
        class VideoCache
        {
            private Dictionary<int, string> cache = new Dictionary<int, string>();
            private string cachePath = null;
            public VideoCache(VideoCapture videocap, int totalframes, string name)
            {
                cachePath = $".\\cache\\{name}_{new Random().Next(0, 1000000)}";
                if (!File.Exists(cachePath))
                {
                    Directory.CreateDirectory($"{cachePath}");
                }
                for (int i = 0; i < totalframes; i++)
                {
                    if (!videocap.Set(Emgu.CV.CvEnum.CapProp.PosFrames, i)) { continue; }
                    Mat frame = videocap.QueryFrame();
                    if (frame == null) { continue; }
                    Console.WriteLine($"{cachePath}/{name}_{i}.jpeg");
                    Emgu.CV.CvInvoke.Resize(frame, frame, new Size(768, 480), 0, 0, Emgu.CV.CvEnum.Inter.Linear);
                    frame.Save($"{cachePath}\\{name}_{i}.jpeg");
                }
            }

        }

        public static class FFmpegUtils
        {
            public static string extract_clip(string videoDir, Dictionary<string, float> segment)
            {
                var start = segment["start"].ToString().Replace(",", ".");
                var end = segment["end"].ToString().Replace(",", ".");
                int counter = 0;
                string outDir = null;
                while (File.Exists(outDir = $"{Path.GetPathRoot(videoDir)}\\{Path.GetFileNameWithoutExtension(videoDir)}(clip {counter}).mp4"))
                {
                    counter++;
                }
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-ss {start} -i \"{videoDir}\" -c copy -t {end} \"{outDir}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                List<string> output = new List<string>();
                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    output.Add(line);
                    // do something with line
                }
                proc.WaitForExit();
                foreach(string line in output)
                {
                    Console.WriteLine(line);
                }
                return outDir;
            }
        }
    }
}
