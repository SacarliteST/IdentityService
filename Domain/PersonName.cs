namespace IdentityService.Domain;

/// <summary>Правила нормализации ФИО и сборки отображаемого имени.</summary>
public static class PersonName
{
    public const int MaxPartLength = 100;

    /// <summary>Обрезает пробелы; пустая строка становится <see langword="null"/>.</summary>
    public static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return String.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>Отображаемое имя «Имя Фамилия» (отчество в короткое имя не входит); нет ни имени, ни фамилии — <see langword="null"/>.</summary>
    public static string? BuildDisplayName(string? firstName, string? lastName)
    {
        var parts = new[] { Normalize(firstName), Normalize(lastName) }.Where(part => part is not null);
        var name = String.Join(' ', parts);
        return name.Length == 0 ? null : name;
    }
}
