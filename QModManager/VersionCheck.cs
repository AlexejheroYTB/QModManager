﻿using Harmony;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Events;

namespace QModManager
{
    internal static class VersionCheck
    {
        internal const string nexusmodsURL = "https://nexusmods.com/subnautica/mods/16"; // ???

        internal static void Parse(string versionStr)
        {
            if (versionStr == null)
            {
                UnityEngine.Debug.Log("Could not get latest version!");
                return;
            }
            Version version = new Version(versionStr);
            if (version == null)
            {
                UnityEngine.Debug.Log("Could not get latest version!");
                return;
            }
            if (!version.Equals(QMod.QModManagerVersion) && QModPatcher.erroredMods.Count <= 0)
                Dialog.Show($"There is a newer version of QModManager available: {version.ToString()} " + 
                    "(current version: {QMod.QModManagerVersion.ToString()})", 
                () => Process.Start(nexusmodsURL), leftButtonText: "Download", blue: true);
        }

        internal const string VersionURL = "https://raw.githubusercontent.com/QModManager/QModManager/2.0/latest-version.txt";

        private static float timer = 0f;

        internal static void Check()
        {
            timer += Time.deltaTime;
            if (timer < 1) return;
            Hooks.Update -= Check;
            if (PlayerPrefs.GetInt("QModManager_EnableUpdateCheck", 1) == 0) return;

            ServicePointManager.ServerCertificateValidationCallback = CustomRemoteCertificateValidationCallback;

            using (WebClient client = new WebClient())
            {
                client.DownloadStringAsync(new Uri(VersionURL));
                client.DownloadStringCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        UnityEngine.Debug.LogException(e.Error);
                        return;
                    }
                    Parse(e.Result);
                };
            }
        }

        [HarmonyPatch(typeof(uGUI_OptionsPanel), "AddTabs")]
        internal static class AddTabPatch
        {
            [HarmonyPostfix]
            internal static void Postfix(uGUI_OptionsPanel __instance)
            {
                bool currentValue = PlayerPrefs.GetInt("QModManager_EnableUpdateCheck", 1) == 0 ? false : true;
                int modsTab = __instance.AddTab("Mods");
                __instance.AddHeading(modsTab, "QModManager");
                __instance.AddToggleOption(modsTab, "Check for updates", currentValue,
                    new UnityAction<bool>((bool toggleVal) => PlayerPrefs.SetInt("QModManager_EnableUpdateCheck", toggleVal == false ? 0 : 1)));
            }
        }

        private static bool CustomRemoteCertificateValidationCallback(object sender,
    X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain,
            // look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        continue;
                    }
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid)
                    {
                        isOk = false;
                        break;
                    }
                }
            }
            return isOk;
        }
    }
}
