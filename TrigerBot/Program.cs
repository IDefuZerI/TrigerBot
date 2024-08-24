using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrigerBot
{
    internal class Program
    {
        private static bool _running = false;
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static Task _botTask;

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

        [Flags]
        public enum MouseEventFlags : uint
        {
            LeftDown = 0x0002,
            LeftUp = 0x0004
        }

        public static Bitmap CaptureScreen()
        {
            Rectangle screenArea = new Rectangle(0, 0, 960, 1080); // Перша половина екрану 1920x1080
            Bitmap bitmap = new Bitmap(screenArea.Width, screenArea.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(screenArea.Location, Point.Empty, screenArea.Size);
            }
            return bitmap;
        }

        private static bool IsGreenStar(Color color)
        {
            return color.R >= 100 && color.R <= 160 &&
                   color.G >= 230 && color.G <= 270 &&
                   color.B >= 20 && color.B <= 40;
        }

        private static bool IsBlueStar(Color color)
        {
            return color.R >= 20 && color.R <= 50 &&
                   color.G >= 50 && color.G <= 120 &&
                   color.B >= 180 && color.B <= 255;
        }

        private static bool IsRedButton(Color color)
        {
            return color.R >= 190 && color.R <= 225 &&
                   color.G >= 80 && color.G <= 115 &&
                   color.B >= 90 && color.B <= 130;
        }

        public static Point? FindStar(Bitmap bitmap)
        {
            for (int x = 0; x < bitmap.Width; x += 3) // Перевірка кожного третього пікселя для пришвидшення
            {
                for (int y = 0; y < bitmap.Height; y += 3)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    if (IsGreenStar(pixelColor) || IsBlueStar(pixelColor))
                    {
                        return new Point(x, y);
                    }
                }
            }
            return null;
        }

        public static Point? FindRedButton(Bitmap bitmap)
        {
            for (int x = 0; x < bitmap.Width; x += 3) // Перевірка кожного третього пікселя для пришвидшення
            {
                for (int y = 0; y < bitmap.Height; y += 3)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    if (IsRedButton(pixelColor))
                    {
                        return new Point(x, y);
                    }
                }
            }
            return null;
        }

        public static void MoveMouseTo(Point point)
        {
            Cursor.Position = new Point(point.X, point.Y);
            MouseEvent(MouseEventFlags.LeftDown);
            MouseEvent(MouseEventFlags.LeftUp);
        }

        private static void MouseEvent(MouseEventFlags value)
        {
            mouse_event((uint)value, 0, 0, 0, IntPtr.Zero);
        }

        public static void StartBot(CancellationToken token)
        {
            _running = true;
            int countdown = 250;

            while (!token.IsCancellationRequested && countdown > 0)
            {
                try
                {
                    Bitmap screen = CaptureScreen();
                    Point? starLocation = FindStar(screen);
                    if (starLocation.HasValue)
                    {
                        MoveMouseTo(starLocation.Value);
                    }
                    Console.WriteLine($"Time remaining: {countdown} seconds");
                    countdown--;
                    Thread.Sleep(80); // Increase delay for debugging
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during bot operation: {ex.Message}");
                    break;
                }
            }

            // Find and click the red button after bot's task is complete
            try
            {
                Bitmap finalScreen = CaptureScreen();
                Point? redButtonLocation = FindRedButton(finalScreen);
                if (redButtonLocation.HasValue)
                {
                    MoveMouseTo(redButtonLocation.Value);
                    Console.WriteLine("Red button clicked.");
                }
                else
                {
                    Console.WriteLine("Red button not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding or clicking red button: {ex.Message}");
            }

            _running = false;
            Console.WriteLine("Bot stopped automatically after 40 seconds.");
        }

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                if (_running)
                {
                    _cts.Cancel();
                    _botTask.Wait();
                    Console.WriteLine("Bot stopped by Ctrl+C.");
                }
                e.Cancel = true; // Запобігає завершенню програми при натисканні Ctrl+C
            };

            Console.WriteLine("Type '1' to start the bot or '0' to stop it. Press Ctrl+C to stop the bot as well.");

            while (true)
            {
                string input = Console.ReadLine();
                if (input.Equals("1", StringComparison.OrdinalIgnoreCase)) // Використовуємо '1' для старту
                {
                    if (!_running)
                    {
                        _cts = new CancellationTokenSource();
                        _botTask = Task.Run(() => StartBot(_cts.Token));
                        Console.WriteLine("Bot started.");
                    }
                    else
                    {
                        Console.WriteLine("Bot is already running.");
                    }
                }
                else if (input.Equals("0", StringComparison.OrdinalIgnoreCase)) // Використовуємо '0' для зупинки
                {
                    if (_running)
                    {
                        _cts.Cancel();
                        _botTask.Wait();
                        Console.WriteLine("Bot stopped.");
                    }
                    else
                    {
                        Console.WriteLine("Bot is not running.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command. Type '1' to start the bot or '0' to stop it.");
                }
            }
        }
    }
}
