using BepInEx;
using System.Diagnostics;
using System.IO;

namespace Judgement
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "Judgement";
    public const string PluginVersion = "1.5.0";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }

    public void Awake()
    {
      Instance = this;
      
      Stopwatch stopwatch = Stopwatch.StartNew();

      Log.Init(Logger);

      PluginDirectory = Path.GetDirectoryName(Info.Location);
      LanguageFolderHandler.Register(PluginDirectory);

      new CreateGameMode();
      new BazaarHooks();
      
      stopwatch.Stop();
      Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

  }
}