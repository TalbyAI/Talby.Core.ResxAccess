namespace Consumer.Api;
[DeterministicResxAccess("<root><data name=\"Welcome\"><value> Hello {name} </value></data></root>")]
internal static class GeneratedTexts
{
  private static readonly global::System.Resources.ResourceManager __resxResourceManager = (global::System.Resources.ResourceManager)(new("ConsumerRoot.Resources.Labels", typeof(global::Consumer.Api.GeneratedTexts).Assembly));
  public static global::System.String FormatWelcome(global::System.Object? name)
  {
    var resourceCulture = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    var text = (string)global::Consumer.Api.GeneratedTexts.Welcome(resourceCulture);
    var arguments = new object? [1];
    arguments[0] = name;
    if (text == " Hello {name} ")
    {
      return (global::System.String)string.Format(formattingCulture, " Hello {0} ", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String FormatWelcome(global::System.Object? name, global::System.Globalization.CultureInfo resourceCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    var text = (string)global::Consumer.Api.GeneratedTexts.Welcome(resourceCulture_1);
    var arguments = new object? [1];
    arguments[0] = name;
    if (text == " Hello {name} ")
    {
      return (global::System.String)string.Format(formattingCulture, " Hello {0} ", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String FormatWelcome(global::System.Object? name, global::System.Globalization.CultureInfo resourceCulture, global::System.Globalization.CultureInfo formattingCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture_1 = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    formattingCulture_1 = (global::System.Globalization.CultureInfo)formattingCulture;
    global::System.ArgumentNullException.ThrowIfNull(formattingCulture_1);
    var text = (string)global::Consumer.Api.GeneratedTexts.Welcome(resourceCulture_1);
    var arguments = new object? [1];
    arguments[0] = name;
    if (text == " Hello {name} ")
    {
      return (global::System.String)string.Format(formattingCulture_1, " Hello {0} ", arguments);
    }
    return (global::System.String)string.Format(formattingCulture_1, text, arguments);
  }
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