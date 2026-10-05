#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/PlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
using Talby.Core.ResxAccess;

namespace Consumer.Api;

[DeterministicResxAccess(
    """
        <root>
          <data name="a-b"><value>Dash {name@string}</value></data>
          <data name="a b"><value>Space {0}</value></data>
          <data name="a_b"><value>Reserved</value></data>
        </root>
        """,
    localizedXml: """
        <root>
          <data name="a_b"><value>Reservado</value></data>
          <data name="a b"><value>Espacio {0:N2}</value></data>
          <data name="a-b"><value>Guion {name}</value></data>
        </root>
        """,
    InvalidKeyHandling = InvalidKeyHandling.Normalize
)]
internal static class NormalizedTexts { }
