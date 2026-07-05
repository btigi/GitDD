namespace GitDD.Models;

public sealed class DndCharacter
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public AbilityScores AbilityScores { get; set; } = new();
    public string Class { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Alignment { get; set; } = string.Empty;
    public string AdventuringReport { get; set; } = string.Empty;
    public int PublicGists { get; set; }
    public int Allies { get; set; }
    public IReadOnlyList<string> LanguagesSpoken { get; set; } = [];
    public string SignatureWeaponName { get; set; } = string.Empty;
    public int SignatureWeaponStars { get; set; }
    public int Disciples { get; set; }
    public IReadOnlyList<string> Proficiencies { get; set; } = [];
    public int HiddenProficiencyCount { get; set; }
    public CharacterHelpInfo Help { get; set; } = new();
}

public sealed class AbilityScores
{
    public AbilityScore Strength { get; set; } = new();
    public AbilityScore Dexterity { get; set; } = new();
    public AbilityScore Constitution { get; set; } = new();
    public AbilityScore Intelligence { get; set; } = new();
    public AbilityScore Wisdom { get; set; } = new();
    public AbilityScore Charisma { get; set; } = new();

    public IEnumerable<(string Name, string Abbreviation, AbilityScore Score)> All =>
    [
        ("Strength", "STR", Strength),
        ("Dexterity", "DEX", Dexterity),
        ("Constitution", "CON", Constitution),
        ("Intelligence", "INT", Intelligence),
        ("Wisdom", "WIS", Wisdom),
        ("Charisma", "CHA", Charisma)
    ];
}

public sealed class AbilityScore
{
    public int Score { get; set; }

    public int Modifier => (int)Math.Floor((Score - 10) / 2.0);

    public string ModifierLabel => Modifier >= 0 ? $"+{Modifier}" : Modifier.ToString();
}
