using Metalama.Testing.UnitTesting;

namespace Talby.Core.ResxAccess.Tests;

public class MetalamaSetupTests : UnitTestClass
{
    [Fact]
    public void CanCreateAndQueryCompilation()
    {
        using var context = CreateTestContext();
        var compilation = context.CreateCompilation("public class Sample { }");

        Assert.Equal("Sample", Assert.Single(compilation.Types).Name);
    }
}
