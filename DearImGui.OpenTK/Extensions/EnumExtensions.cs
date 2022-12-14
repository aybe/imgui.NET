#pragma warning disable CS1591
namespace DearImGui.OpenTK.Extensions;

public static class EnumExtensions
{
    public static bool HasFlags<T>(this T value, T flags) where T : struct, Enum, IConvertible
    {
        var a = value.ToUInt64(null);
        var b = flags.ToUInt64(null);
        var c = (a & b) == b;

        return c;
    }
}