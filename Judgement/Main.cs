using BepInEx;
using RoR2;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Judgement
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "Judgement";
    public const string PluginVersion = "1.5.1";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }

    public void Awake()
    {
      Instance = this;

      Stopwatch stopwatch = Stopwatch.StartNew();

      Log.Init(Logger);

      new CreateGameMode();
      new RunHooks();
      new SimulacrumHooks();
      new EntityStateHooks();

      stopwatch.Stop();
      Log.Info_NoCallerPrefix($"Judgement: Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

  }
}