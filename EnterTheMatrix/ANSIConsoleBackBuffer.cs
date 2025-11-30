using System.Runtime.InteropServices;
using System.Text;

namespace EnterTheMatrix;

/// <summary>
/// Creates a buffer for fast graphical updates to the Console
/// </summary>
public class AnsiConsoleBackBuffer
{
    private readonly StringBuilder _stringBuilder;

    public int ForegroundColor = ColorHelper.RGB(204, 204, 204);
    public int BackgroundColor = ColorHelper.RGB(0, 0, 0);

    public int X = 0;
    public int Y = 0;

    public readonly int Width;
    public readonly int Height;

    private readonly int[] _fgBuffer;
    private readonly int[] _bgBuffer;
    private readonly char[] _buffer;

    public AnsiConsoleBackBuffer(int width, int height)
    {
        Width = width;
        Height = height;

        _buffer = new char[width * height];
        _fgBuffer = new int[width * height];
        _bgBuffer = new int[width * height];
        _stringBuilder = new StringBuilder(width * height);
    }

    /// <summary>
    /// Writes text to the buffer at the current cursor position, and increments the X position of the cursor
    /// </summary>
    /// <param name="text"></param>
    public void Write(string text)
    {
        WriteText(X, Y, text, ForegroundColor, BackgroundColor);
        X += text.Length;
    }


    /// <summary>
    /// Writes text to the buffer at the current cursor position, and moves the cursor to the next line
    /// </summary>
    /// <param name="text"></param>
    public void WriteLine(string text)
    {
        Write(text);
        X = 0;
        if (Y < Height)
        {
            Y++;
        }
    }


    /// <summary>
    /// Writes text to the buffer at the specified cursor position using the colors specified for each character in the text.
    /// If the colors contain fewer entries than the number of characters in the text, the colors will repeat from the beginning.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="text"></param>
    /// <param name="foreground"></param>
    /// <param name="background"></param>
    public void WriteText(int x, int y, string text, int[] foreground, int[] background)
    {
        int index = (y * Width + x);

        int i = 0;
        int j = 0;

        // Write UTF-16 char
        foreach (var c in text)
        {
            if (c == '\r')
            {
                continue;
            }
            else if (c == '\n')
            {
                // move to the next Line
                index += (Width) - (index % (Width));
                continue;
            }


            _buffer[index] = c;
            _fgBuffer[index] = foreground[i];
            _bgBuffer[index] = background[j];
            index += 1;
            i++;
            if (i >= foreground.Length) i = 0;
            j++;
            if (j >= background.Length) j = 0;
        }
    }

    /// <summary>
    /// Writes text to the buffer at the specified cursor position using the current Foreground and Background colors
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="text"></param>
    public void WriteText(int x, int y, string text)
    {
        WriteText(x, y, text, ForegroundColor, BackgroundColor);
    }

    /// <summary>
    /// Writes text to the buffer at the specified cursor position using the specified fgBuffer and background colors
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="text"></param>
    /// <param name="foreground"></param>
    /// <param name="background"></param>
    public void WriteText(int x, int y, string text, int foreground, int background)
    {
        int index = (y * Width + x);

        // Write UTF-16 char
        foreach (var c in text)
        {
            if (index >= _buffer.Length) break;

            if (c == '\r')
            {
                continue;
            }
            else if (c == '\n')
            {
                // move to the next Line
                index += (Width) - (index % (Width));
                continue;
            }

            _buffer[index] = c;
            _fgBuffer[index] = foreground;
            _bgBuffer[index] = background;
            index += 1;
        }
    }

    /// <summary>
    /// Sets all characters in the buffer to an empty space, using the current Foreground and Background colors 
    /// </summary>
    public void Clear()
    {
        Clear(' ', ForegroundColor, BackgroundColor);
    }

    /// <summary>
    /// Sets all characters in the buffer to a specified character, using the specified fgBuffer and background colors 
    /// </summary>
    public void Clear(Char c, int foreground, int background)
    {
        _buffer.AsSpan().Fill(c);
        _fgBuffer.AsSpan().Fill(foreground);
        _bgBuffer.AsSpan().Fill(background);
    }

    /// <summary>
    /// Sets the character and fgBuffer and background colors at the specified cursor position.
    /// </summary>
    public void SetCell(int x, int y, char c, int foreground, int background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return;

        int index = (y * Width + x);

        _buffer[index] = c;
        _fgBuffer[index] = foreground;
        _bgBuffer[index] = background;
    }

    /// <summary>
    /// Renders the character and color buffers to ANSI escape codes
    /// </summary>
    /// <returns></returns>
    private StringBuilder Render()
    {
        // Reuse the stringBuilder
        _stringBuilder.Clear();

        int index = 0;

        var size = Width * Height;

        int currentForeground = _fgBuffer[index];
        int currentBackground = _bgBuffer[index];

        // Move to 0,0
        // Alternative to SetCursorPosition(0,0)
        //_stringBuilder.Append("\e[H");

        // Set the initial foreground and background colors
        _stringBuilder.Append(RgBtoAnsiEscapeCode(ColorSeqCode.Foreground, currentForeground));
        _stringBuilder.Append(RgBtoAnsiEscapeCode(ColorSeqCode.Background, currentBackground));

        bool foregroundChanged = false;
        bool backgroundChanged = false;

        int lastIndex = 0;

        var span = _buffer.AsSpan();

        while (index < size)
        {
            foregroundChanged = currentForeground != _fgBuffer[index];
            backgroundChanged = currentBackground != _bgBuffer[index];

            // to avoid writing the foreground/background escape sequences for every character,
            // we only write to the StringBuilder when either of the colors change
            if (foregroundChanged || backgroundChanged)
            {
                currentForeground = _fgBuffer[index];
                currentBackground = _bgBuffer[index];

                if (lastIndex < index)
                {
                    // write everything since the last update, all the way to the current position
                    _stringBuilder.Append(span[lastIndex..index]);
                    lastIndex = index;
                }

                // update the color states as needed
                if (foregroundChanged)
                {
                    _stringBuilder.Append(RgBtoAnsiEscapeCode(ColorSeqCode.Foreground, currentForeground));
                }
                if (backgroundChanged)
                {
                    _stringBuilder.Append(RgBtoAnsiEscapeCode(ColorSeqCode.Background, currentBackground));
                }
            }

            index++;
        }

        // If we reached the end of the buffer, check for any pending characters that are unwritten
        if (lastIndex < size)
        {
            _stringBuilder.Append(span[lastIndex..size]);
        }

        return _stringBuilder;
    }

    /// <summary>
    /// Writes the entire back buffer to the console screen buffer
    /// </summary>
    public void Blit()
    {
        Console.SetCursorPosition(0, 0);
        Console.Out.Write(Render());
    }



    // Color cache
    private static Dictionary<string, string> _rgbColors = new();

    private static string RgBtoAnsiEscapeCode(ColorSeqCode code, int rgb)
    {
        var key = $"{(int)code}:{rgb}";

        // Instead of rebuilding the colors everytime we need them, we can cache them
        // There is probably a slight performance benefit to this, but most of the performance is swallowed
        // by rendering the buffer to ANSI and writing it to the console
        if (!_rgbColors.TryGetValue(key, out var value))
        {
            int b = rgb & 0xFF;
            int g = (rgb >> 8) & 0xFF;
            int r = (rgb >> 16) & 0xFF;
            value = $"\e[{(int)code};2;{r};{g};{b}m";
            _rgbColors[key] = value;
        }

        return value;
    }

    private enum ColorSeqCode
    {
        Foreground = 38,
        Background = 48
    }

    public static void EnableAnsiMode()
    {
        // Get the handle to the standard output stream
        var handle = GetStdHandle(StdOutputHandle);

        // Get the current console mode
        uint mode;
        if (!GetConsoleMode(handle, out mode))
        {
            Console.Error.WriteLine("Failed to get console mode");
            return;
        }

        // Enable the virtual terminal processing mode
        mode |= EnableVirtualTerminalProcessing;

        if (!SetConsoleMode(handle, mode))
        {
            Console.Error.WriteLine("Failed to set console mode");
            return;
        }

    }

    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 4;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}