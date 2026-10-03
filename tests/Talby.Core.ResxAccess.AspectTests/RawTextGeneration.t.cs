namespace Consumer.Api;
[DeterministicResxAccess("<root><data name=\"Welcome\"><value> Hello {name} </value></data></root>")]
internal static class GeneratedTexts
{
  private static readonly global::System.Resources.ResourceManager __resxResourceManager = (global::System.Resources.ResourceManager)(new("ConsumerRoot.Resources.Labels", typeof(global::Consumer.Api.GeneratedTexts).Assembly));
  public static global::System.String Welcome(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.GeneratedTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("Welcome", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'Welcome' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'Welcome' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'Welcome' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String Welcome()
  {
    return global::Consumer.Api.GeneratedTexts.Welcome(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
}