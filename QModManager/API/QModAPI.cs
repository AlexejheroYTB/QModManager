﻿using QModManager.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace QModManager.API
{
    public partial class QModAPI : IQModAPI
    {
        /// <summary>
        /// The main instance of this class <para/>
        /// Used for dependency injection
        /// </summary>
        public static IQModAPI Main { get; } = new QModAPI();
        private QModAPI() { }

        internal static List<Assembly> ErroredMods = new List<Assembly>();

        #region Static

        /// <summary>
        /// Marks a mod as errored <para/>
        /// The mod will appear in the red pop-up which is shows when the game starts
        /// </summary>
        /// <param name="modAssembly">
        /// The assembly of the mod to mark as errored. <para/>
        /// If omitted, it will use the calling <see cref="Assembly"/>
        /// </param>
        public static void MarkAsErrored(Assembly modAssembly = null)
            => Main.MarkAsErrored(modAssembly);

        /// <summary>
        /// Returns a list of all of the mods
        /// </summary>
        /// <param name="includeUnloaded">Set to <see langword="true"/> to also include unloaded mods</param>
        /// <param name="includeErrored">Set to <see langword="true"/> to also include errored mods</param>
        /// <returns>A read only list of mods containing all of the loaded mods, and optionally unloaded/errored mods</returns>
        public static ReadOnlyCollection<IQMod> GetAllMods(bool includeUnloaded = false, bool includeErrored = false)
            => Main.GetAllMods(includeUnloaded, includeErrored);

        /// <summary>
        /// Returns the mod from the assembly which called this method
        /// </summary>
        public static IQMod GetMyMod()
            => Main.GetMyMod();

        /// <summary>
        /// Returns a mod from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        public static IQMod GetMod(Assembly modAssembly, bool includeUnloaded = false, bool includeErrored = false)
            => Main.GetMod(modAssembly, includeUnloaded, includeErrored);

        /// <summary>
        /// Returns a mod from an ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        public static IQMod GetMod(string id, bool includeUnloaded = false, bool includeErrored = false)
            => Main.GetMod(id, includeUnloaded, includeErrored);

        /// <summary>
        /// Checks whether or not a mod is present based on its <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        public static bool ModPresent(Assembly modAssembly, bool includeUnloaded = false, bool includeErrored = false)
            => Main.ModPresent(modAssembly, includeUnloaded, includeErrored);

        /// <summary>
        /// Checks whether or not a mod is present based on its ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        public static bool ModPresent(string id, bool includeUnloaded = false, bool includeErrored = false)
            => Main.ModPresent(id, includeUnloaded, includeErrored);

        #endregion

        /// <summary>
        /// Marks a mod as errored <para/>
        /// The mod will appear in the red pop-up which is shows when the game starts
        /// </summary>
        void IQModAPI.MarkAsErrored()
            => MarkAsErrored(null);
        /// <summary>
        /// Marks a mod as errored <para/>
        /// The mod will appear in the red pop-up which is shows when the game starts
        /// </summary>
        /// <param name="modAssembly">
        /// The assembly of the mod to mark as errored. <para/>
        /// </param>
        void IQModAPI.MarkAsErrored(Assembly modAssembly)
        {
            modAssembly = modAssembly ?? ReflectionHelper.CallingAssemblyByStackTrace();

            if (ErroredMods.Contains(modAssembly)) return;

            ErroredMods.Add(modAssembly);
        }

        /// <summary>
        /// Returns a list of all of the mods
        /// </summary>
        /// <returns>A read only list of mods containing all of the loaded mods</returns>
        ReadOnlyCollection<IQMod> IQModAPI.GetAllMods()
            => GetAllMods(false, false);
        /// <summary>
        /// Returns a list of all of the mods
        /// </summary>
        /// <param name="includeUnloaded">Set to <see langword="true"/> to also include unloaded mods</param>
        /// <returns>A read only list of mods containing all of the loaded mods, and optionally unloaded mods</returns>
        ReadOnlyCollection<IQMod> IQModAPI.GetAllMods(bool includeUnloaded)
            => GetAllMods(includeUnloaded, false);
        /// <summary>
        /// Returns a list of all of the mods
        /// </summary>
        /// <param name="includeUnloaded">Set to <see langword="true"/> to also include unloaded mods</param>
        /// <param name="includeErrored">Set to <see langword="true"/> to also include errored mods</param>
        /// <returns>A read only list of mods containing all of the loaded mods, and optionally unloaded/errored mods</returns>
        ReadOnlyCollection<IQMod> IQModAPI.GetAllMods(bool includeUnloaded, bool includeErrored)
        {
            if (includeErrored)
                return Patcher.foundMods.Select(qmod => (IQMod)qmod).ToList().AsReadOnly();
            else if (includeUnloaded)
                return Patcher.sortedMods.Select(qmod => (IQMod)qmod).ToList().AsReadOnly();
            else
                return Patcher.loadedMods.Select(qmod => (IQMod)qmod).ToList().AsReadOnly();
        }

        /// <summary>
        /// Returns the mod from the assembly which called this method
        /// </summary>
        IQMod IQModAPI.GetMyMod()
            => GetMod(ReflectionHelper.CallingAssemblyByStackTrace(), true, true);

        /// <summary>
        /// Returns a mod from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        IQMod IQModAPI.GetMod(Assembly modAssembly)
            => GetMod(modAssembly, false, false);
        /// <summary>
        /// Returns a mod from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        IQMod IQModAPI.GetMod(Assembly modAssembly, bool includeUnloaded)
            => GetMod(modAssembly, includeUnloaded, false);
        /// <summary>
        /// Returns a mod from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        IQMod IQModAPI.GetMod(Assembly modAssembly, bool includeUnloaded, bool includeErrored)
        {
            if (modAssembly == null) return null;

            foreach (QMod mod in GetAllMods(includeUnloaded, includeErrored))
            {
                if (mod.LoadedAssembly == modAssembly)
                    return mod;
            }

            return null;
        }

        /// <summary>
        /// Returns a mod from an ID
        /// </summary>
        /// <param name="id"></param>
        IQMod IQModAPI.GetMod(string id)
            => GetMod(id, false, false);
        /// <summary>
        /// Returns a mod from an ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        IQMod IQModAPI.GetMod(string id, bool includeUnloaded)
            => GetMod(id, includeUnloaded, false);
        /// <summary>
        /// Returns a mod from an ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        IQMod IQModAPI.GetMod(string id, bool includeUnloaded, bool includeErrored)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (QMod mod in GetAllMods(includeUnloaded, includeErrored))
            {
                if (mod.Id == Regex.Replace(id, Patcher.IDRegex, "", RegexOptions.IgnoreCase))
                    return mod;
            }

            return null;
        }

        /// <summary>
        /// Checks whether or not a mod is present based on its <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        bool IQModAPI.ModPresent(Assembly modAssembly)
            => ModPresent(modAssembly, false, false);
        /// <summary>
        /// Checks whether or not a mod is present based on its <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        bool IQModAPI.ModPresent(Assembly modAssembly, bool includeUnloaded)
            => ModPresent(modAssembly, includeUnloaded, false);
        /// <summary>
        /// Checks whether or not a mod is present based on its <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        bool IQModAPI.ModPresent(Assembly modAssembly, bool includeUnloaded, bool includeErrored)
            => GetMod(modAssembly, includeUnloaded, includeErrored) != null;

        /// <summary>
        /// Checks whether or not a mod is present based on its ID
        /// </summary>
        /// <param name="id"></param>
        bool IQModAPI.ModPresent(string id)
            => ModPresent(id, false, false);
        /// <summary>
        /// Checks whether or not a mod is present based on its ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        bool IQModAPI.ModPresent(string id, bool includeUnloaded)
            => ModPresent(id, includeUnloaded, false);
        /// <summary>
        /// Checks whether or not a mod is present based on its ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        bool IQModAPI.ModPresent(string id, bool includeUnloaded, bool includeErrored)
            => GetMod(id, includeUnloaded, includeErrored) != null;
    }

    public partial interface IQModAPI
    {
        /// <summary>
        /// Marks a mod as errored <para/>
        /// The mod will appear in the red pop-up which is shows when the game starts
        /// </summary>
        void MarkAsErrored();
        /// <summary>
        /// Marks a mod as errored <para/>
        /// The mod will appear in the red pop-up which is shows when the game starts
        /// </summary>
        /// <param name="modAssembly">
        /// The assembly of the mod to mark as errored. <para/>
        /// </param>
        void MarkAsErrored(Assembly modAssembly);

        /// <summary>
        /// Returns a list of all of the mods
        /// </summary>
        /// <returns>A read only list of mods containing all of the loaded mods</returns>
        ReadOnlyCollection<IQMod> GetAllMods();
        /// <summary>
        /// Returns a list of all of the mods
        /// </summary>
        /// <param name="includeUnloaded">Set to <see langword="true"/> to also include unloaded mods</param>
        /// <returns>A read only list of mods containing all of the loaded mods, and optionally unloaded mods</returns>
        ReadOnlyCollection<IQMod> GetAllMods(bool includeUnloaded);
        /// <summary>
        /// Returns a list of all of the mods
        /// </summary>
        /// <param name="includeUnloaded">Set to <see langword="true"/> to also include unloaded mods</param>
        /// <param name="includeErrored">Set to <see langword="true"/> to also include errored mods</param>
        /// <returns>A read only list of mods containing all of the loaded mods, and optionally unloaded/errored mods</returns>
        ReadOnlyCollection<IQMod> GetAllMods(bool includeUnloaded, bool includeErrored);

        /// <summary>
        /// Returns the mod from the assembly which called this method
        /// </summary>
        IQMod GetMyMod();

        /// <summary>
        /// Returns a mod from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        IQMod GetMod(Assembly modAssembly);
        /// <summary>
        /// Returns a mod from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        IQMod GetMod(Assembly modAssembly, bool includeUnloaded);
        /// <summary>
        /// Returns a mod from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        IQMod GetMod(Assembly modAssembly, bool includeUnloaded, bool includeErrored);

        /// <summary>
        /// Returns a mod from an ID
        /// </summary>
        /// <param name="id"></param>
        IQMod GetMod(string id);
        /// <summary>
        /// Returns a mod from an ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        IQMod GetMod(string id, bool includeUnloaded);
        /// <summary>
        /// Returns a mod from an ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        IQMod GetMod(string id, bool includeUnloaded, bool includeErrored);

        /// <summary>
        /// Checks whether or not a mod is present based on its <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        bool ModPresent(Assembly modAssembly);
        /// <summary>
        /// Checks whether or not a mod is present based on its <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        bool ModPresent(Assembly modAssembly, bool includeUnloaded);
        /// <summary>
        /// Checks whether or not a mod is present based on its <see cref="Assembly"/>
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        bool ModPresent(Assembly modAssembly, bool includeUnloaded, bool includeErrored);

        /// <summary>
        /// Checks whether or not a mod is present based on its ID
        /// </summary>
        /// <param name="id"></param>
        bool ModPresent(string id);
        /// <summary>
        /// Checks whether or not a mod is present based on its ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        bool ModPresent(string id, bool includeUnloaded);
        /// <summary>
        /// Checks whether or not a mod is present based on its ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeUnloaded">Whether or not to include unloaded mods</param>
        /// <param name="includeErrored">Whether or not to include unloaded mods</param>
        bool ModPresent(string id, bool includeUnloaded, bool includeErrored);
    }
}
