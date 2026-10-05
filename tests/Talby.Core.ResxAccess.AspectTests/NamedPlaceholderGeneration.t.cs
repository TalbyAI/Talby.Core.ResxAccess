namespace Consumer.Api;
[DeterministicResxAccess("""
        <root>
          <data name="Types"><value>{text@string} {optionalText@string?} {flag@bool} {optionalFlag@bool?} {count@int} {optionalCount@int?} {big@long} {optionalBig@long?} {ratio@double} {optionalRatio@double?} {amount@decimal} {optionalAmount@decimal?} {when@DateTime} {optionalWhen@DateTime?} {offset@DateTimeOffset} {optionalOffset@DateTimeOffset?} {id@Guid} {optionalId@Guid?}</value></data>
          <data name="Mixed"><value>{{{name,6}}} {2} {0:N2} {name} {other@string?}</value></data>
        </root>
        """, localizedXml: """
        <root>
          <data name="Types"><value>{optionalId} {id} {optionalOffset} {offset} {optionalWhen} {when} {optionalAmount} {amount} {optionalRatio} {ratio} {optionalBig} {big} {optionalCount} {count} {optionalFlag} {flag} {optionalText} {text}</value></data>
          <data name="Mixed"><value>{other} {0:N1} {name,-5} {2} {name}</value></data>
        </root>
        """)]
internal static class NamedTexts
{
  private static readonly global::System.Resources.ResourceManager __resxResourceManager = (global::System.Resources.ResourceManager)(new("ConsumerRoot.Resources.Labels", typeof(global::Consumer.Api.NamedTexts).Assembly));
  public static global::System.String FormatMixed(global::System.Object? name, global::System.String? other, global::System.Object? arg0, global::System.Object? arg2)
  {
    var resourceCulture = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    var text = (string)global::Consumer.Api.NamedTexts.Mixed(resourceCulture);
    var arguments = new object? [4];
    arguments[0] = name;
    arguments[1] = other;
    arguments[2] = arg0;
    arguments[3] = arg2;
    if (text == "{{{name,6}}} {2} {0:N2} {name} {other@string?}")
    {
      return (global::System.String)string.Format(formattingCulture, "{{{0,6}}} {3} {2:N2} {0} {1}", arguments);
    }
    if (text == "{other} {0:N1} {name,-5} {2} {name}")
    {
      return (global::System.String)string.Format(formattingCulture, "{1} {2:N1} {0,-5} {3} {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String FormatMixed(global::System.Object? name, global::System.String? other, global::System.Object? arg0, global::System.Object? arg2, global::System.Globalization.CultureInfo resourceCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    var text = (string)global::Consumer.Api.NamedTexts.Mixed(resourceCulture_1);
    var arguments = new object? [4];
    arguments[0] = name;
    arguments[1] = other;
    arguments[2] = arg0;
    arguments[3] = arg2;
    if (text == "{{{name,6}}} {2} {0:N2} {name} {other@string?}")
    {
      return (global::System.String)string.Format(formattingCulture, "{{{0,6}}} {3} {2:N2} {0} {1}", arguments);
    }
    if (text == "{other} {0:N1} {name,-5} {2} {name}")
    {
      return (global::System.String)string.Format(formattingCulture, "{1} {2:N1} {0,-5} {3} {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text, arguments);
  }
  public static global::System.String FormatMixed(global::System.Object? name, global::System.String? other, global::System.Object? arg0, global::System.Object? arg2, global::System.Globalization.CultureInfo resourceCulture, global::System.Globalization.CultureInfo formattingCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture_1 = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    formattingCulture_1 = (global::System.Globalization.CultureInfo)formattingCulture;
    global::System.ArgumentNullException.ThrowIfNull(formattingCulture_1);
    var text = (string)global::Consumer.Api.NamedTexts.Mixed(resourceCulture_1);
    var arguments = new object? [4];
    arguments[0] = name;
    arguments[1] = other;
    arguments[2] = arg0;
    arguments[3] = arg2;
    if (text == "{{{name,6}}} {2} {0:N2} {name} {other@string?}")
    {
      return (global::System.String)string.Format(formattingCulture_1, "{{{0,6}}} {3} {2:N2} {0} {1}", arguments);
    }
    if (text == "{other} {0:N1} {name,-5} {2} {name}")
    {
      return (global::System.String)string.Format(formattingCulture_1, "{1} {2:N1} {0,-5} {3} {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture_1, text, arguments);
  }
  public static global::System.String FormatTypes(global::System.String text, global::System.String? optionalText, global::System.Boolean flag, global::System.Boolean? optionalFlag, global::System.Int32 count, global::System.Int32? optionalCount, global::System.Int64 big, global::System.Int64? optionalBig, global::System.Double ratio, global::System.Double? optionalRatio, global::System.Decimal amount, global::System.Decimal? optionalAmount, global::System.DateTime when, global::System.DateTime? optionalWhen, global::System.DateTimeOffset offset, global::System.DateTimeOffset? optionalOffset, global::System.Guid id, global::System.Guid? optionalId)
  {
    var resourceCulture = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    var text_1 = (string)global::Consumer.Api.NamedTexts.Types(resourceCulture);
    var arguments = new object? [18];
    arguments[0] = text;
    arguments[1] = optionalText;
    arguments[2] = flag;
    arguments[3] = optionalFlag;
    arguments[4] = count;
    arguments[5] = optionalCount;
    arguments[6] = big;
    arguments[7] = optionalBig;
    arguments[8] = ratio;
    arguments[9] = optionalRatio;
    arguments[10] = amount;
    arguments[11] = optionalAmount;
    arguments[12] = when;
    arguments[13] = optionalWhen;
    arguments[14] = offset;
    arguments[15] = optionalOffset;
    arguments[16] = id;
    arguments[17] = optionalId;
    if (text_1 == "{text@string} {optionalText@string?} {flag@bool} {optionalFlag@bool?} {count@int} {optionalCount@int?} {big@long} {optionalBig@long?} {ratio@double} {optionalRatio@double?} {amount@decimal} {optionalAmount@decimal?} {when@DateTime} {optionalWhen@DateTime?} {offset@DateTimeOffset} {optionalOffset@DateTimeOffset?} {id@Guid} {optionalId@Guid?}")
    {
      return (global::System.String)string.Format(formattingCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17}", arguments);
    }
    if (text_1 == "{optionalId} {id} {optionalOffset} {offset} {optionalWhen} {when} {optionalAmount} {amount} {optionalRatio} {ratio} {optionalBig} {big} {optionalCount} {count} {optionalFlag} {flag} {optionalText} {text}")
    {
      return (global::System.String)string.Format(formattingCulture, "{17} {16} {15} {14} {13} {12} {11} {10} {9} {8} {7} {6} {5} {4} {3} {2} {1} {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text_1, arguments);
  }
  public static global::System.String FormatTypes(global::System.String text, global::System.String? optionalText, global::System.Boolean flag, global::System.Boolean? optionalFlag, global::System.Int32 count, global::System.Int32? optionalCount, global::System.Int64 big, global::System.Int64? optionalBig, global::System.Double ratio, global::System.Double? optionalRatio, global::System.Decimal amount, global::System.Decimal? optionalAmount, global::System.DateTime when, global::System.DateTime? optionalWhen, global::System.DateTimeOffset offset, global::System.DateTimeOffset? optionalOffset, global::System.Guid id, global::System.Guid? optionalId, global::System.Globalization.CultureInfo resourceCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    var text_1 = (string)global::Consumer.Api.NamedTexts.Types(resourceCulture_1);
    var arguments = new object? [18];
    arguments[0] = text;
    arguments[1] = optionalText;
    arguments[2] = flag;
    arguments[3] = optionalFlag;
    arguments[4] = count;
    arguments[5] = optionalCount;
    arguments[6] = big;
    arguments[7] = optionalBig;
    arguments[8] = ratio;
    arguments[9] = optionalRatio;
    arguments[10] = amount;
    arguments[11] = optionalAmount;
    arguments[12] = when;
    arguments[13] = optionalWhen;
    arguments[14] = offset;
    arguments[15] = optionalOffset;
    arguments[16] = id;
    arguments[17] = optionalId;
    if (text_1 == "{text@string} {optionalText@string?} {flag@bool} {optionalFlag@bool?} {count@int} {optionalCount@int?} {big@long} {optionalBig@long?} {ratio@double} {optionalRatio@double?} {amount@decimal} {optionalAmount@decimal?} {when@DateTime} {optionalWhen@DateTime?} {offset@DateTimeOffset} {optionalOffset@DateTimeOffset?} {id@Guid} {optionalId@Guid?}")
    {
      return (global::System.String)string.Format(formattingCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17}", arguments);
    }
    if (text_1 == "{optionalId} {id} {optionalOffset} {offset} {optionalWhen} {when} {optionalAmount} {amount} {optionalRatio} {ratio} {optionalBig} {big} {optionalCount} {count} {optionalFlag} {flag} {optionalText} {text}")
    {
      return (global::System.String)string.Format(formattingCulture, "{17} {16} {15} {14} {13} {12} {11} {10} {9} {8} {7} {6} {5} {4} {3} {2} {1} {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture, text_1, arguments);
  }
  public static global::System.String FormatTypes(global::System.String text, global::System.String? optionalText, global::System.Boolean flag, global::System.Boolean? optionalFlag, global::System.Int32 count, global::System.Int32? optionalCount, global::System.Int64 big, global::System.Int64? optionalBig, global::System.Double ratio, global::System.Double? optionalRatio, global::System.Decimal amount, global::System.Decimal? optionalAmount, global::System.DateTime when, global::System.DateTime? optionalWhen, global::System.DateTimeOffset offset, global::System.DateTimeOffset? optionalOffset, global::System.Guid id, global::System.Guid? optionalId, global::System.Globalization.CultureInfo resourceCulture, global::System.Globalization.CultureInfo formattingCulture)
  {
    var resourceCulture_1 = global::System.Globalization.CultureInfo.CurrentUICulture;
    var formattingCulture_1 = global::System.Globalization.CultureInfo.CurrentCulture;
    resourceCulture_1 = (global::System.Globalization.CultureInfo)resourceCulture;
    formattingCulture_1 = (global::System.Globalization.CultureInfo)formattingCulture;
    global::System.ArgumentNullException.ThrowIfNull(formattingCulture_1);
    var text_1 = (string)global::Consumer.Api.NamedTexts.Types(resourceCulture_1);
    var arguments = new object? [18];
    arguments[0] = text;
    arguments[1] = optionalText;
    arguments[2] = flag;
    arguments[3] = optionalFlag;
    arguments[4] = count;
    arguments[5] = optionalCount;
    arguments[6] = big;
    arguments[7] = optionalBig;
    arguments[8] = ratio;
    arguments[9] = optionalRatio;
    arguments[10] = amount;
    arguments[11] = optionalAmount;
    arguments[12] = when;
    arguments[13] = optionalWhen;
    arguments[14] = offset;
    arguments[15] = optionalOffset;
    arguments[16] = id;
    arguments[17] = optionalId;
    if (text_1 == "{text@string} {optionalText@string?} {flag@bool} {optionalFlag@bool?} {count@int} {optionalCount@int?} {big@long} {optionalBig@long?} {ratio@double} {optionalRatio@double?} {amount@decimal} {optionalAmount@decimal?} {when@DateTime} {optionalWhen@DateTime?} {offset@DateTimeOffset} {optionalOffset@DateTimeOffset?} {id@Guid} {optionalId@Guid?}")
    {
      return (global::System.String)string.Format(formattingCulture_1, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17}", arguments);
    }
    if (text_1 == "{optionalId} {id} {optionalOffset} {offset} {optionalWhen} {when} {optionalAmount} {amount} {optionalRatio} {ratio} {optionalBig} {big} {optionalCount} {count} {optionalFlag} {flag} {optionalText} {text}")
    {
      return (global::System.String)string.Format(formattingCulture_1, "{17} {16} {15} {14} {13} {12} {11} {10} {9} {8} {7} {6} {5} {4} {3} {2} {1} {0}", arguments);
    }
    return (global::System.String)string.Format(formattingCulture_1, text_1, arguments);
  }
  public static global::System.String Mixed(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.NamedTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("Mixed", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'Mixed' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'Mixed' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'Mixed' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String Mixed()
  {
    return global::Consumer.Api.NamedTexts.Mixed(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
  public static global::System.String Types(global::System.Globalization.CultureInfo resourceCulture)
  {
    global::System.ArgumentNullException.ThrowIfNull(resourceCulture);
    var resourceManager = (global::System.Resources.ResourceManager)global::Consumer.Api.NamedTexts.__resxResourceManager;
    try
    {
      return (global::System.String)(resourceManager.GetString("Types", resourceCulture) ?? throw new global::System.InvalidOperationException($"Resource Key 'Types' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."));
    }
    catch (global::System.Resources.MissingManifestResourceException exception)
    {
      throw new global::System.InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key 'Types' and Resource Culture '{resourceCulture.Name}'.", exception);
    }
    catch (global::System.Resources.MissingSatelliteAssemblyException exception_1)
    {
      throw new global::System.InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key 'Types' and Resource Culture '{resourceCulture.Name}'.", exception_1);
    }
  }
  public static global::System.String Types()
  {
    return global::Consumer.Api.NamedTexts.Types(global::System.Globalization.CultureInfo.CurrentUICulture);
  }
}