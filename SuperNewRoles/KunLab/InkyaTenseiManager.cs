using HarmonyLib;
using UnityEngine;

namespace SuperNewRoles.KunLab;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public class InkyaTenseiManager
{
    public static readonly Sprite GetButtonSprite = ModHelpers.LoadSpriteFromResources("SuperNewRoles.Resources.ArsonistDouse.png", 115f);

    private static GameObject Test2;

    private static PlayerControl targetPlayerControl;

    public static void Postfix(PlayerControl __instance)
    {
        if (__instance.GetRole() == RoleId.TestRole)
        {
            __instance.gameObject.transform.Find("Cosmetics").gameObject.SetActive(false);
            __instance.gameObject.transform.Find("BodyForms").transform.Find("Normal").transform.localScale = new Vector3(0,0,0);


            if (Test2 == null)
            {
                Test2 = new GameObject();

                Test2.transform.parent = __instance.gameObject.transform.Find("BodyForms");
                Test2.transform.localPosition = new Vector3(0, 0, 0);
                var testSpriteRenderer = Test2.AddComponent<SpriteRenderer>();
                testSpriteRenderer.sprite = ModHelpers.LoadSpriteFromResources("SuperNewRoles.Resources.ArsonistDouse.png", 150f);
                testSpriteRenderer.sortingOrder = 1;
            }

        }


        if (targetPlayerControl != null)
        {
            PlayerControl.LocalPlayer.gameObject.transform.position = targetPlayerControl.gameObject.transform.position + new Vector3(1.5f,0,0);
        }
    }


    public static void つかまれる(byte targetPlayer)
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            if (player.PlayerId == targetPlayer)
            {
                targetPlayerControl = player;
            }
        }
    }

    public static void おろされる()
    {
        targetPlayerControl = null;
    }

}