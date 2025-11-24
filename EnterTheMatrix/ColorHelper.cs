using System.Runtime.CompilerServices;

namespace EnterTheMatrix;

public static class ColorHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RGB(byte r, byte g, byte b)
    {
        return (r << 16) | (g << 8) | (b);
    }
}