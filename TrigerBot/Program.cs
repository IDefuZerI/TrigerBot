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
            // Визначення висоти для перевірки зверху
            int searchHeight = 150;

            // Пошук зліва направо
            for (int x = 0; x < bitmap.Width; x += 3)
            {
                // Перевірка знизу вгору, але не більше ніж 150 пікселів від нижнього краю
                for (int y = bitmap.Height - 1; y >= Math.Max(0, bitmap.Height - searchHeight); y -= 3)
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
            Cursor.Position = point;
            MouseEvent(MouseEventFlags.LeftDown);
            MouseEvent(MouseEventFlags.LeftUp);
        }

        private static void MouseEvent(MouseEventFlags value)
        {
            mouse_event((uint)value, 0, 0, 0, IntPtr.Zero);
        }

        public static async Task StartBotAsync(CancellationToken token)
        {
            _running = true;
            int countdown = 280;

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
                    Console.WriteLine($"Час до завершення: {countdown} секунд");
                    countdown--;
                    await Task.Delay(70, token); // Асинхронна затримка
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка під час роботи бота: {ex.Message}");
                    break;
                }
            }

            // Безперервний пошук червоної кнопки після завершення зворотного відліку
            bool redButtonFound = false;
            while (!token.IsCancellationRequested && !redButtonFound)
            {
                try
                {
                    Bitmap finalScreen = CaptureScreen();
                    Point? redButtonLocation = FindRedButton(finalScreen);
                    if (redButtonLocation.HasValue)
                    {
                        MoveMouseTo(redButtonLocation.Value);
                        Console.WriteLine("Червона кнопка натиснута.");
                        redButtonFound = true;
                    }
                    else
                    {
                        Console.WriteLine("Червона кнопка не знайдена. Шукаю знову...");
                    }
                    await Task.Delay(1000, token); // Асинхронна затримка між спробами
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при знаходженні або натисканні червоної кнопки: {ex.Message}");
                }
            }

            // Перезапуск бота після натискання червоної кнопки
            if (!token.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
                _botTask = Task.Run(() => StartBotAsync(_cts.Token));
            }
            else
            {
                _running = false;
                Console.WriteLine("Бот зупинено.");
            }
        }

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                if (_running)
                {
                    _cts.Cancel();
                    _botTask.Wait();
                    Console.WriteLine("Бот зупинено через Ctrl+C.");
                }
                e.Cancel = true; // Запобігає завершенню програми при натисканні Ctrl+C
            };

            Console.WriteLine("Введіть '1', щоб запустити бота, або '0', щоб зупинити його. Натисніть Ctrl+C, щоб зупинити бота також.");

            while (true)
            {
                string input = Console.ReadLine();
                if (input.Equals("1", StringComparison.OrdinalIgnoreCase)) // Використовуйте '1' для запуску
                {
                    if (!_running)
                    {
                        _cts = new CancellationTokenSource();
                        _botTask = Task.Run(() => StartBotAsync(_cts.Token));
                        Console.WriteLine("Бот запущено.");
                    }
                    else
                    {
                        Console.WriteLine("Бот вже працює.");
                    }
                }
                else if (input.Equals("0", StringComparison.OrdinalIgnoreCase)) // Використовуйте '0' для зупинки
                {
                    if (_running)
                    {
                        _cts.Cancel();
                        _botTask.Wait();
                        _running = false;
                        Console.WriteLine("Бот зупинено.");
                    }
                    else
                    {
                        Console.WriteLine("Бот не працює.");
                    }
                }
                else
                {
                    Console.WriteLine("Невірна команда. Введіть '1', щоб запустити бота, або '0', щоб зупинити його.");
                }
            }
        }
    }
}
