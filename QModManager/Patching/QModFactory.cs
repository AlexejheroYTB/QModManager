﻿namespace QModManager.Patching
{
    using Oculus.Newtonsoft.Json;
    using QModManager.API;
    using QModManager.DataStructures;
    using QModManager.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class QModFactory : IQModFactory
    {
        internal static readonly ManifestValidator Validator = new ManifestValidator();

        /// <summary>
        /// Searches through all folders in the provided directory and returns an ordered list of mods to load.<para/>
        /// Mods that cannot be loaded will have an unsuccessful <see cref="QMod.Status"/> value.
        /// </summary>
        /// <param name="qmodsDirectory">The QMods directory</param>
        /// <returns>A new, sorted <see cref="List{QMod}"/> ready to be initialized or skipped.</returns>
        public List<QMod> BuildModLoadingList(string qmodsDirectory)
        {
            if (!Directory.Exists(qmodsDirectory))
            {
                Logger.Info("QMods directory was not found! Creating...");
                Directory.CreateDirectory(qmodsDirectory);

                return new List<QMod>(0);
            }

            string[] subDirectories = Directory.GetDirectories(qmodsDirectory);
            var modSorter = new SortedCollection<string, QMod>();
            var earlyErrors = new List<QMod>(subDirectories.Length);

            foreach (string subDir in subDirectories)
            {
                string[] dllFiles = Directory.GetFiles(subDir, "*.dll", SearchOption.TopDirectoryOnly);

                if (dllFiles.Length < 1)
                    continue;

                string jsonFile = Path.Combine(subDir, "mod.json");

                string folderName = new DirectoryInfo(subDir).Name;

                if (!File.Exists(jsonFile))
                {
                    Logger.Error($"Unable to set up mod in folder \"{folderName}\"");
                    earlyErrors.Add(new QModPlaceholder(folderName, ModStatus.MissingCoreInfo));
                    continue;
                }

                QMod mod = CreateFromJsonManifestFile(subDir);

                ModStatus status = Validator.ValidateManifest(mod, subDir);

                if (status != ModStatus.Success)
                {
                    Logger.Debug($"Mod '{mod.Id}' will not be loaded");
                    earlyErrors.Add(mod);
                    continue;
                }

                Logger.Debug($"Sorting mod {mod.Id}");
                bool added = modSorter.AddSorted(mod);
                if (!added)
                {
                    Logger.Debug($"DuplicateId on mod {mod.Id}");
                    mod.Status = ModStatus.DuplicateIdDetected;
                    earlyErrors.Add(mod);
                }
            }

            List<QMod> modsToLoad = modSorter.GetSortedList();

            return CreateModStatusList(earlyErrors, modsToLoad);
        }

        internal static List<QMod> CreateModStatusList(List<QMod> earlyErrors, List<QMod> modsToLoad)
        {
            var modList = new List<QMod>(modsToLoad.Count + earlyErrors.Count);

            foreach (QMod mod in modsToLoad)
            {
                Logger.Debug($"{mod.Id} ready to load");
                modList.Add(mod);
            }

            foreach (QMod erroredMod in earlyErrors)
            {
                Logger.Debug($"{erroredMod.Id} had an early error");
                modList.Add(erroredMod);
            }

            foreach (QMod mod in modList)
            {
                if (mod.Status != ModStatus.Success)
                    continue;

                if (mod.RequiredMods == null)
                    continue;

                foreach (RequiredQMod requiredMod in mod.RequiredMods)
                {
                    QMod dependency = modsToLoad.Find(d => d.Id == requiredMod.Id);

                    if (dependency == null || dependency.Status != ModStatus.Success)
                    {
                        mod.Status = ModStatus.MissingDependency;
                        break;
                    }

                    if (dependency.ParsedVersion < requiredMod.MinimumVersion)
                    {
                        mod.Status = ModStatus.OutOfDateDependency;
                        break;
                    }
                }
            }

            return modList;
        }

        private static QMod CreateFromJsonManifestFile(string subDirectory)
        {
            string jsonFile = Path.Combine(subDirectory, "mod.json");

            if (!File.Exists(jsonFile))
            {
                return null;
            }

            try
            {
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                string jsonText = File.ReadAllText(jsonFile);
                return JsonConvert.DeserializeObject<QMod>(jsonText);
            }
            catch (Exception e)
            {
                Logger.Error($"\"mod.json\" deserialization failed for file \"{jsonFile}\"!");
                Logger.Exception(e);

                return null;
            }
        }
    }
}
