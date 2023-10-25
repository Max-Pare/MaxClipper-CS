using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.Structure;
using MaxClipper_C_;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Windows.Forms;

// xcopy /Y /E /I "E:\\Documentoni 2\\Coding\\C#\\MaxClipper GUI\\MaxClipper C#\\Dependencies" "$(TargetDir)"
namespace MaxClipper //192; 200; 225
{

    public partial class MainWindow : Form
    {
        Dictionary<string, Dictionary<string, string>> ENCODER_PRESETS = new Dictionary<string, Dictionary<string, string>>
        {
            ["no re-encode"] = new Dictionary<string, string>
            {
                ["norencode"] = "true",
            },
            ["balanced"] = new Dictionary<string, string>
            {
                ["quality"] = "22",
                ["speed"] = "slow",
                ["fps"] = "same",
                ["gpu"] = "yes",
                ["res"] = "1920x1080"
            },
            ["fastest"] = new Dictionary<string, string>
            {
                ["quality"] = "24",
                ["speed"] = "fastest",
                ["fps"] = "30",
                ["gpu"] = "yes",
                ["res"] = "1920x1080"
            },
            ["small"] = new Dictionary<string, string>
            {
                ["quality"] = "26",
                ["speed"] = "slowest",
                ["fps"] = "30",
                ["gpu"] = "yes",
                ["res"] = "1280x720"
            },
            ["quality"] = new Dictionary<string, string>
            {
                ["quality"] = "18",
                ["speed"] = "slowest",
                ["fps"] = "same",
                ["gpu"] = "yes",
                ["res"] = "same"
            }
        };

        Dictionary<string, string> RESOLUTION_PRESETS = new Dictionary<string, string>
        {
            ["4k"] = "3840x2160",
            ["1440p"] = "2560x1440",
            ["1080p"] = "1920x1080",
            ["720p"] = "1280x720",
            ["480p"] = "640x480"
        };

        Dictionary<string, int> FPS_PRESETS = new Dictionary<string, int>
        {
            ["same"] = -1,
            ["60FPS"] = 60,
            ["30FPS"] = 30
        };

        Dictionary<string, string> CODEC_PRESET = new Dictionary<string, string>
        {
            ["High compatibility"] = "nvenc_h264",
            ["High quality"] = "nvenc_h265"
        };
        public static string currentDir = System.IO.Directory.GetCurrentDirectory();
        public static string ffmpegEx = ".\\FFmpeg\\bin\\ffmpeg.exe";
        public static string handbrakeEx = ".\\HandBrake\\HandBrakeCLI.exe";
        private DownloadForm downForm = new DownloadForm();

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
            segmentFrames[direction] = (float)(value * (1 / (double)videoObj.frameRate));
            var frameToShow = videoObj.GetFrame((int)Math.Floor(value));
            if (frameToShow == null) { return; }
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
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filePath.Text = openFileDialog1.FileName;
                videoDir = openFileDialog1.FileName;
            }
            if (videoDir == null || !videoDir.EndsWith(".mp4") || !File.Exists(videoDir))
            {
                LogToConsole("ERROR: file is not a valid video file (only mp4 accepted.)");
                return;
            }
            videoObj = new VideoTool(videoDir);
            hScrollBar1.Maximum = videoObj.frameCount;
            hScrollBar2.Maximum = videoObj.frameCount;
            hScrollBar2.Value = videoObj.frameCount;
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
            Console.WriteLine($"Segments before ffmpeg: {segmentFrames["start"]}, {segmentFrames["end"]}");
            string clippedVideo = VideoTool.FFmpegUtils.extract_clip(videoDir, segmentFrames, outputFolder);
            Console.WriteLine(clippedVideo);
            Console.WriteLine(outputFolder);
            if(clippedVideo == "__ERR__") { LogToConsole("Something went wrong while extracting the clip."); return; }
            if(encoderPresetsBox.SelectedItem.ToString() == "no re-encode")
            {
                LogToConsole("Skipping encoding, video has been saved!");
                return;
            }
            LogToConsole("Starting handbrake, this may take a while...");
            string finalOutput = VideoTool.HandBrakeCLI.Encode(
                    clippedVideo,
                    ENCODER_PRESETS[(string)encoderPresetsBox.SelectedItem],
                    (string)fpsBox.SelectedItem == "same" ? videoObj.frameRate : FPS_PRESETS[(string)fpsBox.SelectedItem],
                    (string)resolutionBox.SelectedItem == "same" ? videoObj.resolution : RESOLUTION_PRESETS[(string)resolutionBox.SelectedItem],
                    (string)CODEC_PRESET[(string)codecPickBox.SelectedItem]
                );
            LogToConsole("Done!");
            Process.Start("explorer.exe", "/select, \"" + finalOutput + "\"");
        }

        public string outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Videos";
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

        private void encoderPresets_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void resolutionBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void MainLoad(object sender, EventArgs e)
        {
            bool ffmpegInstalled = false;
            bool handbrakeInstalled = false;
            if (File.Exists(ffmpegEx))
            {
                // throw new FileNotFoundException("FFmpeg not found! It's supposed to be in the same directory as the executable (ProgramFolder\\FFmpeg\\bin\\ffmpeg.exe)");
                ffmpegInstalled = true;
            }
            if (File.Exists(handbrakeEx))
            {
                // throw new FileNotFoundException("HandBrakeCLI not found! It's supposed to be in the same directory as the executable (ProgramFolder\\HandBrake\\HandBrakeCLI.exe)");
                handbrakeInstalled = true;
            }
            if (!handbrakeInstalled || !ffmpegInstalled)
            {
                downForm.Show();
                downForm.Activate();
                downForm.TopMost = true;
            }
            LogToConsole("FFmpeg installed correctly!");
            LogToConsole("Handbrake installed correctly!");

            consoleBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            foreach (string key in ENCODER_PRESETS.Keys)
            {
                encoderPresetsBox.Items.Add(key);
            }
            encoderPresetsBox.SelectedItem = encoderPresetsBox.Items[encoderPresetsBox.Items.IndexOf("balanced")];
            foreach(string key in RESOLUTION_PRESETS.Keys)
            {
                resolutionBox.Items.Add(key);
            }
            resolutionBox.SelectedItem = resolutionBox.Items[2];
            foreach(string key in FPS_PRESETS.Keys)
            {
                fpsBox.Items.Add(key);
            }
            fpsBox.SelectedItem = fpsBox.Items[0];
            foreach(string key in CODEC_PRESET.Keys)
            {
                codecPickBox.Items.Add(key);
            }
            codecPickBox.SelectedItem = codecPickBox.Items[0];
        }

        class VideoTool
        {
            public VideoCapture video;
            public int frameCount;
            public int frameRate;
            public string videoName;
            public string resolution;
            private int lastFrame = 0;

            public VideoTool(string videoPath)
            {
                this.video = new VideoCapture(videoPath);
                this.frameCount = GetFrameCount();
                this.frameRate = GetFrameRate();
                this.videoName = Path.GetFileNameWithoutExtension(videoPath);
                this.resolution = GetResolution();
            }

            public Bitmap GetFrame(int frameIndex)
            {
                this.video.Set(Emgu.CV.CvEnum.CapProp.PosFrames, frameIndex);
                Mat matImage = video.QuerySmallFrame();
                if (matImage == null) { return null; }
                Bitmap frame = matImage.ToImage<Bgr, byte>().AsBitmap<Bgr, byte>();
                return frame;
            }

            public string GetResolution()
            {
                return $"{this.video.Width}x{this.video.Height}";
            }

            public int GetFrameCount()
            {
                return (int)this.video.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
            }

            public int GetFrameRate()
            {
                return (int)this.video.Get(Emgu.CV.CvEnum.CapProp.Fps);
            }

            public static class FFmpegUtils
            {
                public static string extract_clip(string videoDir, Dictionary<string, float> segment, string outDir)
                {
                    var start = segment["start"].ToString().Replace(",", ".");
                    var end = segment["end"].ToString().Replace(",", ".");
                    Console.WriteLine($"start: {start}, end: {end}");
                    int counter = 0;
                    string outFile = null;
                    while (File.Exists(outFile = $"{outDir}\\{Path.GetFileNameWithoutExtension(videoDir)}(clip {counter}).mp4"))
                    {
                        counter++;
                    }
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ffmpegEx,
                            Arguments = $"-ss {start} -i \"{videoDir}\" -c copy -t {end} \"{outFile}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    proc.Start();
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string line = proc.StandardOutput.ReadLine();
                        Console.WriteLine(line);

                    }
                    proc.WaitForExit();
                    if (!File.Exists(outFile))
                    {
                        return "__ERR__";
                    }
                    return outFile;
                }
            }

            public static class HandBrakeCLI
            {
                public static string Encode(string videoDir, Dictionary<string, string> args, int fpsOverride, string resolutionOverride, string encoder)
                {
                    Console.WriteLine($"Args: {fpsOverride}, {resolutionOverride}, {encoder}");
                    string _tempName = $"{Path.GetDirectoryName(videoDir)}\\{new Random().Next(100000, 999999)}.mp4";
                    try
                    {
                        File.Move(videoDir, _tempName);
                    } 
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        return "__ERR__";
                    }
                    string handbrakeArgs = $"-i \"{_tempName}\" -o \"{videoDir}\" -e {encoder} --encoder-preset {args["speed"]} --quality {args["quality"]} --rate {fpsOverride} --two-pass --cfr --width {resolutionOverride.Split('x')[0]} --height {resolutionOverride.Split('x')[1]}";
                    ProcessStartInfo pInfo = new ProcessStartInfo();
                    pInfo.Arguments = handbrakeArgs;
                    pInfo.FileName = handbrakeEx;
                    
                    Process handbrake = Process.Start(pInfo);
    
                    handbrake.WaitForExit();
                    if (!File.Exists(videoDir))
                    {
                        Console.WriteLine("Handbrake returned nothing, something went wrong.");
                        return "__ERR__";
                    }
                    File.Delete(_tempName);
                    return videoDir;
                }
            }
        }

        private void codecPickBox_SelectedIndexChanged(object sender, EventArgs e)
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



}
