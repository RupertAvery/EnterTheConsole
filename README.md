<img width="1425" height="701" alt="image" src="https://github.com/user-attachments/assets/6de0d43a-3ddd-4716-9fe4-a54a9a5f6158" />

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

# The Console Back Buffer

The `AnsiConsoleBackBuffer` class manages 3 components: The character buffer, which says what characters to draw on the screen, the foreground buffer, which says what color each character should be, and the background buffer, which says what the background of character cell should be.

When rendering the buffers to be displayed, it scans over the buffers row by row and converts them into ANSI Escape codes. For efficiency, it only emits escape codes when the foreground and/or background color change. It builds everything in a StringBuilder, then uses Console.Out.Write(StringBuilder) to send all the commands to the Console at once, effectively rendering a "frame" of data.

## ANSI Escape Codes

Modern terminals use ANSI escape codes to send commands to the terminal. Modern terminals allow 24-bit colors (RGB). The commands for changing the current color are as follows:

```
Foreground color - \e[38;2;{r};{g};{b}m
Background coloor - \e[48;2;{r};{g};{b}m
```

`\e` is the shortcut for the escape character `ESC`, character `\x1b`. This tells the terminal that the following characters are commands, not text. 

Here is a more comprehensive list of ANSI escape codes: https://gist.github.com/fnky/458719343aabd01cfb17a3a4f7296797

## Limitations

The `AnsiConsoleBackBuffer` is designed primarily for drawing to a buffer the size of the visible area of the terminal and simply copying it to the terminal. As such, it does not support scrolling.

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
