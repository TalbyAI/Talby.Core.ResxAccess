using Talby.Core.ResxAccess;

namespace Consumer.Api;

[GenerateResxAccess(null!)]
public static class NullPath { }

[GenerateResxAccess("")]
public static class EmptyPath { }

[GenerateResxAccess(" ")]
public static class WhitespacePath { }

[GenerateResxAccess("Labels.txt")]
public static class WrongExtension { }
