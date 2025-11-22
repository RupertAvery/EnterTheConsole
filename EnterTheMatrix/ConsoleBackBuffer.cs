using System.Runtime.InteropServices;

namespace EnterTheMatrix;

/// <summary>
/// Creates a buffer for fast graphical updates to the Console
/// </summary>
public class ConsoleBackBuffer
{
    public ConsoleColor ForegroundColor = ConsoleColor.Gray;
    public ConsoleColor BackgroundColor = ConsoleColor.Black;

    public int X = 0;
    public int Y = 0;

    public readonly int Width;
    public readonly int Height;

    // Raw CHAR_INFO array data (2 bytes char + 2 bytes attributes)
    private readonly byte[] buffer;

    public ConsoleBackBuffer(int width, int height)
    {
        this.Width = width;
        this.Height = height;

        // Each cell = sizeof(CHAR_INFO) = 4 bytes
        buffer = new byte[width * height * 4];
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
    public void WriteText(int x, int y, ReadOnlySpan<char> text, ConsoleColor[] foreground, ConsoleColor[] background)
    {

        int index = (y * Width + x) * 4;

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
                index += (Width * 4) - (index % (Width * 4));
                continue;
            }

            if (index >= buffer.Length) break;

            buffer[index + 0] = (byte)(c & 0xFF);
            buffer[index + 1] = (byte)((c >> 8) & 0xFF);

            ushort attr = (ushort)(((byte)foreground[i] % 16) | (((byte)background[j] % 16) << 4)); // FG/BG color

            // Write attributes
            buffer[index + 2] = (byte)(attr & 0xFF);
            buffer[index + 3] = (byte)((attr >> 8) & 0xFF);

            index += 4;
            i++;
            j++;
            if (i >= foreground.Length) i = 0;
            if (j >= background.Length) j = 0;
        }
    }

    /// <summary>
    /// Writes text to the buffer at the specified cursor position using the current Foreground and Background colors
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="text"></param>
    public void WriteText(int x, int y, ReadOnlySpan<char> text)
    {
        WriteText(x, y, text, ForegroundColor, BackgroundColor);
    }

    /// <summary>
    /// Writes text to the buffer at the specified cursor position using the specified foreground and background colors
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="text"></param>
    /// <param name="foreground"></param>
    /// <param name="background"></param>
    public void WriteText(int x, int y, ReadOnlySpan<char> text, ConsoleColor foreground, ConsoleColor background)
    {

        int index = (y * Width + x) * 4;

        ushort attr = (ushort)(((int)foreground % 16) | (((int)background % 16) << 4)); // FG/BG color

        // Write UTF-16 char
        foreach (var c in text)
        {
            if (index >= buffer.Length) break;

            if (c == '\r')
            {
                continue;
            }
            else if (c == '\n')
            {
                // move to the next Line
                index += (Width * 4) - (index % (Width * 4));
                continue;
            }

            buffer[index + 0] = (byte)(c & 0xFF);
            buffer[index + 1] = (byte)((c >> 8) & 0xFF);

            // Write attributes
            buffer[index + 2] = (byte)(attr & 0xFF);
            buffer[index + 3] = (byte)((attr >> 8) & 0xFF);
            index += 4;
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
    /// Sets all characters in the buffer to a specified character, using the specified foreground and background colors 
    /// </summary>
    public void Clear(Char c, ConsoleColor foreground, ConsoleColor background)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                int index = (y * Width + x) * 4;

                ushort attr = (ushort)(((int)foreground % 16) | (((int)background % 16) << 4)); // FG/BG color


                if (index >= buffer.Length) break;
                buffer[index + 0] = (byte)(c & 0xFF);
                buffer[index + 1] = (byte)((c >> 8) & 0xFF);

                // Write attributes
                buffer[index + 2] = (byte)(attr & 0xFF);
                buffer[index + 3] = (byte)((attr >> 8) & 0xFF);
            }
        }
    }

    /// <summary>
    /// Sets the character and foreground and background colors at the specified cursor position.
    /// </summary>
    public void SetCell(int x, int y, char c, ConsoleColor foreground, ConsoleColor background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return;

        int index = (y * Width + x) * 4;

        ushort attr = (ushort)(((int)foreground % 16) | (((int)background % 16) << 4)); // FG/BG color

        // Write UTF-16 char
        buffer[index + 0] = (byte)(c & 0xFF);
        buffer[index + 1] = (byte)((c >> 8) & 0xFF);

        // Write attributes
        buffer[index + 2] = (byte)(attr & 0xFF);
        buffer[index + 3] = (byte)((attr >> 8) & 0xFF);
    }

    /// <summary>
    /// Writes the entire back buffer to the console screen buffer
    /// </summary>
    public void Blit()
    {
        IntPtr hConsole = GetStdHandle(STD_OUTPUT_HANDLE);

        SMALL_RECT writeRegion = new SMALL_RECT
        {
            Left = 0,
            Top = 0,
            Right = (short)(Width - 1),
            Bottom = (short)(Height - 1)
        };

        COORD bufferSize = new COORD((short)Width, (short)Height);
        COORD bufferCoord = new COORD(0, 0);

        // Pin buffer to unmanaged memory
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        try
        {
            WriteConsoleOutputW(
                hConsole,
                handle.AddrOfPinnedObject(),
                bufferSize,
                bufferCoord,
                ref writeRegion);
        }
        finally
        {
            handle.Free();
        }
    }

    // -----------------------------
    //    Native P/Invoke bindings
    // -----------------------------
    private const int STD_OUTPUT_HANDLE = -11;

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
        public COORD(short x, short y)
        {
            X = x;
            Y = y;
        }
    }

    // Must match Win32 CHAR_INFO layout!!!
    // CHAR_INFO is 4 bytes: UTF-16 char + WORD attributes
    [StructLayout(LayoutKind.Sequential)]
    private struct CHAR_INFO
    {
        public char Char;
        public ushort Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteConsoleOutputW(
        IntPtr hConsoleOutput,
        IntPtr lpBuffer,
        COORD dwBufferSize,
        COORD dwBufferCoord,
        ref SMALL_RECT lpWriteRegion);
}