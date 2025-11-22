using System.Diagnostics;

namespace EnterTheMatrix
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            int width = Console.WindowWidth, height = Console.WindowHeight;

            bool showFPS = false;
            // Buffer for the displayed fps etc.
            // The length should be enough for at least "Frames: 1000000000\r\nFPS: 1000000000\r\nDelay: 1000000000ms".
            Span<char> statsTextBuffer = stackalloc char[64];
            int delay = 60;
            int totalFrames = 0;
            int lastFrames = 0;
            float fps = 0;

            var backBuffer = new ConsoleBackBuffer(width, height);
            var rainDrops = InitializeRainDrops(width, height);

            var running = true;

            var keyListener = new KeyListener();

            // No longer needed since we have a KeyListener
            // Handle CTRL+C ourselves
            //Console.CancelKeyPress += (sender, eventArgs) =>
            //{
            //    running = false;
            //    keyListener.Stop();
            //    // Prevent the process from exiting
            //    eventArgs.Cancel = true;
            //};

            keyListener.Start();

            keyListener.KeyPressed += (sender, e) =>
            {
                switch (e.Key)
                {
                    case ConsoleKey.F:
                        showFPS = !showFPS;
                        break;
                    case ConsoleKey.OemMinus when delay == 0:
                        return;
                    case ConsoleKey.OemMinus:
                        delay--;
                        break;
                    case ConsoleKey.OemPlus when delay == 500:
                        return;
                    case ConsoleKey.OemPlus:
                        delay++;
                        break;
                    // Handle ESC and CTRL-C
                    case ConsoleKey.Escape:
                    case ConsoleKey.C when e.Modifiers == ConsoleModifiers.Control:
                        running = false;
                        break;
                }
            };


            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (running)
            {
                // check if the Console size has changed
                // There is no Event that fires if the Console has changed size, so we need to check every frame
                var newWidth = Console.WindowWidth;
                var newHeight = Console.WindowHeight;

                if (width != newWidth || height != newHeight)
                {
                    width = newWidth;
                    height = newHeight;

                    backBuffer = new ConsoleBackBuffer(width, height);
                    rainDrops = InitializeRainDrops(width, height);
                }

                backBuffer.Clear();

                //backBuffer.Clear('\u2588', ConsoleColor.Cyan, ConsoleColor.Black);

                foreach (var rainDrop in rainDrops)
                {
                    rainDrop.Update();
                    RenderDrop(rainDrop, backBuffer);
                }

                if (showFPS)
                {
                    bool bufferWriteSuccess = statsTextBuffer.TryWrite($"Frames: {totalFrames}\r\nFPS: {fps}\r\nDelay: {delay}ms", out int statsTextLength);
                    Debug.Assert(bufferWriteSuccess, "Stats text buffer is too small for the text. Increase size of the buffer.");
                    if (bufferWriteSuccess)
                    {
                        ReadOnlySpan<char> statsText = statsTextBuffer[..statsTextLength];
                        backBuffer.WriteText(0, 0, statsText);
                    }
                }

                backBuffer.Blit();

                // Try changing this or removing it to see how fast we can redraw to the terminal
                Thread.Sleep(delay);

                totalFrames++;

                if (stopWatch.ElapsedMilliseconds > 1000)
                {
                    fps = totalFrames - lastFrames;
                    lastFrames = totalFrames;
                    stopWatch.Restart();
                }
            }

            keyListener.Stop();

            // Set each raindrop to expire

            foreach (var rainDrop in rainDrops)
            {
                rainDrop.Expires = true;
            }

            // Wait for all raindrops to go offscreen

            while (rainDrops.Any(r => !r.Offscreen))
            {
                backBuffer.Clear();

                foreach (var rainDrop in rainDrops)
                {
                    if (!rainDrop.Offscreen)
                    {
                        rainDrop.Update();
                        RenderDrop(rainDrop, backBuffer);
                    }
                }

                backBuffer.Blit();

                Thread.Sleep(delay);
            }


            Console.CursorVisible = true;
            Console.Clear();
        }

        static List<RainDrop> InitializeRainDrops(int width, int height)
        {
            var rainDrops = new List<RainDrop>();

            for (var i = 0; i < 60; i++)
            {
                var rainDrop = new RainDrop(width, height);
                rainDrops.Add(rainDrop);
            }

            return rainDrops;
        }

        static void RenderDrop(RainDrop rainDrop, ConsoleBackBuffer backBuffer)
        {
            // Draw each character in the raindrop.
            // Only draw from the tail to the head. This will allow overlapping drops,
            // unlike if we drew from the top of the column to the bottom (0 to height)

            for (var i = rainDrop.Tail; i <= rainDrop.Head; i++)
            {
                var age = rainDrop.Ages[i];
                var character = rainDrop.Chars[i];

                ConsoleColor color;

                if (age == 1)
                {
                    color = ConsoleColor.White;
                }
                else if (age == 2)
                {
                    color = ConsoleColor.Yellow;
                }
                else if (age == 3)
                {
                    color = ConsoleColor.DarkYellow;
                }
                else
                {
                    // The length of the raindrop is also our max age
                    var agePercent = (rainDrop.Ages[i] / (float)rainDrop.Length);

                    //  > 70% of max age : dark gray
                    //  > 30% of max age : dark green
                    // <= 30% of max age : green

                    color = agePercent > 0.7
                        ? ConsoleColor.DarkGray
                        : agePercent > 0.3
                            ? ConsoleColor.DarkGreen
                            : ConsoleColor.Green;
                }

                backBuffer.SetCell(rainDrop.X, i, character, color, ConsoleColor.Black);
            }

        }
    }
}
