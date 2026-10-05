using Talby.Core.ResxAccess;
namespace Consumer.Api;
[DeterministicResxAccess("""
    <root>
      <data name="a-b"><value>Dash {name@string}</value></data>
      <data name="a b"><value>Space {0}</value></data>
      <data name="a_b"><value>Reserved</value></data>
    </root>
    """, localizedXml: """
    <root>
      <data name="a_b"><value>Reservado</value></data>
      <data name="a b"><value>Espacio {0:N2}</value></data>
      <data name="a-b"><value>Guion {name}</value></data>
    </root>
    """, InvalidKeyHandling = InvalidKeyHandling.Normalize)]
internal static class NormalizedTexts
{
  private static readonly global::System.Resources.ResourceManager __resxResourceManager = (global::System.Resources.ResourceManager)(new("ConsumerRoot.Resources.Labels", typeof(global::Consumer.Api.NormalizedTexts).Assembly));
  public static global::System.String Formata_b_2(global::System.Object? arg0)
  {
    var resourceCulture = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    var text = (string)global::Consumer.Api.NormalizedTexts.a_b_2(resourceCulture);
    var arguments = new object? [1];
    arguments[0] = arg0;
    if (text == "Space {0}")
    {
      return (global::System.String)string.Format(formattingCulture, "Space {0}", arguments);
    }
    if (text == "Espacio {0:N2}")
    {
      return (global::System.String)string.Format(formattingCulture, "Espacio {0:N2}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String Formata_b_2(global::System.Object? arg0, global::System.Globalization.CultureInfo resourceCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    var text = (string)global::Consumer.Api.NormalizedTexts.a_b_2(resourceCulture_1);
    var arguments = new object? [1];
    arguments[0] = arg0;
    if (text == "Space {0}")
    {
      return (global::System.String)string.Format(formattingCulture, "Space {0}", arguments);
    }
    if (text == "Espacio {0:N2}")
    {
      return (global::System.String)string.Format(formattingCulture, "Espacio {0:N2}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String Formata_b_2(global::System.Object? arg0, global::System.Globalization.CultureInfo resourceCulture, global::System.Globalization.CultureInfo formattingCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture_1 = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    formattingCulture_1 = (global::System.Globalization.CultureInfo)formattingCulture;
    global::System.ArgumentNullException.ThrowIfNull(formattingCulture_1);
    var text = (string)global::Consumer.Api.NormalizedTexts.a_b_2(resourceCulture_1);
    var arguments = new object? [1];
    arguments[0] = arg0;
    if (text == "Space {0}")
    {
      return (global::System.String)string.Format(formattingCulture_1, "Space {0}", arguments);
    }
    if (text == "Espacio {0:N2}")
    {
      return (global::System.String)string.Format(formattingCulture_1, "Espacio {0:N2}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture_1, text, arguments);
  }
  public static global::System.String Formata_b_3(global::System.String name)
  {
    var resourceCulture = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    var text = (string)global::Consumer.Api.NormalizedTexts.a_b_3(resourceCulture);
    var arguments = new object? [1];
    arguments[0] = name;
    if (text == "Dash {name@string}")
    {
      return (global::System.String)string.Format(formattingCulture, "Dash {0}", arguments);
    }
    if (text == "Guion {name}")
    {
      return (global::System.String)string.Format(formattingCulture, "Guion {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String Formata_b_3(global::System.String name, global::System.Globalization.CultureInfo resourceCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    var text = (string)global::Consumer.Api.NormalizedTexts.a_b_3(resourceCulture_1);
    var arguments = new object? [1];
    arguments[0] = name;
    if (text == "Dash {name@string}")
    {
      return (global::System.String)string.Format(formattingCulture, "Dash {0}", arguments);
    }
    if (text == "Guion {name}")
    {
      return (global::System.String)string.Format(formattingCulture, "Guion {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String Formata_b_3(global::System.String name, global::System.Globalization.CultureInfo resourceCulture, global::System.Globalization.CultureInfo formattingCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture_1 = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    formattingCulture_1 = (global::System.Globalization.CultureInfo)formattingCulture;
    global::System.ArgumentNullException.ThrowIfNull(formattingCulture_1);
    var text = (string)global::Consumer.Api.NormalizedTexts.a_b_3(resourceCulture_1);
    var arguments = new object? [1];
    arguments[0] = name;
    if (text == "Dash {name@string}")
    {
      return (global::System.String)string.Format(formattingCulture_1, "Dash {0}", arguments);
    }
    if (text == "Guion {name}")
    {
      return (global::System.String)string.Format(formattingCulture_1, "Guion {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture_1, text, arguments);
  }
  public static global::System.String a_b(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.NormalizedTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("a_b", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'a_b' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'a_b' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'a_b' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String a_b()
  {
    return global::Consumer.Api.NormalizedTexts.a_b(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
  public static global::System.String a_b_2(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.NormalizedTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("a b", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'a b' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'a b' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'a b' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String a_b_2()
  {
    return global::Consumer.Api.NormalizedTexts.a_b_2(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
  public static global::System.String a_b_3(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.NormalizedTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("a-b", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'a-b' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'a-b' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'a-b' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String a_b_3()
  {
    return global::Consumer.Api.NormalizedTexts.a_b_3(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
}