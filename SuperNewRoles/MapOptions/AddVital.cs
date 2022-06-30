using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SuperNewRoles.MapOptions
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
    public class AddVitals
    {
        public static void Postfix()
        {
            if (PlayerControl.GameOptions.MapId == 1 && MapOption.AddVitalsMira.getBool() && Mode.ModeHandler.isMode(Mode.ModeId.Default) && MapOption.MapOptionSetting.getBool())
            {
                Transform Vital = GameObject.Instantiate(PolusObject.transform.FindChild("Office").FindChild("panel_vitals"), GameObject.Find("MiraShip(Clone)").transform);
                Vital.transform.position = new Vector3(8.5969f, 14.6337f, 0.0142f);
            }
        }
        public static GameObject PolusObject => Agartha.MapLoader.PolusObject;
        public static ShipStatus Polus => Agartha.MapLoader.Polus;
    }
}
