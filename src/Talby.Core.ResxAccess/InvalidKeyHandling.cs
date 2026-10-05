using Metalama.Framework.Aspects;

namespace Talby.Core.ResxAccess;

/// <summary>Controls generation for Resource Keys that are not C# identifiers.</summary>
[RunTimeOrCompileTime]
public enum InvalidKeyHandling
{
    /// <summary>Omits invalid Resource Key members and reports identifier warnings.</summary>
    Warn,

    /// <summary>Omits invalid Resource Key members without identifier warnings.</summary>
    Ignore,

    /// <summary>Replaces invalid identifier characters with underscores and resolves normalization collisions.</summary>
    Normalize,
}
