namespace DmdClock.Core.Library;

public sealed class NaturalPathComparer : IComparer<string>
{
    public static NaturalPathComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left is null) return -1;
        if (right is null) return 1;

        var leftIndex = 0;
        var rightIndex = 0;
        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            if (char.IsDigit(left[leftIndex]) && char.IsDigit(right[rightIndex]))
            {
                var leftStart = leftIndex;
                var rightStart = rightIndex;
                while (leftIndex < left.Length && char.IsDigit(left[leftIndex])) leftIndex++;
                while (rightIndex < right.Length && char.IsDigit(right[rightIndex])) rightIndex++;

                var leftNumber = left.AsSpan(leftStart, leftIndex - leftStart).TrimStart('0');
                var rightNumber = right.AsSpan(rightStart, rightIndex - rightStart).TrimStart('0');
                var lengthComparison = leftNumber.Length.CompareTo(rightNumber.Length);
                if (lengthComparison != 0) return lengthComparison;
                var numberComparison = leftNumber.CompareTo(rightNumber, StringComparison.Ordinal);
                if (numberComparison != 0) return numberComparison;
                continue;
            }

            var characterComparison = char.ToUpperInvariant(left[leftIndex]).CompareTo(char.ToUpperInvariant(right[rightIndex]));
            if (characterComparison != 0) return characterComparison;
            leftIndex++;
            rightIndex++;
        }

        return left.Length.CompareTo(right.Length);
    }
}

