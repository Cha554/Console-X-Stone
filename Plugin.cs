using BepInEx;

namespace CXS
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        void Start() => CXS.LoadConsole();
    }
}
