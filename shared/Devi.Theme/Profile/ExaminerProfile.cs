using System.Text.Json.Serialization;

namespace Devi.Theme.Profile;

/// <summary>
/// The examiner profile shared by every DEVI app: entered once, autofilled everywhere.
/// It never holds a case number, evidence paths, or hashes.
/// </summary>
public sealed class ExaminerProfile
{
    public string? ExaminerName { get; set; }

    public string? Agency { get; set; }

    public string? Unit { get; set; }

    public string? TitleOrBadge { get; set; }

    public string? Contact { get; set; }

    [JsonIgnore]
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(ExaminerName) && string.IsNullOrWhiteSpace(Agency) && string.IsNullOrWhiteSpace(Unit)
        && string.IsNullOrWhiteSpace(TitleOrBadge) && string.IsNullOrWhiteSpace(Contact);

    public static ExaminerProfile Load() => DeviLocalStore.Load<ExaminerProfile>(DeviLocalStore.ProfilePath);

    public void Save()
    {
        ExaminerName = Clean(ExaminerName);
        Agency = Clean(Agency);
        Unit = Clean(Unit);
        TitleOrBadge = Clean(TitleOrBadge);
        Contact = Clean(Contact);
        DeviLocalStore.Save(DeviLocalStore.ProfilePath, this);
    }

    public static void Clear() => DeviLocalStore.Delete(DeviLocalStore.ProfilePath);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
