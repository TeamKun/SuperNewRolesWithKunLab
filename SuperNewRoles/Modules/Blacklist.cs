using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using InnerNet;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace SuperNewRoles.Modules;

public static class Blacklist
{
    public class BlackPlayer
    {
        public static List<BlackPlayer> Players = new();
        public string FriendCode;
        public string AddedMod = "None";
        public string ReasonCode = "NoneCode";
        public string ReasonTitle = "";
        public string ReasonDescription = "None";
        public DateTime? EndBanTime = null;
        public BlackPlayer(string FriendCode, string AddedMod, string ReasonCode,
            string ReasonTitle, string ReasonDescription, DateTime? EndBanTime = null)
        {
            this.FriendCode = FriendCode;
            this.AddedMod = AddedMod;
            this.ReasonCode = ReasonCode;
            this.ReasonTitle = ReasonTitle;
            this.ReasonDescription = ReasonDescription;
            this.EndBanTime = EndBanTime;
            Players.Add(this);
        }
    }
    public const string BlacklistServerURL = "https://amongusbanlist-1-f7670492.deta.app/api/get_list";
    static bool downloaded = false;
    /// <summary>
    /// 起動時などで予め取得しておく
    /// </summary>
    /// <returns></returns>
    public static IEnumerator FetchBlacklist()
    {
        if (downloaded)
        {
            yield break;
        }
        downloaded = true;
        var request = UnityWebRequest.Get(BlacklistServerURL);
        yield return request.SendWebRequest();
        //new BlackPlayer("", "SuperNewRoles", 0010, "公開からの誘導はおやめください", "公開部屋から誘導してMODをプレイしていたため");
        if (request.isNetworkError || request.isHttpError)
        {
            downloaded = false;
            Logger.Info("Blacklist Error Fetch:"+request.responseCode.ToString());
            yield break;
        }
        var json = JObject.Parse(request.downloadHandler.text);
        for (var user = json["blockedPlayers"].First; user != null; user = user.Next)
        {
            string endbantime = user["EndBanTime"]?.ToString();
            BlackPlayer player = new(
                user["FriendCode"]?.ToString(), user["AddedMod"]?.ToString(), user["Reason"]?["Code"]?.ToString(),
                user["Reason"]?["Title"]?.ToString(), user["Reason"]?["Description"]?.ToString(), endbantime == "never" ? null : (DateTime.TryParse(endbantime, out DateTime resulttime) ? (resulttime - new TimeSpan(9,0,0)) : null));
        }
    }
    public static IEnumerator Check(ClientData clientData = null, int ClientId=-1)
    {
        if (clientData == null)
        {
            do
            {
                yield return null;
                clientData = AmongUsClient.Instance
                                        .allClients
                                        .ToArray()
                                        .FirstOrDefault(client => client.Id == ClientId);
            } while (clientData == null);
        }
        foreach (var player in BlackPlayer.Players)
        {
            if ((!player.EndBanTime.HasValue || player.EndBanTime.Value >= DateTime.UtcNow) && player.FriendCode == clientData.FriendCode)
            {
                if (PlayerControl.LocalPlayer.PlayerId == clientData.Character.PlayerId)
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.Custom);
                    AmongUsClient.Instance.LastCustomDisconnect = "<size=0%>MOD</size>" + player.ReasonTitle+"\n\nMODからこのアカウントのゲームプレイに制限をかけています。\nBANコード："+player.ReasonCode.ToString()+"\n理由："+player.ReasonDescription+"\n期間："+(!player.EndBanTime.HasValue ? "永久" : (player.EndBanTime.Value.ToLocalTime().ToString("yyyy/MM/dd")+"まで"));
                }
                else
                {
                    AmongUsClient.Instance.KickPlayer(clientData.Id, ban: true);
                }
            }
        }
    }
}
[HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.Close))]
internal class DisconnectPopupClosePatch
{
    public static void Prefix(DisconnectPopup __instance)
    {
        try
        {
            __instance.transform.FindChild("CloseButton").localPosition = new(-2.75f, 0.5f, 0);
            __instance.GetComponent<SpriteRenderer>().size = new(5, 1.5f);
            __instance._textArea.fontSizeMin = 1.9f;
            __instance._textArea.enableWordWrapping = true;
        } catch(Exception e){
            Logger.Info(e.ToString());
            
        }
    }
}
[HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.DoShow))]
internal class DisconnectPopupDoShowPatch
{
    public static void Postfix(DisconnectPopup __instance)
    {
        if (AmongUsClient.Instance.LastDisconnectReason == DisconnectReasons.Custom && AmongUsClient.Instance.LastCustomDisconnect.StartsWith("<size=0%>MOD</size>"))
        {
            __instance.transform.FindChild("CloseButton").localPosition = new(-3.2f, 2.15f, -1);
            __instance.GetComponent<SpriteRenderer>().size = new(6, 4);
            __instance._textArea.fontSizeMin = 1.9f;
            __instance._textArea.enableWordWrapping = false;
        }
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
internal class OnGameJoinedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        __instance.StartCoroutine(Blacklist.Check(ClientId:__instance.ClientId).WrapToIl2Cpp());
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
internal class OnPlayerJoinedPatch
{
    public static void Postfix(AmongUsClient __instance,
                                [HarmonyArgument(0)] ClientData data)
    {
        if (__instance.AmHost)
        {
            __instance.StartCoroutine(Blacklist.Check(data).WrapToIl2Cpp());
        }
    }
}
