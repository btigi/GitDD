using GitDD.Models;

namespace GitDD.Services;

public sealed class CharacterBuilder
{
    private const int MaxDisplayedProficiencies = 20;

    private static readonly (string Stat, string ClassName)[] ClassPriority =
    [
        ("STR", "Barbarian"),
        ("DEX", "Rogue"),
        ("CON", "Paladin"),
        ("INT", "Wizard"),
        ("WIS", "Cleric"),
        ("CHA", "Bard")
    ];

    public DndCharacter Build(GitHubProfileData profile)
    {
        var user = profile.User;
        var ownedRepos = profile.Repos.Where(repo => !repo.Fork).ToList();
        var totalStars = ownedRepos.Sum(repo => repo.StargazersCount);
        var totalForks = ownedRepos.Sum(repo => repo.ForksCount);
        var languages = ownedRepos
            .Select(repo => repo.Language)
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Select(language => language!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(language => language, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var distinctLanguages = languages.Count;
        var proficiencies = ownedRepos
            .SelectMany(repo => repo.Topics)
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(topic => topic, StringComparer.OrdinalIgnoreCase)
            .Select(FormatTopicLabel)
            .ToList();
        var signatureRepo = ownedRepos
            .OrderByDescending(repo => repo.StargazersCount)
            .ThenBy(repo => repo.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var cutoff = DateTimeOffset.UtcNow.AddDays(-90);
        var recentCommits = 0;
        var pullRequests = 0;
        var reviews = 0;
        var issues = 0;

        foreach (var gitEvent in profile.Events.Where(evt => evt.CreatedAt >= cutoff))
        {
            switch (gitEvent.Type)
            {
                case "PushEvent":
                    recentCommits += gitEvent.Payload?.Commits?.Count ?? 1;
                    break;
                case "PullRequestEvent":
                    pullRequests++;
                    break;
                case "PullRequestReviewEvent":
                    reviews++;
                    break;
                case "IssuesEvent":
                    issues++;
                    break;
            }
        }

        var accountYears = Math.Max(0, (DateTimeOffset.UtcNow - user.CreatedAt).TotalDays / 365.25);
        var constitutionValue = accountYears * 2 + user.PublicRepos;
        var wisdomValue = pullRequests + reviews + (issues / 2.0);

        var strength = ToAbilityScore(totalStars, 4.2);
        var dexterity = ToAbilityScore(recentCommits, 4.5);
        var constitution = ToAbilityScore(constitutionValue, 3.8);
        var intelligence = ToAbilityScore(distinctLanguages, 5.5);
        var wisdom = ToAbilityScore(wisdomValue, 4.8);
        var charisma = ToAbilityScore(user.Followers, 4.0);

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["STR"] = strength,
            ["DEX"] = dexterity,
            ["CON"] = constitution,
            ["INT"] = intelligence,
            ["WIS"] = wisdom,
            ["CHA"] = charisma
        };

        var dominantStat = ClassPriority
            .OrderByDescending(entry => scores[entry.Stat])
            .ThenBy(entry => ClassPriority.ToList().FindIndex(item => item.Stat == entry.Stat))
            .First()
            .Stat;

        var className = ClassPriority.First(entry => entry.Stat == dominantStat).ClassName;
        var level = CalculateLevel(totalStars, recentCommits, user.PublicRepos, distinctLanguages, wisdomValue, user.Followers);
        var alignment = DetermineAlignment(scores);
        var adventuringReport = BuildAdventuringReport(className, level, dominantStat, scores);

        return new DndCharacter
        {
            Username = user.Login,
            DisplayName = string.IsNullOrWhiteSpace(user.Name) ? user.Login : user.Name,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            AbilityScores = new AbilityScores
            {
                Strength = new AbilityScore { Score = strength },
                Dexterity = new AbilityScore { Score = dexterity },
                Constitution = new AbilityScore { Score = constitution },
                Intelligence = new AbilityScore { Score = intelligence },
                Wisdom = new AbilityScore { Score = wisdom },
                Charisma = new AbilityScore { Score = charisma }
            },
            Class = className,
            Level = level,
            Alignment = alignment,
            AdventuringReport = adventuringReport,
            PublicGists = user.PublicGists,
            Allies = user.Following,
            LanguagesSpoken = languages,
            SignatureWeaponName = signatureRepo?.Name ?? "Unarmed",
            SignatureWeaponStars = signatureRepo?.StargazersCount ?? 0,
            Disciples = totalForks,
            Proficiencies = proficiencies.Take(MaxDisplayedProficiencies).ToList(),
            HiddenProficiencyCount = Math.Max(0, proficiencies.Count - MaxDisplayedProficiencies)
        };
    }

    private static string FormatTopicLabel(string topic) =>
        topic.Replace('-', ' ');

    private static int ToAbilityScore(double value, double scale)
    {
        return (int)Math.Clamp(3 + Math.Round(scale * Math.Log10(value + 1)), 3, 20);
    }

    private static int CalculateLevel(
        int totalStars,
        int recentCommits,
        int publicRepos,
        int distinctLanguages,
        double wisdomValue,
        int followers)
    {
        var activity = totalStars + recentCommits + publicRepos + distinctLanguages + wisdomValue + followers;
        return (int)Math.Clamp(1 + Math.Round(Math.Log10(activity + 1) * 3.2), 1, 20);
    }

    private static string DetermineAlignment(IReadOnlyDictionary<string, int> scores)
    {
        var average = scores.Values.Average();
        var spread = scores.Values.Max() - scores.Values.Min();

        var lawChaos = scores["WIS"] + scores["CON"] >= scores["DEX"] + scores["CHA"]
            ? "Lawful"
            : scores["DEX"] + scores["CHA"] >= scores["WIS"] + scores["CON"] + 4
                ? "Chaotic"
                : "Neutral";

        var goodEvil = scores["STR"] + scores["CHA"] >= scores["INT"] + scores["WIS"]
            ? "Good"
            : scores["INT"] >= scores["STR"] + 3
                ? "Neutral"
                : "Neutral";

        if (spread <= 3 && average >= 12)
        {
            return "True Neutral";
        }

        return $"{lawChaos} {goodEvil}";
    }

    private static string BuildAdventuringReport(
        string className,
        int level,
        string dominantStat,
        IReadOnlyDictionary<string, int> scores)
    {
        var highest = scores.MaxBy(pair => pair.Value);
        var flavor = dominantStat switch
        {
            "STR" => "A steadfast frontline warrior whose strength is measured in contributions and sustained grit.",
            "DEX" => "Fleet-footed coder who darts between branches and commits with practiced ease",
            "CON" => "Hardened veteran of long campaigns, enduring builds, forks, and refactors.",
            "INT" => "Learned sage who collects languages and arcane repo lore.",
            "WIS" => "Field tactician, reading the lay of the project through reviews and PRs.",
            "CHA" => "Charismatic leader who rallies teammates and sparks enthusiasm in the guild.",
            _ => "A versatile adventurer with an unusual stat spread."
        };

        return $"Level {level} {className}. Peak {highest.Key} {highest.Value}. {flavor}";
    }
}
