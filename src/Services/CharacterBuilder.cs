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
        var activity = totalStars + recentCommits + user.PublicRepos + distinctLanguages + wisdomValue + user.Followers;
        var help = BuildHelpInfo(
            totalStars,
            recentCommits,
            accountYears,
            user.PublicRepos,
            constitutionValue,
            distinctLanguages,
            pullRequests,
            reviews,
            issues,
            wisdomValue,
            user.Followers,
            user.PublicGists,
            user.Following,
            totalForks,
            languages.Count,
            proficiencies.Count,
            signatureRepo,
            strength,
            dexterity,
            constitution,
            intelligence,
            wisdom,
            charisma,
            dominantStat,
            className,
            level,
            alignment,
            scores,
            activity);

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
            HiddenProficiencyCount = Math.Max(0, proficiencies.Count - MaxDisplayedProficiencies),
            Help = help
        };
    }

    private static CharacterHelpInfo BuildHelpInfo(
        int totalStars,
        int recentCommits,
        double accountYears,
        int publicRepos,
        double constitutionValue,
        int distinctLanguages,
        int pullRequests,
        int reviews,
        int issues,
        double wisdomValue,
        int followers,
        int publicGists,
        int following,
        int totalForks,
        int languageCount,
        int proficiencyCount,
        GitHubRepo? signatureRepo,
        int strength,
        int dexterity,
        int constitution,
        int intelligence,
        int wisdom,
        int charisma,
        string dominantStat,
        string className,
        int level,
        string alignment,
        IReadOnlyDictionary<string, int> scores,
        double activity) =>
        new()
        {
            StrengthHelp = $"Based on {totalStars} total stars on owned (non-fork) repos.",
            DexterityHelp = $"Based on {recentCommits} commits/pushes from public events in the last 90 days.",
            ConstitutionHelp = $"Based on account age ({accountYears:F1} years) and public repos ({publicRepos}).",
            IntelligenceHelp = $"Based on {distinctLanguages} distinct languages across owned repos.",
            WisdomHelp = $"Based on {pullRequests} pull requests, {reviews} reviews, and {issues} issue events in the last 90 days (issues count at half weight → {wisdomValue:F0}).",
            CharismaHelp = $"Based on {followers} followers.",
            ClassHelp = $"Highest ability score is {dominantStat} ({scores[dominantStat]}). That maps to {className}. STR: Barbarian, DEX: Rogue, CON: Paladin, INT: Wizard, WIS: Cleric, CHA: Bard.",
            LevelHelp = $"Overall activity ({activity:F0}).",
            AlignmentHelp = $"Inferred from stat spread: average {scores.Values.Average():F1}, spread {scores.Values.Max() - scores.Values.Min()}. Result: {alignment}.",
            PublicGistsHelp = $"Your public gist count from GitHub → {publicGists}.",
            AlliesHelp = $"Your following count (accounts you follow) → {following}.",
            DisciplesHelp = $"Total forks across owned repos → {totalForks}.",
            LanguagesSpokenHelp = languageCount == 0 ? "No languages recorded on owned repos." : $"{languageCount} distinct languages across owned repos.",
            SignatureWeaponHelp = signatureRepo is null ? "No owned repos found, so no signature weapon is recorded." : $"Top owned repo by stars: {signatureRepo.Name} - {signatureRepo.StargazersCount} stars.",
            ProficienciesHelp = proficiencyCount == 0 ? "No topics tagged on owned repos." : $"{proficiencyCount} distinct repo topics.",
            AdventuringReportHelp = $"Summary from level, class, dominant stat ({dominantStat}), and play-style flavor."
        };

    private static string FormatTopicLabel(string topic) => topic.Replace('-', ' ');

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
