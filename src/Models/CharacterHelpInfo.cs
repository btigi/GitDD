namespace GitDD.Models;

public sealed class CharacterHelpInfo
{
    public string ClassHelp { get; set; } = string.Empty;
    public string LevelHelp { get; set; } = string.Empty;
    public string AlignmentHelp { get; set; } = string.Empty;
    public string StrengthHelp { get; set; } = string.Empty;
    public string DexterityHelp { get; set; } = string.Empty;
    public string ConstitutionHelp { get; set; } = string.Empty;
    public string IntelligenceHelp { get; set; } = string.Empty;
    public string WisdomHelp { get; set; } = string.Empty;
    public string CharismaHelp { get; set; } = string.Empty;
    public string PublicGistsHelp { get; set; } = string.Empty;
    public string AlliesHelp { get; set; } = string.Empty;
    public string DisciplesHelp { get; set; } = string.Empty;
    public string LanguagesSpokenHelp { get; set; } = string.Empty;
    public string SignatureWeaponHelp { get; set; } = string.Empty;
    public string ProficienciesHelp { get; set; } = string.Empty;
    public string AdventuringReportHelp { get; set; } = string.Empty;

    public string? GetAbilityHelp(string abbreviation) => abbreviation switch
    {
        "STR" => StrengthHelp,
        "DEX" => DexterityHelp,
        "CON" => ConstitutionHelp,
        "INT" => IntelligenceHelp,
        "WIS" => WisdomHelp,
        "CHA" => CharismaHelp,
        _ => null
    };
}
