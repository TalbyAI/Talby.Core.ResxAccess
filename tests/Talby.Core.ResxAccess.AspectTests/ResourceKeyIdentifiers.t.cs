// Warning TRESX006 on `IdentifierTexts`: `Resource Key '1Text' in 'Labels.resx' cannot become a C# method identifier; its members are omitted.`
// Warning TRESX006 on `IdentifierTexts`: `Resource Key 'has-dash' in 'Labels.resx' cannot become a C# method identifier; its members are omitted.`
// Warning TRESX006 on `IdentifierTexts`: `Resource Key 'two words' in 'Labels.resx' cannot become a C# method identifier; its members are omitted.`
namespace Consumer.Api;
using Talby.Core.ResxAccess;
[DeterministicResxAccess("""
        <root>
          <data name="record"><value>Contextual keyword</value></data>
          <data name="漢字"><value>Unicode</value></data>
          <data name="áéí"><value>Unicode letters</value></data>
          <data name="has-dash"><value>Invalid identifier</value></data>
          <data name="two words"><value>Invalid identifier</value></data>
          <data name="1Text"><value>Invalid start</value></data>
        </root>
        """)]
internal static class IdentifierTexts
{
  private static readonly global::System.Resources.ResourceManager __resxResourceManager = (global::System.Resources.ResourceManager)(new("ConsumerRoot.Resources.Labels", typeof(global::Consumer.Api.IdentifierTexts).Assembly));
  public static global::System.String record(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.IdentifierTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("record", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'record' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'record' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'record' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String record()
  {
    return global::Consumer.Api.IdentifierTexts.record(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
  public static global::System.String áéí(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.IdentifierTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("áéí", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'áéí' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'áéí' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'áéí' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String áéí()
  {
    return global::Consumer.Api.IdentifierTexts.áéí(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
  public static global::System.String 漢字(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.IdentifierTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("漢字", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key '漢字' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key '漢字' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key '漢字' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String 漢字()
  {
    return global::Consumer.Api.IdentifierTexts.漢字(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
}
[DeterministicResxAccess("<root><data name=\"Plain\"><value>Preserved</value></data><data name=\"invalid-key\"><value>{name@string}</value></data></root>", InvalidKeyHandling = InvalidKeyHandling.Ignore)]
internal static class IgnoredIdentifierTexts
{
  private static readonly global::System.Resources.ResourceManager __resxResourceManager = (global::System.Resources.ResourceManager)(new("ConsumerRoot.Resources.Labels", typeof(global::Consumer.Api.IgnoredIdentifierTexts).Assembly));
  public static global::System.String Plain(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.IgnoredIdentifierTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("Plain", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'Plain' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'Plain' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'Plain' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String Plain()
  {
    return global::Consumer.Api.IgnoredIdentifierTexts.Plain(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
}