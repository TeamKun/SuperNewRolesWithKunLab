using System;
using System.Collections;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SuperNewRoles.Helpers;
using SuperNewRoles.Patches;
using UnityEngine;

namespace SuperNewRoles.KunLab;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public class InkyaTenseiManager
{
    public static readonly Sprite GetButtonSprite = ModHelpers.LoadSpriteFromResources("SuperNewRoles.Resources.ArsonistDouse.png", 115f);

    private static GameObject InkyaTenseiImage;

    private static PlayerControl inkyaPlayer;


    private static bool 投げられる = false;
    private const float 投げられる時間 = 0.5f;
    private static DateTime 投げられた開始時間;

    public static void Postfix(PlayerControl __instance)
    {
        if (__instance.GetRole() == RoleId.Inkya)
        {
            __instance.gameObject.transform.Find("Cosmetics").gameObject.SetActive(false);
            __instance.gameObject.transform.Find("BodyForms").transform.Find("Normal").transform.localScale = new Vector3(0,0,0);


            if (InkyaTenseiImage == null)
            {
                InkyaTenseiImage = new GameObject();

                InkyaTenseiImage.transform.parent = __instance.gameObject.transform.Find("BodyForms");
                InkyaTenseiImage.transform.localPosition = new Vector3(0, 0, 0);
                var testSpriteRenderer = InkyaTenseiImage.AddComponent<SpriteRenderer>();
                testSpriteRenderer.sprite = ModHelpers.LoadSpriteFromResources("SuperNewRoles.Resources.Inkya.default.png", 150f);
                testSpriteRenderer.sortingOrder = 1;
                testSpriteRenderer.transform.localScale = new Vector3(0.05f,0.05f,0.05f);
            }

        }


        //これがnullになっていないってことはつかまれている
        if (inkyaPlayer != null && !PlayerControl.LocalPlayer.IsDead())
        {
            var localPlayerPos = PlayerControl.LocalPlayer.gameObject.transform.position;
            if (投げられる)
            {
                localPlayerPos += new Vector3(0.08f, 0.08f, 0.08f);
                if (DateTime.Now - 投げられた開始時間 > TimeSpan.FromSeconds(投げられる時間))
                {
                    投げられる = false;
                    PlayerControl.LocalPlayer.RpcMurderPlayer(PlayerControl.LocalPlayer,true);
                    PlayerControl.LocalPlayer.RpcSetFinalStatus(FinalStatus.SuicideWisherSelfDeath);
                    inkyaPlayer = null;
                }
            }
            else
            {
                localPlayerPos = inkyaPlayer.gameObject.transform.position + new Vector3(1.5f,0,0);
            }
            PlayerControl.LocalPlayer.gameObject.transform.position = localPlayerPos;

            var writer = RPCHelper.StartRPC(CustomRPC.陰キャ転生_SetPosition);
            writer.Write(PlayerControl.LocalPlayer.PlayerId);
            writer.Write(localPlayerPos.x);
            writer.Write(localPlayerPos.y);
            writer.Write(localPlayerPos.z);
            writer.EndRPC();

            if (__instance.IsDead())
            {
                inkyaPlayer = null;
            }
        }
    }


    public static void つかまれる(byte inkyaPlayerId,  Il2CppStructArray<byte> targetPlayerIds)
    {
        if (PlayerControl.LocalPlayer.IsDead())
        {
            return;
        }
        var isTarget = false;
        //ターゲットかどうかをチェック
        foreach (var targetPlayerId in targetPlayerIds)
        {
            if (targetPlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                isTarget = true;
                continue;
            }
        }
        if (!isTarget)
        {
            return;
        }

        //ターゲットならつかまれる
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            if (player.PlayerId == inkyaPlayerId)
            {
                inkyaPlayer = player;
            }
        }
    }

    public static void おろされる()
    {
        if (PlayerControl.LocalPlayer.IsDead())
        {
            return;
        }
        inkyaPlayer = null;
    }


    public static void 投げる()
    {
        if (PlayerControl.LocalPlayer.IsDead())
        {
            return;
        }
        if (inkyaPlayer != null)
        {
            投げられる = true;
            投げられた開始時間 = DateTime.Now;
        }
    }



    public static void SetPlayerPosition(byte playerId, Vector3 position)
    {
        if (PlayerControl.LocalPlayer.PlayerId == playerId)
        {
            return;
        }
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            if (player.PlayerId == playerId)
            {
                player.transform.position = position;
                return;
            }
        }
    }


}