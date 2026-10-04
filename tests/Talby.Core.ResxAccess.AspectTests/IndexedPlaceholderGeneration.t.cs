namespace Consumer.Api;
[DeterministicResxAccess("<root><data name=\"Summary\"><value>{2} / {0:N2} / {2}</value></data><data name=\"Literal\"><value>{{0}}</value></data></root>")]
internal static class IndexedTexts
{
  private static readonly global::System.Resources.ResourceManager __resxResourceManager = (global::System.Resources.ResourceManager)(new("ConsumerRoot.Resources.Labels", typeof(global::Consumer.Api.IndexedTexts).Assembly));
  public static global::System.String FormatSummary(global::System.Object? arg0, global::System.Object? arg2)
  {
    var resourceCulture = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    var text = (string)global::Consumer.Api.IndexedTexts.Summary(resourceCulture);
    var arguments = new object? [3];
    arguments[0] = arg0;
    arguments[2] = arg2;
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String FormatSummary(global::System.Object? arg0, global::System.Object? arg2, global::System.Globalization.CultureInfo resourceCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    var text = (string)global::Consumer.Api.IndexedTexts.Summary(resourceCulture_1);
    var arguments = new object? [3];
    arguments[0] = arg0;
    arguments[2] = arg2;
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String FormatSummary(global::System.Object? arg0, global::System.Object? arg2, global::System.Globalization.CultureInfo resourceCulture, global::System.Globalization.CultureInfo formattingCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture_1 = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    formattingCulture_1 = (global::System.Globalization.CultureInfo)formattingCulture;
    global::System.ArgumentNullException.ThrowIfNull(formattingCulture_1);
    var text = (string)global::Consumer.Api.IndexedTexts.Summary(resourceCulture_1);
    var arguments = new object? [3];
    arguments[0] = arg0;
    arguments[2] = arg2;
    return (global::System.String)string.Format(formattingCulture_1, text, arguments);
  }
  public static global::System.String Literal(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.IndexedTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("Literal", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'Literal' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'Literal' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'Literal' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String Literal()
  {
    return global::Consumer.Api.IndexedTexts.Literal(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
  public static global::System.String Summary(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.IndexedTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("Summary", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'Summary' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'Summary' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'Summary' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String Summary()
  {
    return global::Consumer.Api.IndexedTexts.Summary(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
}