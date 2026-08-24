namespace PetHub.API.Utils;

public static class CnpjHelper
{
    public static string Normalize(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return string.Empty;

        return new string(cnpj.Where(char.IsDigit).ToArray());
    }

    public static bool IsValid(string? cnpj)
    {
        var digits = Normalize(cnpj);
        if (digits.Length != 14)
            return false;

        if (digits.Distinct().Count() == 1)
            return false;

        var numbers = digits.Select(c => c - '0').ToArray();

        return CalculateCheckDigit(numbers, 12) == numbers[12]
            && CalculateCheckDigit(numbers, 13) == numbers[13];
    }

    private static int CalculateCheckDigit(int[] numbers, int length)
    {
        int[] weights =
            length == 12
                ? [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
                : [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
            sum += numbers[i] * weights[i];

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
