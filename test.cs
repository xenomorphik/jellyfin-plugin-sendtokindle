using System;
using System.Reflection;
using System.Linq;

class Program {
    static void Main() {
        var asm = Assembly.LoadFrom("Jellyfin.Plugin.SendToKindle/bin/Release/net8.0/Jellyfin.Plugin.SendToKindle.dll");
        var type = asm.GetType("Jellyfin.Plugin.SendToKindle.Plugin");
        Console.WriteLine(type.BaseType.FullName);
        var pluginInfoMethod = type.GetMethod("GetPluginInfo");
        Console.WriteLine(pluginInfoMethod != null ? pluginInfoMethod.DeclaringType.FullName : "null");
    }
}
