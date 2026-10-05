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
          <data name="record"><value>Contextual keyword</value></data>
          <data name="漢字"><value>Unicode</value></data>
          <data name="áéí"><value>Unicode letters</value></data>
          <data name="has-dash"><value>Invalid identifier</value></data>
          <data name="two words"><value>Invalid identifier</value></data>
          <data name="1Text"><value>Invalid start</value></data>
        </root>
        """
)]
internal static class IdentifierTexts { }
