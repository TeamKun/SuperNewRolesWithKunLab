using System;
using System.Collections.Generic;
using HarmonyLib;
using SuperNewRoles.Patches;
using UnityEngine;

namespace SuperNewRoles.KunLab;

/// <summary>
/// labmemo 簡単にチャットログにテキストを送信できるようにする
/// </summary>
[HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
class ChatLogger
{

    private static readonly List<string> sendChatLog = new List<string>();

    public static void SendChat(string text)
    {
        sendChatLog.Add(text);
    }

    public static void ExportLog(string text)
    {
        string fileName = $"ChatLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        System.IO.File.WriteAllText(fileName, text);
        //ファイルを開く
        System.Diagnostics.Process.Start(fileName);
    }


    public static void Postfix(PlayerPhysics __instance)
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            var hierarchy = GetHierarchy.GetHierarchyText();
            SendChat(hierarchy);
            ExportLog(hierarchy);
            AddChatPatch.SendCommand(__instance.myPlayer, "test", "SNRwithKun");
        }

        if (sendChatLog.Count <= 0) return;

        foreach (var text in sendChatLog)
        {
            AddChatPatch.SendCommand(__instance.myPlayer, text, "SNRwithKun");
        }
        sendChatLog.Clear();
    }
}