using System.Globalization;
using System.Resources;

if (args.Length == 1 && args[0] == "culture-casing")
{
    Console.WriteLine(Customer.Api.CanonicalTexts.Plain(CultureInfo.GetCultureInfo("es-MX")));
    Console.WriteLine(Customer.Api.LowercaseTexts.Plain(CultureInfo.GetCultureInfo("es-MX")));
    return;
}

if (args.Length == 1 && args[0] == "localized")
{
    var originalResourceCulture = CultureInfo.CurrentUICulture;
    var originalFormattingCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-MX");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Console.WriteLine(Customer.Api.LocalizedTexts.Plain());
        Console.WriteLine(Customer.Api.LocalizedTexts.Plain(CultureInfo.GetCultureInfo("es-AR")));
        Console.WriteLine(Customer.Api.LocalizedTexts.Plain(CultureInfo.GetCultureInfo("fr")));
        Console.WriteLine(Customer.Api.LocalizedTexts.Plain(CultureInfo.GetCultureInfo("de-DE")));
        Console.WriteLine(Customer.Api.LocalizedTexts.Plain(CultureInfo.InvariantCulture));
        Console.WriteLine(Customer.Api.LocalizedTexts.Raw(CultureInfo.GetCultureInfo("es")));
        Console.WriteLine($"[{Customer.Api.LocalizedTexts.Empty()}]");
        Console.WriteLine($"[{Customer.Api.LocalizedTexts.Blank()}]");
        Console.WriteLine($"[{Customer.Api.LocalizedTexts.Empty(CultureInfo.InvariantCulture)}]");
        Console.WriteLine($"[{Customer.Api.LocalizedTexts.Blank(CultureInfo.InvariantCulture)}]");
    }
    finally
    {
        CultureInfo.CurrentUICulture = originalResourceCulture;
        CultureInfo.CurrentCulture = originalFormattingCulture;
    }
    return;
}

if (args.Length > 0)
{
    var missingManifest = args.Single() switch
    {
        "missing-manifest" => true,
        "missing-key" => false,
        _ => throw new ArgumentException("Unknown runtime scenario."),
    };
    var manifestBaseName = missingManifest
        ? "ConsumerRoot.Resources.MissingManifest"
        : "ConsumerRoot.Resources.Labels";
    try
    {
        if (missingManifest)
        {
            Customer.Api.MissingManifestTexts.Plain(CultureInfo.GetCultureInfo("de-DE"));
        }
        else
        {
            Customer.Api.MissingKeyTexts.Plain(CultureInfo.GetCultureInfo("de-DE"));
        }
        throw new Exception("Expected failure.");
    }
    catch (InvalidOperationException exception)
    {
        if (
            !exception.Message.Contains("Plain")
            || !exception.Message.Contains(manifestBaseName)
            || !exception.Message.Contains("de-DE")
        )
        {
            throw;
        }
        if (missingManifest && exception.InnerException is not MissingManifestResourceException)
        {
            throw;
        }
        Console.WriteLine("Descriptive failure");
    }
    return;
}

var originalCulture = CultureInfo.CurrentCulture;
var originalUICulture = CultureInfo.CurrentUICulture;
try
{
    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
    Console.WriteLine(Customer.Api.Texts.Welcome());
    Console.WriteLine(Customer.Api.Texts.Welcome(CultureInfo.InvariantCulture));
    Console.WriteLine(Customer.Api.Texts.Plain());
    var type = typeof(Customer.Api.Texts);
    if (type.IsPublic || type.FullName != "Customer.Api.Texts")
    {
        throw new Exception("Class identity changed.");
    }
    var methods = type.GetMethods(
        System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.DeclaredOnly
    );
    if (methods.Length != 4 || methods.Any(m => m.Name.StartsWith("Format")))
    {
        throw new Exception("Unexpected API.");
    }

    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-MX");
    Console.WriteLine(Customer.Api.AssociatedTexts.Plain());
    Console.WriteLine(Customer.Api.AssociatedTexts.Plain(CultureInfo.GetCultureInfo("fr-CA")));
    Console.WriteLine(Customer.Api.AssociatedTexts.Plain(CultureInfo.GetCultureInfo("de-DE")));
    Console.WriteLine(Customer.Api.AssociatedTexts.Welcome(CultureInfo.InvariantCulture));
    if (!typeof(Customer.Api.AssociatedTexts).IsPublic)
    {
        throw new Exception("Accessibility changed.");
    }
    try
    {
        Customer.Api.AssociatedTexts.Plain(null!);
        throw new Exception("Null culture accepted.");
    }
    catch (ArgumentNullException) { }

    CultureInfo.CurrentCulture = originalCulture;
    CultureInfo.CurrentUICulture = originalUICulture;
    if (
        Customer.Api.EdgeTexts.Plain() != "Edge text"
        || Customer.Api.EdgeTexts.@class() != "Keyword"
        || Customer.Api.EdgeTexts.Café() != "Unicode"
        || Customer.Api.EdgeTexts.Empty() != ""
        || Customer.Api.EdgeTexts.Blank() != "   "
    )
    {
        throw new Exception("Raw Text changed.");
    }
    Console.WriteLine("Preserved");
}
finally
{
    CultureInfo.CurrentCulture = originalCulture;
    CultureInfo.CurrentUICulture = originalUICulture;
}
