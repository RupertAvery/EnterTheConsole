# Digital Rain in the Terminal

This is a sample program that demonstrates buffered drawing to the terminal using P/Invoke.  

As you progress in C#, you might find yourself trying to build more complex console programs and encountering flickering and lag. This is an inherent limitation of using the default Console methods, because these methods are designed for immediate text ouput, not graphics.

Feel free to borrow the code and use in your own projects, and enhance and extend the classes as you need.

# The Terminal

The terminal itself is created and managed by the OS, not by .NET. Whenever we call any Console methods, we have to perform a platform invoke, or P/Invoke. 

This is an expensive call. There is additional overhead when crossing the boundary between .NET managed code and the OS or Win32 API layer. 

Console methods are designed to update the terminal with the changes immediately, which is useful when printing messages onto the screen.

The problem arises when you call Console methods several times in a loop, such as when drawing to various positions on screen. You are introducing a lag by making that P/Invoke call as well as having the terminal draw the update for that single call.

When making console games that update many cells, or the entire screen each frame, the best way to do it is to draw everything in memory, then flush it to the terminal all at once in one call.

## Memory Layout

To display a character in the terminal, you need 3 things: the character to display, the foreground color, and the background color. These 3 things are stored in a "cell".

The contents of the terminal is stored as an array of bytes, with each cell taking 4 bytes.

The first two bytes stores the character to be displayed in the cell. Having 2 bytes allows for Unicode characters to be represented. 

The next two bytes stores the attribute, but only the first byte is used. 

The reason 4 bytes per cell are used and not 3 bytes is probably memory alignment - it is more performant for memory operations to operate on sizes that are powers of 2.

```
 |   CELL 0  | |   CELL 1  | |   CELL 2  |
 |CL|CH|AL|AH| |CL|CH|AL|AH| |CL|CH|AL|AH|  ...

  CL - Character low byte
  CH - Character high byte
  AL - Attribute low byte
  AH - Attribute high byte
```

The attribute contains the foreground color (the color that is used when displaying the character in the cell) and the background color.

The terminal supports only 16 colors, this means only 4 bits (2^4 = 16) are needed to represent all the available colors. So the foreground and background colors are stuffed or packed into one byte, with the foreground taking the lower 4 bits (nibble) and the background taking the upper nibble.

How can you pack the foreground and background into one byte?

Lets start with two values that are stored in separate variables.

Here are 2 8-bit bytes, where F represent the bits for the foreground color, and B representing the bits for the background color

```
0000FFFF - Foreground byte - lower 4 bits
0000BBBB - Background byte - lower 4 bits
```

We first need to clear the upper 4 bits, just it case the bytes contain values higher than 16, otherwise we would end up with incorrect values later.

To do this, we modulo the values with 16. This is the remainder after division with 16, and will always be a value lower than 16.

Then we shift the background 4 bits to the left. This will move the lower 4 bits into the upper 4 bits, and leave the lower bits as 0

```
BBBB0000 - Background byte shifted 4 bits left
```

Now we can OR the foreground and background byte, merging the bits together.

```
0000FFFF - Foreground byte - lower 4 bits
BBBB0000 - Background byte shifted 4 bits left
BBBBFFFF - Foreground and background bytes ORed
```
The code to do this is as follows:

```csharp
(foreground[i] % 16) | ((background[j] % 16) << 4)
```

## Writing a character and an attribute to the buffer

Since each cell takes 4 bytes, and has Width * Height cells, the index of the first byte in the four-byte group representing one cell is

```csharp
int index = (y * Width + x) * 4;
```

We compute our attribute value from the foreground and background bytes        

```csharp
ushort attr = (ushort)((foreground % 16) | ((background % 16) << 4));
```

A char is a 16-bit value, so in order to break it up into two bytes, we mask the bottom byte by bitwise ANDing with 0xFF,  store it in the lower byte, and then shift the char 8 bits to the right, moving the upper 8 bits into the lower 8 bits, then mask again by bitwise ANDing with 0xFF.

Here, the char contains 16 bits, and I have denoted C as the upper 8 bits, and c as the lower 8 bits:

```
CCCCCCCCcccccccc  - 16-bit char
0000000011111111  - 0xFF Mask  
00000000cccccccc  - bitwise AND result

CCCCCCCCcccccccc  - 16-bit char
00000000CCCCCCCC  - upper 8 bits shifted right by 8 bits
0000000011111111  - 0xFF Mask  
00000000CCCCCCCC  - bitwise AND result
```

You may notice that the mask is not actually necessary, because shifting a 16-bit value right fills in the 
upper bits with zero anyway. If the input value was 32-bit or 64-bit, it might be necessary, so it pays to be 
consistent.

```csharp
buffer[index + 0] = (byte)(c & 0xFF);           // Store lower 8 bits of c in first byte
buffer[index + 1] = (byte)((c >> 8) & 0xFF);    // Store upper 8 bits of c in second byte

// Write attributes
buffer[index + 2] = (byte)(attr & 0xFF);        // Store lower 8 bits of attr in second byte
buffer[index + 3] = (byte)((attr >> 8) & 0xFF); // Store upper 8 bits of attr in second byte
```

## Blitting to the Terminal

With the buffer filled, it's time to blit the buffer to the terminal. "Blit" is a term from the acronym BLT, which stands for BLock Transfer, the act of copying data in a contiguous block from one location to another.

To do this requires P/Invoke, calling the OS methods to transfer data to the terminal.

```csharp
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
```

The `ConsoleBackBuffer` class encapsulates all of this functionality. You can use it and extend it for your own purposes.

## Limitations

The `ConsoleBackBuffer` is designed primarily for drawing to a buffer the size of the visible area of the terminal and simply copying it to the terminal. As such, it does not support scrolling.

The `Write` and `WriteText` methods are untested and may not behave exactly as expected in certain cases.

# The Rain Drop

The Digital Rain effect used in The Matrix shows "rain drops" falling down from the top of the screen. As the drop moves downwards, is changes to a random character, and leaves a trail of green characters that fade away. The drop itself glows white.

In this example the Rain Drop is modeled as a column of cells that occupies the screen from top to bottom. To animate the drop, we simply have a counter `Position`, and at each `Update`, we increment the `Position` and draw a random character to the current cell.

To achieve the color changes, we assign an age to each cell. The age of the cell starts at 1, and every update we increment the age of each cell. When the age of the cell exceeds the `MaxAge` of the rain drop, the cell is cleared.

If we were to visualize the contents of the cells and the ages after each update it might look like this:

Chars

```
0   1   2   3    Position
--------------
r   r   r   r
    @   @   @
        9   9
            U
```

Ages

```
0   1   2   3    Position
--------------
1   2   3   4
    1   2   3
        1   2 
            1
```

# The KeyListener

The `KeyListener` class handles keypresses and exposes an event to capture them.

It runs in a `Task`, so it is independent of the update rate of your main loop. Note the use of a `CancellationTokenSource` to handle exiting the Task gracefully.

Another thing the KeyListener does is swallow buffered keypresses. Holding down a key on the keyboard will begin to insert keypresses into the buffer when the loop can't process them fast enough. This will lead to phantom keypresses that continue to be processed after you let go of the key. To prevent this, we loop, reading the keys off the buffer until there are none left.

```csharp
while (Console.KeyAvailable)
{
    Console.ReadKey(true);
}
```