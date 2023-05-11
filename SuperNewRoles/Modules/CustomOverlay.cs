using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace SuperNewRoles.Patches;
[Harmony]
public class CustomOverlays
{
    public static Sprite helpButton;
    private static Sprite colorBG;
    private static SpriteRenderer meetingUnderlay;
    private static SpriteRenderer infoUnderlay;
    private static TMPro.TextMeshPro infoOverlayLeft;
    private static TMPro.TextMeshPro infoOverlayCenter;
    private static TMPro.TextMeshPro infoOverlayRight;
    public static bool overlayShown = false;

    public static void ResetOverlays()
    {
        HideBlackBG();
        HideInfoOverlay();
        UnityEngine.Object.Destroy(meetingUnderlay);
        UnityEngine.Object.Destroy(infoUnderlay);
        UnityEngine.Object.Destroy(infoOverlayLeft);
        UnityEngine.Object.Destroy(infoOverlayCenter);
        UnityEngine.Object.Destroy(infoOverlayRight);
        meetingUnderlay = infoUnderlay = null;
        infoOverlayLeft = infoOverlayCenter = infoOverlayRight = null;
        overlayShown = false;
    }

    public static bool InitializeOverlays()
    {
        HudManager hudManager = FastDestroyableSingleton<HudManager>.Instance;
        if (hudManager == null) return false;

        if (helpButton == null)
        {
            helpButton = ModHelpers.LoadSpriteFromResources("SuperNewRoles.Resources.HelpButton.png", 115f);
        }

        if (colorBG == null)
        {
            colorBG = ModHelpers.LoadSpriteFromResources("SuperNewRoles.Resources.White.png", 100f);
        }

        if (meetingUnderlay == null)
        {
            meetingUnderlay = UnityEngine.Object.Instantiate(hudManager.FullScreen, hudManager.transform);
            meetingUnderlay.transform.localPosition = new Vector3(0f, 0f, 20f);
            meetingUnderlay.gameObject.SetActive(true);
            meetingUnderlay.enabled = false;
        }

        if (infoUnderlay == null)
        {
            infoUnderlay = UnityEngine.Object.Instantiate(meetingUnderlay, hudManager.transform);
            infoUnderlay.transform.localPosition = new Vector3(0f, 0f, -900f);
            infoUnderlay.gameObject.SetActive(true);
            infoUnderlay.enabled = false;
        }

        if (infoOverlayLeft == null)
        {
            infoOverlayLeft = UnityEngine.Object.Instantiate(hudManager.TaskPanel.taskText, hudManager.transform);
            infoOverlayLeft.fontSize = infoOverlayLeft.fontSizeMin = infoOverlayLeft.fontSizeMax = 1.15f;
            infoOverlayLeft.autoSizeTextContainer = false;
            infoOverlayLeft.enableWordWrapping = false;
            infoOverlayLeft.alignment = TMPro.TextAlignmentOptions.TopLeft;
            infoOverlayLeft.transform.position = Vector3.zero;
            infoOverlayLeft.transform.localPosition = new Vector3(-2.5f, 1.15f, -910f);
            infoOverlayLeft.transform.localScale = Vector3.one;
            infoOverlayLeft.color = Palette.White;
            infoOverlayLeft.enabled = false;
        }

        if (infoOverlayCenter == null)
        {
            infoOverlayCenter = UnityEngine.Object.Instantiate(infoOverlayLeft, hudManager.transform);
            infoOverlayCenter.maxVisibleLines = 28;
            infoOverlayCenter.fontSize = infoOverlayCenter.fontSizeMin = infoOverlayCenter.fontSizeMax = 1.15f;
            infoOverlayCenter.outlineWidth += 0.02f;
            infoOverlayCenter.autoSizeTextContainer = false;
            infoOverlayCenter.enableWordWrapping = false;
            infoOverlayCenter.alignment = TMPro.TextAlignmentOptions.TopLeft;
            infoOverlayCenter.transform.position = Vector3.zero;
            infoOverlayCenter.transform.localPosition = infoOverlayLeft.transform.localPosition + new Vector3(2.5f, 0.0f, 0.0f);
            infoOverlayCenter.transform.localScale = Vector3.one;
            infoOverlayCenter.color = Palette.White;
            infoOverlayCenter.enabled = false;
        }

        if (infoOverlayRight == null)
        {
            infoOverlayRight = UnityEngine.Object.Instantiate(infoOverlayCenter, hudManager.transform);
            infoOverlayRight.maxVisibleLines = 28;
            infoOverlayRight.fontSize = infoOverlayRight.fontSizeMin = infoOverlayRight.fontSizeMax = 1.15f;
            infoOverlayRight.outlineWidth += 0.02f;
            infoOverlayRight.autoSizeTextContainer = false;
            infoOverlayRight.enableWordWrapping = false;
            infoOverlayRight.alignment = TMPro.TextAlignmentOptions.TopLeft;
            infoOverlayRight.transform.position = Vector3.zero;
            infoOverlayRight.transform.localPosition = infoOverlayCenter.transform.localPosition + new Vector3(2.5f, 0.0f, 0.0f);
            infoOverlayRight.transform.localScale = Vector3.one;
            infoOverlayRight.color = Palette.White;
            infoOverlayRight.enabled = false;
        }
        return true;
    }

    public static void ShowBlackBG()
    {
        if (FastDestroyableSingleton<HudManager>.Instance == null) return;
        if (!InitializeOverlays()) return;

        meetingUnderlay.sprite = colorBG;
        meetingUnderlay.enabled = true;
        meetingUnderlay.transform.localScale = new Vector3(20f, 20f, 1f);
        var clearBlack = new Color32(0, 0, 0, 0);

        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
        {
            meetingUnderlay.color = Color.Lerp(clearBlack, Palette.Black, t);
        })));
    }

    public static void HideBlackBG()
    {
        if (meetingUnderlay == null) return;
        meetingUnderlay.enabled = false;
    }

    public static void ShowInfoOverlay(int pattern)
    {
        if (overlayShown) return;

        HudManager hudManager = FastDestroyableSingleton<HudManager>.Instance;
        if ((MapUtilities.CachedShipStatus == null || PlayerControl.LocalPlayer == null || hudManager == null || FastDestroyableSingleton<HudManager>.Instance.IsIntroDisplayed || PlayerControl.LocalPlayer.CanMove) && MeetingHud.Instance != null)
            return;

        if (!InitializeOverlays()) return;

        if (MapBehaviour.Instance != null)
            MapBehaviour.Instance.Close();

        hudManager.SetHudActive(false);

        overlayShown = true;

        Transform parent = MeetingHud.Instance != null ? MeetingHud.Instance.transform : hudManager.transform;
        infoUnderlay.transform.parent = parent;
        infoOverlayLeft.transform.parent = parent;
        infoOverlayCenter.transform.parent = parent;
        infoOverlayRight.transform.parent = parent;

        infoUnderlay.sprite = colorBG;
        infoUnderlay.color = new Color(0.1f, 0.1f, 0.1f, 0.88f);
        infoUnderlay.transform.localScale = new Vector3(7.5f, 5f, 1f);
        infoUnderlay.enabled = true;

        string leftText = "";
        string centerText = "";
        string rightText = "";

        switch (pattern)
        {
            case (int)CustomOverlayPattern.GameInfo:
                SuperNewRolesPlugin.optionsPage = 0;
                break;
            case (int)CustomOverlayPattern.Regulation:
                Regulation(out leftText, out centerText, out rightText);
                infoOverlayRight.transform.localPosition = infoOverlayLeft.transform.localPosition + new Vector3(3.75f, 0.0f, 0.0f);
                break;
            case (int)CustomOverlayPattern.MyRole:
                break;
        }

        infoOverlayLeft.text = leftText;
        infoOverlayLeft.enabled = true;

        infoOverlayCenter.text = centerText;
        infoOverlayCenter.enabled = true;

        infoOverlayRight.text = rightText;
        infoOverlayRight.enabled = true;

        var underlayTransparent = new Color(0.1f, 0.1f, 0.1f, 0.0f);
        var underlayOpaque = new Color(0.1f, 0.1f, 0.1f, 0.88f);
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
        {
            infoUnderlay.color = Color.Lerp(underlayTransparent, underlayOpaque, t);
            infoOverlayLeft.color = Color.Lerp(Palette.ClearWhite, Palette.White, t);
            infoOverlayCenter.color = Color.Lerp(Palette.ClearWhite, Palette.White, t);
            infoOverlayRight.color = Color.Lerp(Palette.ClearWhite, Palette.White, t);
        })));
    }

    public static void HideInfoOverlay()
    {
        if (!overlayShown) return;

        if (MeetingHud.Instance == null) FastDestroyableSingleton<HudManager>.Instance.SetHudActive(true);

        overlayShown = false;
        var underlayTransparent = new Color(0.1f, 0.1f, 0.1f, 0.0f);
        var underlayOpaque = new Color(0.1f, 0.1f, 0.1f, 0.88f);

        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
        {
            if (infoUnderlay != null)
            {
                infoUnderlay.color = Color.Lerp(underlayOpaque, underlayTransparent, t);
                if (t >= 1.0f) infoUnderlay.enabled = false;
            }

            if (infoOverlayLeft != null)
            {
                infoOverlayLeft.color = Color.Lerp(Palette.White, Palette.ClearWhite, t);
                if (t >= 1.0f) infoOverlayLeft.enabled = false;
            }

            if (infoOverlayCenter != null)
            {
                infoOverlayCenter.color = Color.Lerp(Palette.White, Palette.ClearWhite, t);
                if (t >= 1.0f) infoOverlayCenter.enabled = false;
            }

            if (infoOverlayRight != null)
            {
                infoOverlayRight.color = Color.Lerp(Palette.White, Palette.ClearWhite, t);
                if (t >= 1.0f) infoOverlayRight.enabled = false;
            }
        })));
    }

    public static void YoggleInfoOverlay(int pattern)
    {
        if (overlayShown)
            HideInfoOverlay();
        else
            ShowInfoOverlay(pattern);
    }

    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    public static class CustomOverlayKeybinds
    {
        public static void Postfix(KeyboardJoystick __instance)
        {
            if (FastDestroyableSingleton<HudManager>.Instance.Chat.IsOpen && overlayShown)
                HideInfoOverlay();
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;

            if (Input.GetKeyDown(KeyCode.F3)) YoggleInfoOverlay((int)CustomOverlayPattern.GameInfo);
            if (Input.GetKeyDown(KeyCode.H)) YoggleInfoOverlay((int)CustomOverlayPattern.Regulation);
            else if (Input.GetKeyDown(KeyCode.M)) YoggleInfoOverlay((int)CustomOverlayPattern.MyRole);
        }
    }

    private enum CustomOverlayPattern
    {
        GameInfo,
        Regulation,
        MyRole,
    }

    // 2頁毎に設定を表示する
    private static void Regulation(out string left, out string center, out string right)
    {
        left = center = right = null;
        if (SuperNewRolesPlugin.optionsPage > SuperNewRolesPlugin.optionsMaxPage) SuperNewRolesPlugin.optionsPage = 0;

        switch (SuperNewRolesPlugin.optionsPage % 2)
        {
            case 0:
                break;
            case 1:
                SuperNewRolesPlugin.optionsPage -= 1;
                break;
        }

        int firstPage = SuperNewRolesPlugin.optionsPage;
        int page = firstPage;

        left = GameOptionsDataPatch.ResultData();
        SuperNewRolesPlugin.optionsPage = page + 1;

        if (SuperNewRolesPlugin.optionsPage <= SuperNewRolesPlugin.optionsMaxPage)
            right = GameOptionsDataPatch.ResultData();

        SuperNewRolesPlugin.optionsPage = firstPage;
    }
}