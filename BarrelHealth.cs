using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Barrel Health", "VisEntities", "1.0.0")]
    [Description("Sets custom health for loot barrels.")]
    public class BarrelHealth : RustPlugin
    {
        #region Fields

        private static BarrelHealth _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Barrel Groups")]
            public List<BarrelGroupConfig> BarrelGroups { get; set; }
        }

        private class BarrelGroupConfig
        {
            [JsonProperty("Health")]
            public float Health { get; set; }

            [JsonProperty("Prefabs")]
            public List<string> Prefabs { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                BarrelGroups = new List<BarrelGroupConfig>
                {
                    new BarrelGroupConfig
                    {
                        Health = 50f,
                        Prefabs = new List<string>
                        {
                            "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-2.prefab",
                            "assets/bundled/prefabs/radtown/loot_barrel_1.prefab",
                        }
                    },
                    new BarrelGroupConfig
                    {
                        Health = 35f,
                        Prefabs = new List<string>
                        {
                            "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-1.prefab",
                            "assets/bundled/prefabs/radtown/loot_barrel_2.prefab",
                        }
                    },
                    new BarrelGroupConfig
                    {
                        Health = 50f,
                        Prefabs = new List<string>
                        {
                            "assets/bundled/prefabs/radtown/oil_barrel.prefab"
                        }
                    }
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            Unsubscribe(nameof(OnLootSpawn));
            _plugin = this;
        }

        private void Unload()
        {
            CoroutineUtil.StopAllCoroutines();
            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            Subscribe(nameof(OnLootSpawn));

            if (!isStartup)
                CoroutineUtil.StartCoroutine(Guid.NewGuid().ToString(), UpdateHealthForAllBarrelsCoroutine());
        }

        private void OnLootSpawn(LootContainer container)
        {
            if (container != null && container.PrefabName.Contains("barrel"))
            {
                InitializeBarrelHealth(container);
            }
        }

        #endregion Oxide Hooks

        #region Health Initialization

        private IEnumerator UpdateHealthForAllBarrelsCoroutine()
        {
            foreach (LootContainer container in BaseNetworkable.serverEntities.OfType<LootContainer>())
            {
                OnLootSpawn(container);
                yield return CoroutineEx.waitForEndOfFrame;
            }
        }

        private void InitializeBarrelHealth(LootContainer container)
        {
            foreach (BarrelGroupConfig barrelGroup in _config.BarrelGroups)
            {
                if (barrelGroup.Prefabs.Contains(container.PrefabName))
                {
                    container.InitializeHealth(barrelGroup.Health, barrelGroup.Health);
                    container.SendNetworkUpdateImmediate();
                    break;
                }
            }
        }

        #endregion Health Initialization

        #region Helper Classes

        public static class CoroutineUtil
        {
            private static readonly Dictionary<string, Coroutine> _activeCoroutines = new Dictionary<string, Coroutine>();

            public static void StartCoroutine(string coroutineName, IEnumerator coroutineFunction)
            {
                StopCoroutine(coroutineName);

                Coroutine coroutine = ServerMgr.Instance.StartCoroutine(coroutineFunction);
                _activeCoroutines[coroutineName] = coroutine;
            }

            public static void StopCoroutine(string coroutineName)
            {
                if (_activeCoroutines.TryGetValue(coroutineName, out Coroutine coroutine))
                {
                    if (coroutine != null)
                        ServerMgr.Instance.StopCoroutine(coroutine);

                    _activeCoroutines.Remove(coroutineName);
                }
            }

            public static void StopAllCoroutines()
            {
                foreach (string coroutineName in _activeCoroutines.Keys.ToArray())
                {
                    StopCoroutine(coroutineName);
                }
            }
        }

        #endregion Helper Classes
    }
}