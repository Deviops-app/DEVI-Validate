using System.Collections.Generic;

namespace DeviValidate.Core.IO;

public sealed record EvidenceScan(string RootPath, IReadOnlyList<EvidenceFile> Files, IReadOnlyList<SkippedEntry> Skipped);
