#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/PlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

[DeterministicResxAccess(
    """
        <root>
          <data name="Types"><value>{text@string} {optionalText@string?} {flag@bool} {optionalFlag@bool?} {count@int} {optionalCount@int?} {big@long} {optionalBig@long?} {ratio@double} {optionalRatio@double?} {amount@decimal} {optionalAmount@decimal?} {when@DateTime} {optionalWhen@DateTime?} {offset@DateTimeOffset} {optionalOffset@DateTimeOffset?} {id@Guid} {optionalId@Guid?}</value></data>
          <data name="Mixed"><value>{{{name,6}}} {2} {0:N2} {name} {other@string?}</value></data>
        </root>
        """,
    localizedXml: """
        <root>
          <data name="Types"><value>{optionalId} {id} {optionalOffset} {offset} {optionalWhen} {when} {optionalAmount} {amount} {optionalRatio} {ratio} {optionalBig} {big} {optionalCount} {count} {optionalFlag} {flag} {optionalText} {text}</value></data>
          <data name="Mixed"><value>{other} {0:N1} {name,-5} {2} {name}</value></data>
        </root>
        """
)]
internal static class NamedTexts { }
