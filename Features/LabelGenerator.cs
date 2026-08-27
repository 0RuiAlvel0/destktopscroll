namespace DesktopScroll;

public static class LabelGenerator
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

    public static string[] GenerateLabels(int cellCount, int minLength = 2, int maxLength = 3)
    {
        if (cellCount <= 0)
        {
            return [];
        }

        var maxCapacity = CapacityForLength(maxLength);
        if (cellCount > maxCapacity)
        {
            throw new InvalidOperationException($"Grid requires {cellCount} labels, but max supported with length {maxLength} is {maxCapacity}.");
        }

        var labelLength = DetermineLabelLength(cellCount, minLength, maxLength);
        var labels = new List<string>();

        for (var index = 0; index < cellCount; index++)
        {
            labels.Add(ToLabel(index, labelLength));
        }

        return labels.ToArray();
    }

    public static int DetermineLabelLength(int cellCount, int minLength, int maxLength)
    {
        if (cellCount <= 0)
        {
            return minLength;
        }

        var minRequiredLength = minLength;
        var maxCellsForMinLength = CapacityForLength(minLength);
        if (cellCount <= maxCellsForMinLength)
        {
            return minLength;
        }

        var candidate = minLength + 1;
        while (candidate <= maxLength)
        {
            var capacity = CapacityForLength(candidate);
            if (cellCount <= capacity)
            {
                return candidate;
            }

            candidate++;
        }

        return Math.Min(maxLength, Math.Max(minLength, maxLength));
    }

    private static string ToLabel(int index, int length)
    {
        var value = index;
        var chars = new char[length];

        for (var position = length - 1; position >= 0; position--)
        {
            chars[position] = Alphabet[value % Alphabet.Length];
            value /= Alphabet.Length;
        }

        return new string(chars);
    }

    private static int CapacityForLength(int length)
    {
        return (int)Math.Pow(Alphabet.Length, Math.Max(1, length));
    }
}
