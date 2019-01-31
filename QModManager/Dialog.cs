﻿using Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace QModManager
{
    internal static class Dialog
    {
        internal static void Show(string error, Action onLeftButtonPressed = null, Action onRightButtonPressed = null, string leftButtonText = "See Log", string rightButtonText = "Close", bool blue = false)
        {
            uGUI_SceneConfirmation confirmation = uGUI.main.confirmation;

            if (onLeftButtonPressed == null) onLeftButtonPressed = () 
                    => Process.Start(Path.Combine(QModPatcher.QModBaseDir, "%userprofile%/appdata/locallow/Unknown Worlds/Subnautica_Below Zero/output_log.txt"));
            if (onRightButtonPressed == null) onRightButtonPressed = () => { };

            if (string.IsNullOrEmpty(leftButtonText)) confirmation.yes.gameObject.SetActive(false);
            else confirmation.yes.gameObject.GetComponentInChildren<Text>().text = leftButtonText;

            if (string.IsNullOrEmpty(rightButtonText)) confirmation.no.gameObject.SetActive(false);
            else confirmation.no.gameObject.GetComponentInChildren<Text>().text = rightButtonText;

            Sprite sprite = confirmation.gameObject.GetComponentInChildren<Image>().sprite;

            if (blue)
            {
                confirmation.gameObject.GetComponentInChildren<Image>().sprite = confirmation.gameObject.GetComponentsInChildren<Image>()[1].sprite;
            }

            List<Text> texts = confirmation.gameObject.GetComponentsInChildren<Text>().ToList();
            texts.RemoveAt(0);
            texts.Do(t => t.fontSize = t.fontSize - 2);

            confirmation.Show(error, delegate (bool leftButtonClicked)
            {
                if (leftButtonClicked) onLeftButtonPressed.Invoke();
                else onRightButtonPressed.Invoke();

                confirmation.yes.gameObject.SetActive(true);
                confirmation.no.gameObject.SetActive(true);

                confirmation.yes.gameObject.GetComponentInChildren<Text>().text = "Yes";
                confirmation.no.gameObject.GetComponentInChildren<Text>().text = "No";

                confirmation.gameObject.GetComponentInChildren<Image>().sprite = sprite;

                texts.Do(t => t.fontSize = t.fontSize + 2);
            });
        }
    }
}
