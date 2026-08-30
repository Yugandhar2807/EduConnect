using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using EduConnect.Models;

namespace EduConnect.Services
{
    /// <summary>
    /// Renders an AI-scripted, narrated, animated explainer video for a topic:
    /// an animated cartoon presenter stands beside a whiteboard and "speaks" the
    /// narration (neural female voice via Edge TTS, offline SAPI fallback) while
    /// the board shows each scene's content. Frames are drawn with GDI+ and
    /// assembled with ffmpeg. Windows-only by design (runs on the IIS host).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class VideoGenerationService
    {
        private const int Width = 1280;
        private const int Height = 720;
        private const int Fps = 12;

        private readonly ILogger<VideoGenerationService> _logger;
        private readonly string _toolsDir;
        private readonly string _workRoot;
        private string _lastToolError = string.Empty;

        public VideoGenerationService(ILogger<VideoGenerationService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            // tools/ ships with the app (ffmpeg + node + tts script)
            _toolsDir = new[]
            {
                Path.Combine(env.ContentRootPath, "tools"),
                Path.Combine(AppContext.BaseDirectory, "tools"),
            }.FirstOrDefault(Directory.Exists) ?? Path.Combine(env.ContentRootPath, "tools");

            // Scratch space under App_Data: the IIS worker runs without a user profile,
            // so the system TEMP directory is not writable for it — App_Data is.
            _workRoot = Path.Combine(env.ContentRootPath, "App_Data", "tmp");
        }

        private string Ffmpeg => Path.Combine(_toolsDir, "ffmpeg.exe");
        private string Ffprobe => Path.Combine(_toolsDir, "ffprobe.exe");
        private string NodeExe => Path.Combine(_toolsDir, "node.exe");
        private string TtsScript => Path.Combine(_toolsDir, "tts", "tts.mjs");

        public bool ToolsAvailable => File.Exists(Ffmpeg) && File.Exists(Ffprobe);

        /// <summary>
        /// Generates the video and returns (relativeWebPath, fileSizeBytes), or null on failure.
        /// </summary>
        public async Task<(string RelativePath, long FileSize)?> GenerateAsync(VideoScriptData script, string webRootPath)
        {
            if (!ToolsAvailable)
            {
                _logger.LogError("Video tools missing in {ToolsDir} (ffmpeg/ffprobe).", _toolsDir);
                return null;
            }

            var workDir = Path.Combine(_workRoot, $"video_{Guid.NewGuid():N}");
            Directory.CreateDirectory(workDir);
            try
            {
                var segmentPaths = new List<string>();
                for (int i = 0; i < script.Slides.Count; i++)
                {
                    var slide = script.Slides[i];
                    var slideDir = Path.Combine(workDir, $"s{i}");
                    Directory.CreateDirectory(slideDir);

                    // 1. Narration audio (neural voice first, offline fallback second)
                    var audioPath = await SynthesizeNarrationAsync(slide.Narration, slideDir);
                    if (audioPath == null)
                        throw new InvalidOperationException($"Narration synthesis failed on slide {i + 1}. {_lastToolError}");
                    var duration = ProbeDurationSeconds(audioPath);
                    if (duration <= 0) duration = Math.Max(6, slide.Narration.Length / 14.0);

                    // 2. Animated frames for the scene
                    var frameCount = (int)Math.Ceiling((duration + 0.6) * Fps);
                    RenderSceneFrames(script, slide, i, frameCount, slideDir);

                    // 3. Encode the segment
                    var segment = Path.Combine(workDir, $"seg{i}.mp4");
                    var encode = RunTool(Ffmpeg,
                        $"-y -framerate {Fps} -i \"{Path.Combine(slideDir, "f_%04d.png")}\" -i \"{audioPath}\" " +
                        $"-c:v libx264 -preset veryfast -pix_fmt yuv420p -c:a aac -b:a 128k -shortest \"{segment}\"",
                        180_000);
                    if (encode != 0 || !File.Exists(segment))
                        throw new InvalidOperationException($"ffmpeg encode failed on slide {i + 1} (exit {encode}). {_lastToolError}");
                    segmentPaths.Add(segment);
                    Directory.Delete(slideDir, true); // free frame PNGs early
                }

                // 4. Concatenate all segments
                var listFile = Path.Combine(workDir, "list.txt");
                await File.WriteAllLinesAsync(listFile,
                    segmentPaths.Select(p => $"file '{p.Replace('\\', '/')}'"));
                var finalPath = Path.Combine(workDir, "final.mp4");
                var concat = RunTool(Ffmpeg, $"-y -f concat -safe 0 -i \"{listFile}\" -c copy \"{finalPath}\"", 120_000);
                if (concat != 0 || !File.Exists(finalPath))
                    throw new InvalidOperationException($"ffmpeg concat failed (exit {concat}). {_lastToolError}");

                // 5. Publish into wwwroot/uploads/materials
                var materialsDir = Path.Combine(webRootPath, "uploads", "materials");
                Directory.CreateDirectory(materialsDir);
                var safeTitle = string.Concat(script.Title.Where(c => char.IsLetterOrDigit(c) || c == ' '))
                    .Trim().Replace(' ', '_');
                if (safeTitle.Length > 60) safeTitle = safeTitle[..60];
                var fileName = $"{Guid.NewGuid():N}_{safeTitle}_video.mp4";
                var destination = Path.Combine(materialsDir, fileName);
                File.Copy(finalPath, destination, overwrite: true);

                return ($"/uploads/materials/{fileName}", new FileInfo(destination).Length);
            }
            finally
            {
                try { Directory.Delete(workDir, true); } catch { /* best effort cleanup */ }
            }
        }

        // ==================== narration ====================

        private async Task<string?> SynthesizeNarrationAsync(string narration, string slideDir)
        {
            // Primary: Edge neural voice (smooth female) — needs internet + node
            if (File.Exists(NodeExe) && File.Exists(TtsScript))
            {
                try
                {
                    var textFile = Path.Combine(slideDir, "narration.txt");
                    await File.WriteAllTextAsync(textFile, narration);
                    var outDir = Path.Combine(slideDir, "tts");
                    var exit = RunTool(NodeExe, $"\"{TtsScript}\" \"{textFile}\" \"{outDir}\" en-US-JennyNeural", 45_000);
                    var mp3 = Path.Combine(outDir, "audio.mp3");
                    if (exit == 0 && File.Exists(mp3) && new FileInfo(mp3).Length > 1000)
                        return mp3;
                    _logger.LogWarning("Neural TTS unavailable (exit {Exit}); falling back to offline voice.", exit);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Neural TTS failed; falling back to offline voice.");
                }
            }

            // Fallback: offline Windows female voice (Zira)
            try
            {
                var wav = Path.Combine(slideDir, "narration.wav");
                using var synth = new SpeechSynthesizer();
                try { synth.SelectVoiceByHints(VoiceGender.Female); } catch { /* keep default voice */ }
                synth.Rate = 0;
                synth.SetOutputToWaveFile(wav);
                synth.Speak(narration);
                synth.SetOutputToNull();
                return File.Exists(wav) && new FileInfo(wav).Length > 1000 ? wav : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Offline TTS failed");
                _lastToolError = $"Offline TTS: {ex.Message}";
                return null;
            }
        }

        private double ProbeDurationSeconds(string mediaPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Ffprobe,
                    Arguments = $"-v error -show_entries format=duration -of csv=p=0 \"{mediaPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = Process.Start(psi)!;
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(15_000);
                return double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
            }
            catch { return 0; }
        }

        private int RunTool(string exe, string args, int timeoutMs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _toolsDir,
            };
            // The IIS worker has no user profile; give child tools a writable TEMP.
            Directory.CreateDirectory(_workRoot);
            psi.Environment["TEMP"] = _workRoot;
            psi.Environment["TMP"] = _workRoot;
            using var process = Process.Start(psi)!;
            // Drain streams so big ffmpeg logs can't deadlock the pipe buffers.
            process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                _lastToolError = $"{Path.GetFileName(exe)} timed out after {timeoutMs / 1000}s.";
                return -1;
            }
            if (process.ExitCode != 0)
            {
                var stderr = stderrTask.IsCompletedSuccessfully ? stderrTask.Result : string.Empty;
                _lastToolError = $"{Path.GetFileName(exe)}: {stderr[^Math.Min(300, stderr.Length)..].Trim()}";
                _logger.LogWarning("{Exe} exited {Code}: {Err}", Path.GetFileName(exe), process.ExitCode, _lastToolError);
            }
            return process.ExitCode;
        }

        // ==================== scene drawing ====================

        private static readonly Color Indigo = Color.FromArgb(79, 70, 229);
        private static readonly Color DarkInk = Color.FromArgb(30, 27, 75);
        private static readonly Color Skin = Color.FromArgb(236, 188, 148);
        private static readonly Color Hair = Color.FromArgb(52, 38, 28);
        private static readonly Color Shirt = Color.FromArgb(67, 56, 202);
        private static readonly Color Trouser = Color.FromArgb(40, 44, 63);

        private void RenderSceneFrames(VideoScriptData script, VideoSlideData slide, int slideIndex, int frameCount, string outDir)
        {
            for (int f = 0; f < frameCount; f++)
            {
                var t = f / (double)Fps; // seconds into the scene
                using var bmp = new Bitmap(Width, Height);
                using var g = Graphics.FromImage(bmp);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                DrawBackground(g);
                DrawBoard(g, script, slide, slideIndex, t);
                DrawPresenter(g, t, talking: true);
                DrawFooter(g, script, slideIndex);

                bmp.Save(Path.Combine(outDir, $"f_{f:0000}.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static void DrawBackground(Graphics g)
        {
            using var sky = new LinearGradientBrush(new Rectangle(0, 0, Width, Height),
                Color.FromArgb(240, 242, 252), Color.FromArgb(219, 224, 248), 90f);
            g.FillRectangle(sky, 0, 0, Width, Height);
            using var floor = new SolidBrush(Color.FromArgb(203, 208, 236));
            g.FillRectangle(floor, 0, 640, Width, 80);
        }

        private static void DrawBoard(Graphics g, VideoScriptData script, VideoSlideData slide, int slideIndex, double t)
        {
            var board = new Rectangle(420, 70, 800, 520);
            using (var shadow = new SolidBrush(Color.FromArgb(40, 30, 27, 75)))
                g.FillRectangle(shadow, board.X + 8, board.Y + 10, board.Width, board.Height);
            using (var face = new SolidBrush(Color.White))
                g.FillRectangle(face, board);
            using (var frame = new Pen(Color.FromArgb(120, 100, 80), 10))
                g.DrawRectangle(frame, board);

            // Content fades in over the first half second of the scene.
            var alpha = (int)Math.Min(255, 255 * (t / 0.5));

            using var titleFont = new Font("Segoe UI", 30, FontStyle.Bold);
            using var bulletFont = new Font("Segoe UI", 22, FontStyle.Regular);
            using var ink = new SolidBrush(Color.FromArgb(alpha, DarkInk));
            using var accent = new SolidBrush(Color.FromArgb(alpha, Indigo));

            g.DrawString(slide.Title, titleFont, ink, new RectangleF(455, 100, 740, 100));
            g.FillRectangle(accent, 455, 185, 240, 6);

            var y = 225f;
            foreach (var bullet in slide.Bullets.Take(4))
            {
                // Each bullet pops in slightly after the previous one.
                var bulletAlpha = (int)Math.Clamp(255 * ((t - 0.4 - 0.25 * (y - 225) / 90) / 0.4), 0, 255);
                using var bulletInk = new SolidBrush(Color.FromArgb(bulletAlpha, DarkInk));
                using var dot = new SolidBrush(Color.FromArgb(bulletAlpha, Indigo));
                g.FillEllipse(dot, 465, y + 14, 14, 14);
                g.DrawString(bullet, bulletFont, bulletInk, new RectangleF(495, y, 700, 95));
                y += 95;
            }
        }

        private static void DrawPresenter(Graphics g, double t, bool talking)
        {
            // Idle bobbing + gesture cycle
            var bob = (float)(Math.Sin(t * 2.2) * 3);
            var cx = 210f;               // presenter horizontal centre
            var top = 200f + bob;        // top of the head

            // Blink for ~0.14s every ~3.1s
            var blink = (t % 3.1) < 0.14;
            // Mouth cycles between 3 shapes while talking (~7 changes/sec)
            var mouthState = talking ? (int)(t * 7) % 3 : 0;
            // Arm alternates between resting and pointing at the board every 2.6s
            var pointing = (int)(t / 2.6) % 2 == 0;

            using var skin = new SolidBrush(Skin);
            using var hair = new SolidBrush(Hair);
            using var shirt = new SolidBrush(Shirt);
            using var trouser = new SolidBrush(Trouser);
            using var ink = new Pen(DarkInk, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var limb = new Pen(Shirt, 22) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var hand = new SolidBrush(Skin);
            using var white = new SolidBrush(Color.White);
            using var dark = new SolidBrush(DarkInk);

            // Legs + shoes
            using (var leg = new Pen(Trouser, 26) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(leg, cx - 22, top + 265, cx - 26, top + 420);
                g.DrawLine(leg, cx + 22, top + 265, cx + 26, top + 420);
            }
            g.FillEllipse(dark, cx - 52, top + 412, 52, 22);
            g.FillEllipse(dark, cx + 2, top + 412, 52, 22);

            // Torso
            using (var torso = new GraphicsPath())
            {
                torso.AddPolygon(new[]
                {
                    new PointF(cx - 55, top + 115), new PointF(cx + 55, top + 115),
                    new PointF(cx + 48, top + 280), new PointF(cx - 48, top + 280),
                });
                g.FillPath(shirt, torso);
            }

            // Left arm (rests at the side)
            g.DrawLine(limb, cx - 48, top + 135, cx - 78, top + 225);
            g.FillEllipse(hand, cx - 92, top + 215, 26, 26);

            // Right arm (gestures toward the board)
            if (pointing)
            {
                g.DrawLine(limb, cx + 48, top + 135, cx + 135, top + 90);
                g.FillEllipse(hand, cx + 128, top + 74, 28, 28);
            }
            else
            {
                g.DrawLine(limb, cx + 48, top + 135, cx + 95, top + 215);
                g.FillEllipse(hand, cx + 88, top + 208, 26, 26);
            }

            // Head + hair + ears
            g.FillEllipse(skin, cx - 52, top, 104, 118);
            using (var hairPath = new GraphicsPath())
            {
                hairPath.AddArc(cx - 52, top - 4, 104, 90, 180, 180);
                hairPath.AddLine(cx + 52, top + 40, cx - 52, top + 40);
                g.FillPath(hair, hairPath);
            }
            g.FillEllipse(skin, cx - 62, top + 48, 18, 26);
            g.FillEllipse(skin, cx + 44, top + 48, 18, 26);

            // Eyes + brows
            if (blink)
            {
                g.DrawLine(ink, cx - 30, top + 58, cx - 12, top + 58);
                g.DrawLine(ink, cx + 12, top + 58, cx + 30, top + 58);
            }
            else
            {
                g.FillEllipse(white, cx - 32, top + 48, 22, 20);
                g.FillEllipse(white, cx + 10, top + 48, 22, 20);
                g.FillEllipse(dark, cx - 25, top + 53, 9, 10);
                g.FillEllipse(dark, cx + 17, top + 53, 9, 10);
            }
            g.DrawLine(ink, cx - 33, top + 40, cx - 10, top + 38);
            g.DrawLine(ink, cx + 10, top + 38, cx + 33, top + 40);

            // Nose
            g.DrawLine(ink, cx, top + 62, cx - 4, top + 78);

            // Mouth (talking animation)
            switch (mouthState)
            {
                case 0: g.DrawLine(ink, cx - 14, top + 94, cx + 14, top + 94); break;
                case 1: g.FillEllipse(dark, cx - 12, top + 88, 24, 14); break;
                default: g.FillEllipse(dark, cx - 14, top + 85, 28, 22); break;
            }

            // Collar detail
            g.DrawLine(ink, cx - 12, top + 118, cx, top + 132);
            g.DrawLine(ink, cx + 12, top + 118, cx, top + 132);
        }

        private static void DrawFooter(Graphics g, VideoScriptData script, int slideIndex)
        {
            using var bar = new SolidBrush(Color.FromArgb(235, DarkInk));
            g.FillRectangle(bar, 0, 660, Width, 60);
            using var brandFont = new Font("Segoe UI", 15, FontStyle.Bold);
            using var titleFont = new Font("Segoe UI", 14, FontStyle.Regular);
            using var white = new SolidBrush(Color.White);
            using var faint = new SolidBrush(Color.FromArgb(190, 199, 210, 254));
            g.DrawString("EduConnect", brandFont, white, 24, 676);
            g.DrawString(script.Title, titleFont, faint, 170, 678);

            // Scene progress dots
            for (int i = 0; i < script.Slides.Count; i++)
            {
                using var dot = new SolidBrush(i == slideIndex ? Color.White : Color.FromArgb(90, 255, 255, 255));
                g.FillEllipse(dot, Width - 40 - (script.Slides.Count - 1 - i) * 24, 682, 13, 13);
            }
        }
    }
}
