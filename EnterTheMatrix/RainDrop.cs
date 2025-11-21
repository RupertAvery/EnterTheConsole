namespace EnterTheMatrix;


// The RainDrop is modeled as a column of cells that occupies the screen from top to bottom.
// Each cell contains a character and the age of the cell, modeled as two separate arrays of the same size.
// Initially all the cells are empty and the age of each cell is zero
// We use the Position property to tell us where the current position of the head of the raindrop is
// We have a Tail and Head computed properties that tells us which cells are occupied by the raindrop
// Every Update we move to the next cell and set the character of the next cell to a random character
// We increment the age of each cell from the tail to the head
// We also randomly change some the cells anywhere between the tail and the head
// The effect is that as the Head passes through the column, it leaves behind a random character in that cell
// When we render the cell, we set the color of the cell based on its age. The older the cell, the dimmer it gets

public class RainDrop
{

    // Stores the width of the screen. Used to reposition the raindrop after it expires
    private int Width;

    /// <summary>
    /// The height of the column, also the size of the arrays storing information about each cell in the column
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// The horizontal position of the raindrop on the screen
    /// </summary>
    public int X { get; private set; }

    /// <summary>
    /// The buffer that holds the character to display at a given vertical position on screen
    /// </summary>
    public char[] Chars { get; private set; }

    /// <summary>
    /// An array that holds the "age" of each character position on screen. used to control color when rendering
    /// </summary>
    public int[] Ages { get; private set; }

    /// <summary>
    /// The vertical position of the head of the raindrop.
    /// Can extend past the array bounds
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Determines how long the raindrop will get and also how long will be visible on screen
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// The visible tail position of the RainDrop, clamped between 0 and Height - 1 to avoid going out-of-bounds of the arrays
    /// </summary>
    public int Tail => Math.Min(Math.Max(0, Position - Length), Height - 1);

    /// <summary>
    /// The visible head position of the RainDrop, clamped to Height - 1 to avoid going out-of-bounds of the arrays
    /// </summary>
    public int Head => Math.Min(Position, Height - 1);

    public bool Expires { get; set; }

    public bool Offscreen { get; private set; }


    public RainDrop(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        this.Position = 0;

        Chars = new char[height];
        Ages = new int[height];

        Chars[0] = GetRandomChar();
        Ages[0] = 1;

        X = Random.Shared.Next(0, width);

        Length = Random.Shared.Next(15, 45);

        Expires = false;
        Offscreen = false;
    }

    /// <summary>
    /// Updates the state of the raindrop
    /// </summary>
    public void Update()
    {
        if (Offscreen && Expires) return;

        // Randomly mutate 1 or more characters anywhere between the head and the tail
        var mutations = Random.Shared.Next(1, Length / 4);

        var head = Head;

        for (var i = 0; i < mutations; i++)
        {
            Chars[Random.Shared.Next(Tail, head)] = GetRandomChar();
        }

        for (var i = 0; i <= head; i++)
        {
            // increment the age of each cell
            Ages[i] += 1;

            // If the age of the cell goes beyond the Length, clear the cell
            if (Ages[i] > Length)
            {
                Chars[i] = ' ';
            }
        }

        // "Move" the raindrop down
        Position++;

        // Update the head variable after updating the position
        head = Head;

        // Don't draw past the height!
        if (Position < Height)
        {
            Ages[head] = 1;
            Chars[head] = GetRandomChar();
        }

        // Check if the raindrop is fully off-screen
        if (Position > Height + Length)
        {
            Offscreen = Expires;

            // Clear the column and reset the ages
            for (var i = 0; i < Height; i++)
            {
                Ages[i] = 0;
                Chars[i] = ' ';
            }

            X = Random.Shared.Next(0, Width);
            Length = Random.Shared.Next(15, 45);

            // Move back to the top
            Position = 0;
        }
    }

    private static char GetRandomChar()
    {
        return (char)Random.Shared.Next(33, 126);
        //return (char)Random.Shared.Next(0x30a0, 0x30ff);

        // I tried Japanese characters, but some of them are wider than regular characters 
        // and throw the alignment off

        //switch (Random.Shared.Next(0,8))
        //{
        //    case 0:
        //        return (char)Random.Shared.Next(33, 126);
        //    case 1:
        //        // Hiragana
        //        return (char)Random.Shared.Next(0x3040, 0x309f);
        //    case 2:
        //        // Katakana
        //        return (char)Random.Shared.Next(0x30a0, 0x30ff);
        //    case 3:
        //        // CJK Ideograms
        //        return (char)Random.Shared.Next(0x4e00, 0x9faf);
        //    default:
        //        return (char)Random.Shared.Next(33, 126);
        //}
    }
}