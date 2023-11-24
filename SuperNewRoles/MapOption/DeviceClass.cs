using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using SuperNewRoles.Helpers;
using SuperNewRoles.MapCustoms;
using SuperNewRoles.Roles;
using SuperNewRoles.Roles.Neutral;
using TMPro;
using UnityEngine;

namespace SuperNewRoles.MapOption;

public static class DeviceClass
{
    public static bool IsAdminRestrict;
    public static bool IsVitalRestrict;
    public static bool IsCameraRestrict;
    public static float AdminTimer;
    public static float VitalTimer;
    public static float CameraTimer;
    public static DateTime AdminStartTime;
    public static DateTime VitalStartTime;
    public static DateTime CameraStartTime;
    public static Dictionary<DeviceType, PlayerControl> DeviceUsePlayer;
    public static Dictionary<DeviceType, DateTime> DeviceUserUseTime;
    public static TextMeshPro TimeRemaining;
    public enum DeviceType
    {
        Admin,
        Camera,
        Vital
    }

    public static void ClearAndReload()
    {
        /*
        IsAdminLimit = MapOption.Admin&& MapOption.IsAdminLimit.GetBool();
        AdminTimer = MapOption.AdminTimerOption.GetFloat();
        */
        if (MapOption.IsUsingRestrictDevicesTime)
        {
            IsAdminRestrict = MapOption.RestrictAdmin.GetBool();
            AdminTimer = IsAdminRestrict ? MapOption.DeviceUseAdminTime.GetFloat() : 0;
            IsCameraRestrict = MapOption.RestrictCamera.GetBool();
            CameraTimer = IsCameraRestrict ? MapOption.DeviceUseCameraTime.GetFloat() : 0;
            IsVitalRestrict = MapOption.RestrictVital.GetBool();
            VitalTimer = IsVitalRestrict ? MapOption.DeviceUseVitalOrDoorLogTime.GetFloat() : 0;
        }
        else
        {
            IsAdminRestrict = false;
            AdminTimer = 0;
            IsCameraRestrict = false;
            CameraTimer = 0;
            IsVitalRestrict = false;
            VitalTimer = 0;
        }
        DeviceUsePlayer = new() { { DeviceType.Admin, null }, { DeviceType.Camera, null }, { DeviceType.Vital, null } };
        DeviceUserUseTime = new() { { DeviceType.Admin, new() }, { DeviceType.Camera, new() }, { DeviceType.Vital, new() } };
    }
    [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.Use))]
    public static class MapConsoleUsePatch
    {
        public static bool Prefix(MapConsole __instance)
        {
            if (ConfigRoles.DebugMode.Value)
            {
                Logger.Info($"Admin Coordinate(x):{__instance.transform.position.x}", "Debug Mode");
                Logger.Info($"Admin Coordinate(y):{__instance.transform.position.y}", "Debug Mode");
                Logger.Info($"Admin Coordinate(Z):{__instance.transform.position.z}", "Debug Mode");
            }
            Roles.Crewmate.Painter.HandleRpc(Roles.Crewmate.Painter.ActionType.CheckAdmin);
            bool IsUse = MapOption.CanUseAdmin && !PlayerControl.LocalPlayer.IsRole(RoleId.Vampire, RoleId.Dependents);
            return IsUse;
        }
    }
    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnEnable))]
    class MapCountOverlayAwakePatch
    {
        public static void Postfix()
        {
            if (IsAdminRestrict && CachedPlayer.LocalPlayer.IsAlive() && !RoleClass.EvilHacker.IsMyAdmin && !BlackHatHacker.IsMyAdmin) AdminStartTime = DateTime.UtcNow;
        }
    }
    public static bool IsChanging = false;
    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
    class MapCountOverlayUpdatePatch
    {
        public static bool Prefix(MapCountOverlay __instance)
        {
            if (IsAdminRestrict && !RoleClass.EvilHacker.IsMyAdmin && !BlackHatHacker.IsMyAdmin && AdminTimer <= 0)
            {
                MapBehaviour.Instance.Close();
                return false;
            }
            bool IsUse = (MapOption.CanUseAdmin && !PlayerControl.LocalPlayer.IsRole(RoleId.Vampire, RoleId.Dependents)) || RoleClass.EvilHacker.IsMyAdmin || BlackHatHacker.IsMyAdmin;
            if (IsUse)
            {
                if (IsChanging)
                    return false;
                bool commsActive = false;
                foreach (PlayerTask task in CachedPlayer.LocalPlayer.PlayerControl.myTasks)
                    if (task.TaskType == TaskTypes.FixComms) commsActive = true;

                if (!__instance.isSab && commsActive)
                {
                    __instance.isSab = true;
                    __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
                    __instance.SabotageText.gameObject.SetActive(true);
                    return false;
                }

                if (__instance.isSab && !commsActive)
                {
                    __instance.isSab = false;
                    __instance.BackgroundColor.SetColor(Color.green);
                    __instance.SabotageText.gameObject.SetActive(false);
                }

                // インポスターや死体の色が変わる役職等が増えたらここに条件を追加
                bool canSeeImpostorIcon = RoleClass.EvilHacker.IsMyAdmin && RoleClass.EvilHacker.CanSeeImpostorPositions;
                bool canSeeDeadIcon = RoleClass.EvilHacker.IsMyAdmin && RoleClass.EvilHacker.CanSeeDeadBodyPositions;

                for (int i = 0; i < __instance.CountAreas.Length; i++)
                {
                    CounterArea counterArea = __instance.CountAreas[i];

                    // ロミジュリと絵画の部屋をアドミンの対象から外す
                    if (!commsActive && counterArea.RoomType > SystemTypes.Hallway)
                    {
                        PlainShipRoom plainShipRoom = MapUtilities.CachedShipStatus.FastRooms[counterArea.RoomType];

                        if (plainShipRoom != null && plainShipRoom.roomArea)
                        {
                            HashSet<int> hashSet = new();
                            int num = plainShipRoom.roomArea.OverlapCollider(__instance.filter, __instance.buffer);
                            int count = 0;
                            if (Roles.Impostor.Bat.RoleData.IsDeviceStop)
                            {
                                counterArea.UpdateCount(Roles.Impostor.Bat.RoleData.RoomAdminData.TryGetValue((int)counterArea.RoomType, out int batadmincount) ? batadmincount : 0);
                                continue;
                            }
                            List<int> colors = new();
                            // 死体の色で表示する数
                            int numDeadIcons = 0;
                            // インポスターの色で表示する数
                            int numImpostorIcons = 0;

                            for (int j = 0; j < num; j++)
                            {
                                Collider2D collider2D = __instance.buffer[j];
                                if (collider2D.CompareTag("DeadBody") && __instance.includeDeadBodies)
                                {
                                    if (canSeeDeadIcon)
                                    {
                                        numDeadIcons++;
                                    }

                                    if (BlackHatHacker.IsMyAdmin)
                                    {
                                        if (!collider2D.GetComponent<DeadBody>()) continue;
                                        if (!BlackHatHacker.InfectedPlayerId.Contains(collider2D.GetComponent<DeadBody>().ParentId)) continue;
                                    }

                                    count++;
                                    colors.Add(ModHelpers.PlayerById(collider2D.GetComponent<DeadBody>().ParentId).CurrentOutfit.ColorId);
                                }
                                else
                                {
                                    PlayerControl component = collider2D.GetComponent<PlayerControl>();
                                    if (!component) continue;
                                    if (component.Data == null || component.Data.Disconnected || component.Data.IsDead) continue;
                                    if (!__instance.showLivePlayerPosition && component.AmOwner) continue;
                                    if (!hashSet.Add(component.PlayerId)) continue;

                                    if (component.IsRole(RoleId.Vampire, RoleId.Dependents)) continue;
                                    if (!CustomOptionHolder.CrackerIsAdminView.GetBool() && RoleClass.Cracker.CrackedPlayers.Contains(component.PlayerId) &&
                                       (component.PlayerId != CachedPlayer.LocalPlayer.PlayerId || !CustomOptionHolder.CrackerIsSelfNone.GetBool()))
                                        continue;
                                    if (BlackHatHacker.IsMyAdmin && !BlackHatHacker.InfectedPlayerId.Contains(component.PlayerId) && !component.AmOwner) continue;

                                    count++;
                                    colors.Add(component.CurrentOutfit.ColorId);
                                    if (canSeeImpostorIcon && component.IsImpostor())
                                    {
                                        numImpostorIcons++;
                                    }
                                }
                            }
                            counterArea.UpdateCount(count);

                            if (BlackHatHacker.IsMyAdmin && BlackHatHacker.BlackHatHackerIsAdminColor.GetBool())
                            {
                                int color = PlayerControl.LocalPlayer.CurrentOutfit.ColorId;
                                for (int j = 0; j < counterArea.myIcons.Count; j++)
                                {
                                    PoolableBehavior icon = counterArea.myIcons[j];
                                    PlayerControl.LocalPlayer.CurrentOutfit.ColorId = colors.Count > j ? colors[j] : 6;
                                    PlayerControl.LocalPlayer.SetPlayerMaterialColors(icon.GetComponent<SpriteRenderer>());
                                }
                                PlayerControl.LocalPlayer.CurrentOutfit.ColorId = color;
                            }
                            else
                            {
                                foreach (PoolableBehavior icon in counterArea.myIcons)
                                {
                                    Material material = icon.GetComponent<SpriteRenderer>().material;
                                    Color iconColor = numImpostorIcons-- > 0 ? Palette.ImpostorRed : numDeadIcons-- > 0 ? Color.gray : Color.yellow;
                                    material.SetColor(PlayerMaterial.BackColor, iconColor);
                                    material.SetColor(PlayerMaterial.BodyColor, iconColor);
                                }
                            }
                        }
                        else Debug.LogWarning($"Couldn't find counter for:{counterArea.RoomType}");
                    }
                    else counterArea.UpdateCount(0);
                }
            }
            return false;
        }
        public static void Postfix(MapCountOverlay __instance)
        {
            if (RoleClass.EvilHacker.IsMyAdmin || BlackHatHacker.IsMyAdmin) return;
            if (!IsAdminRestrict) return;
            if (CachedPlayer.LocalPlayer.IsDead())
            {
                if (TimeRemaining != null) GameObject.Destroy(TimeRemaining.gameObject);
                return;
            }
            if (AdminTimer <= 0)
            {
                MapBehaviour.Instance.Close();
                return;
            }
            MessageWriter writer;
            if (DeviceUsePlayer[DeviceType.Admin] == null)
            {
                string dateTimeString = AdminStartTime.ToString("yyyy/MM/dd HH:mm:ss");
                writer = RPCHelper.StartRPC(CustomRPC.SetDeviceUseStatus);
                writer.Write((byte)DeviceType.Admin);
                writer.Write(CachedPlayer.LocalPlayer.PlayerId);
                writer.Write(true);
                writer.Write(dateTimeString);
                writer.EndRPC();
                RPCProcedure.SetDeviceUseStatus((byte)DeviceType.Admin, CachedPlayer.LocalPlayer.PlayerId, true, dateTimeString);
            }
            if (DeviceUsePlayer[DeviceType.Admin].PlayerId == CachedPlayer.LocalPlayer.PlayerId)
            {
                AdminTimer -= Time.deltaTime;
                writer = RPCHelper.StartRPC(CustomRPC.SetDeviceTime);
                writer.Write((byte)DeviceType.Admin);
                writer.Write(AdminTimer);
                writer.EndRPC();
                RPCProcedure.SetDeviceTime((byte)DeviceType.Admin, AdminTimer);
            }
            if (TimeRemaining == null)
            {
                TimeRemaining = UnityEngine.Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText, __instance.transform);
                TimeRemaining.alignment = TextAlignmentOptions.BottomRight;
                TimeRemaining.transform.position = Vector3.zero;
                TimeRemaining.transform.localPosition = new Vector3(3.25f, 5.25f);
                TimeRemaining.transform.localScale *= 2f;
                TimeRemaining.color = Palette.White;
            }
            TimeRemaining.text = TimeSpan.FromSeconds(AdminTimer).ToString(@"mm\:ss\.ff");
            TimeRemaining.gameObject.SetActive(true);
        }
    }
    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnDisable))]
    class MapCountOverlayOnDisablePatch
    {
        public static void Postfix()
        {
            if (RoleClass.EvilHacker.IsMyAdmin || BlackHatHacker.IsMyAdmin)
            {
                RoleClass.EvilHacker.IsMyAdmin = false;
                BlackHatHacker.IsMyAdmin = false;
                return;
            }
            if (!IsAdminRestrict) return;
            if (TimeRemaining != null) GameObject.Destroy(TimeRemaining.gameObject);
            if (CachedPlayer.LocalPlayer.IsDead()) return;
            if (AdminTimer <= 0) return;
            if (DeviceUsePlayer[DeviceType.Admin] != null && DeviceUsePlayer[DeviceType.Admin].PlayerId == CachedPlayer.LocalPlayer.PlayerId)
            {
                MessageWriter writer = RPCHelper.StartRPC(CustomRPC.SetDeviceUseStatus);
                writer.Write((byte)DeviceType.Admin);
                writer.Write(CachedPlayer.LocalPlayer.PlayerId);
                writer.Write(false);
                writer.Write("");
                writer.EndRPC();
                RPCProcedure.SetDeviceUseStatus((byte)DeviceType.Admin, CachedPlayer.LocalPlayer.PlayerId, false, "");
            }
        }
    }
    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Begin))]
    class CoVitalsOpen
    {
        static void Postfix(VitalsMinigame __instance)
        {
            if (IsVitalRestrict && CachedPlayer.LocalPlayer.IsAlive() && RoleClass.Doctor.Vital == null && !BlackHatHacker.IsMyVutals) VitalStartTime = DateTime.UtcNow;
            Roles.Crewmate.Painter.HandleRpc(Roles.Crewmate.Painter.ActionType.CheckVital);
        }
    }
    [HarmonyPatch(typeof(Minigame), nameof(Minigame.Close), new Type[] { })]
    class VitalCloseOpen
    {
        static void Postfix(Minigame __instance)
        {
            if (__instance is VitalsMinigame && IsVitalRestrict && CachedPlayer.LocalPlayer.IsAlive() && RoleClass.Doctor.Vital == null && !BlackHatHacker.IsMyVutals)
            {
                if (TimeRemaining != null) GameObject.Destroy(TimeRemaining.gameObject);
                if (VitalTimer <= 0) return;
                if (DeviceUsePlayer[DeviceType.Vital] != null && DeviceUsePlayer[DeviceType.Vital].PlayerId == CachedPlayer.LocalPlayer.PlayerId)
                {
                    MessageWriter writer = RPCHelper.StartRPC(CustomRPC.SetDeviceUseStatus);
                    writer.Write((byte)DeviceType.Vital);
                    writer.Write(CachedPlayer.LocalPlayer.PlayerId);
                    writer.Write(false);
                    writer.Write("");
                    writer.EndRPC();
                    RPCProcedure.SetDeviceUseStatus((byte)DeviceType.Vital, CachedPlayer.LocalPlayer.PlayerId, false, "");
                }
            }
            BlackHatHacker.IsMyVutals = false;
        }
    }
    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    class VitalsDevice
    {
        static void Postfix(VitalsMinigame __instance)
        {
            if ((!MapOption.CanUseVitalOrDoorLog || PlayerControl.LocalPlayer.IsRole(RoleId.Vampire, RoleId.Dependents)) && !BlackHatHacker.IsMyVutals)
            {
                __instance.Close();
            }
            if (Roles.Impostor.Bat.RoleData.IsDeviceStop)
            {
                foreach (VitalsPanel vitals in __instance.vitals)
                {
                    if (Roles.Impostor.Bat.RoleData.AliveData.TryGetValue(vitals.PlayerInfo.PlayerId, out bool IsSetAlive) ? IsSetAlive : false)
                    {
                        vitals.IsDiscon = false;
                        vitals.IsDead = false;
                        vitals.Background.sprite = __instance.PanelPrefab.Background.sprite;
                        vitals.Cardio.gameObject.SetActive(true);
                        vitals.Cardio.SetAlive();
                    }
                    else if (vitals.PlayerInfo.Disconnected)
                    {
                        vitals.SetDisconnected();
                    }
                    else
                    {
                        vitals.SetDead();
                    }
                }
            }
            else
            {
                if (BlackHatHacker.IsMyVutals)
                {
                    __instance.BatteryText.gameObject.SetActive(false);
                    foreach (VitalsPanel vitals in __instance.vitals)
                        vitals.gameObject.SetActive(BlackHatHacker.InfectedPlayerId.Contains(vitals.PlayerInfo.PlayerId) || vitals.PlayerInfo.Object.AmOwner);
                }
            }
            if (!IsVitalRestrict || RoleClass.Doctor.Vital != null || BlackHatHacker.IsMyVutals) return;
            if (CachedPlayer.LocalPlayer.IsDead())
            {
                if (TimeRemaining != null) GameObject.Destroy(TimeRemaining.gameObject);
                return;
            }
            if (VitalTimer <= 0)
            {
                __instance.Close();
                return;
            }
            MessageWriter writer;
            if (DeviceUsePlayer[DeviceType.Vital] == null)
            {
                string dateTimeString = VitalStartTime.ToString("yyyy/MM/dd HH:mm:ss");
                writer = RPCHelper.StartRPC(CustomRPC.SetDeviceUseStatus);
                writer.Write((byte)DeviceType.Vital);
                writer.Write(CachedPlayer.LocalPlayer.PlayerId);
                writer.Write(true);
                writer.Write(dateTimeString);
                writer.EndRPC();
                RPCProcedure.SetDeviceUseStatus((byte)DeviceType.Vital, CachedPlayer.LocalPlayer.PlayerId, true, dateTimeString);
            }
            if (DeviceUsePlayer[DeviceType.Vital].PlayerId == CachedPlayer.LocalPlayer.PlayerId)
            {
                VitalTimer -= Time.deltaTime;
                writer = RPCHelper.StartRPC(CustomRPC.SetDeviceTime);
                writer.Write((byte)DeviceType.Vital);
                writer.Write(VitalTimer);
                writer.EndRPC();
                RPCProcedure.SetDeviceTime((byte)DeviceType.Vital, VitalTimer);
            }
            if (TimeRemaining == null)
            {
                TimeRemaining = UnityEngine.Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText, __instance.transform);
                TimeRemaining.alignment = TextAlignmentOptions.BottomRight;
                TimeRemaining.transform.position = Vector3.zero;
                TimeRemaining.transform.localPosition = new Vector3(1.7f, 4.45f);
                TimeRemaining.transform.localScale *= 1.8f;
                TimeRemaining.color = Palette.White;
            }
            TimeRemaining.text = TimeSpan.FromSeconds(VitalTimer).ToString(@"mm\:ss\.ff");
            TimeRemaining.gameObject.SetActive(true);
        }
    }
    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
    class SurveillanceMinigameUpdatePatch
    {
        public static void Postfix(SurveillanceMinigame __instance)
        {
            if (!MapOption.CanUseCamera || PlayerControl.LocalPlayer.IsRole(RoleId.Vampire, RoleId.Dependents))
            {
                __instance.Close();
            }
            CameraUpdate(__instance);
        }
    }
    static bool IsCameraCloseNow;
    static void CameraClose()
    {
        if (!IsCameraRestrict || CachedPlayer.LocalPlayer.IsDead()) return;
        IsCameraCloseNow = true;
        if (TimeRemaining != null) GameObject.Destroy(TimeRemaining.gameObject);
        if (CameraTimer <= 0) return;
        if (DeviceUsePlayer[DeviceType.Camera] != null && DeviceUsePlayer[DeviceType.Camera].PlayerId == CachedPlayer.LocalPlayer.PlayerId)
        {
            MessageWriter writer = RPCHelper.StartRPC(CustomRPC.SetDeviceUseStatus);
            writer.Write((byte)DeviceType.Camera);
            writer.Write(CachedPlayer.LocalPlayer.PlayerId);
            writer.Write(false);
            writer.Write("");
            writer.EndRPC();
            RPCProcedure.SetDeviceUseStatus((byte)DeviceType.Camera, CachedPlayer.LocalPlayer.PlayerId, false, "");
        }
    }
    static void CameraUpdate(Minigame __instance)
    {
        if (!IsCameraRestrict) return;
        if (CachedPlayer.LocalPlayer.IsDead())
        {
            if (TimeRemaining != null) GameObject.Destroy(TimeRemaining.gameObject);
            return;
        }
        if (IsCameraCloseNow) return;
        if (CameraTimer <= 0)
        {
            __instance.Close();
            return;
        }
        MessageWriter writer;
        if (DeviceUsePlayer[DeviceType.Camera] == null)
        {
            string dateTimeString = CameraStartTime.ToString("yyyy/MM/dd HH:mm:ss");
            writer = RPCHelper.StartRPC(CustomRPC.SetDeviceUseStatus);
            writer.Write((byte)DeviceType.Camera);
            writer.Write(CachedPlayer.LocalPlayer.PlayerId);
            writer.Write(true);
            writer.Write(dateTimeString);
            writer.EndRPC();
            RPCProcedure.SetDeviceUseStatus((byte)DeviceType.Camera, CachedPlayer.LocalPlayer.PlayerId, true, dateTimeString);
        }
        if (DeviceUsePlayer[DeviceType.Camera].PlayerId == CachedPlayer.LocalPlayer.PlayerId)
        {
            CameraTimer -= Time.deltaTime;
            writer = RPCHelper.StartRPC(CustomRPC.SetDeviceTime);
            writer.Write((byte)DeviceType.Camera);
            writer.Write(CameraTimer);
            writer.EndRPC();
            RPCProcedure.SetDeviceTime((byte)DeviceType.Camera, CameraTimer);
        }
        if (TimeRemaining == null)
        {
            TimeRemaining = UnityEngine.Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText, __instance.transform);
            TimeRemaining.alignment = TextAlignmentOptions.BottomRight;
            TimeRemaining.transform.position = Vector3.zero;
            TimeRemaining.transform.localPosition =
                GameManager.Instance.LogicOptions.currentGameOptions.MapId == 5 ?
                new(2.3f, 4.2f, -10) :
                new(0.95f, 4.45f, -10f);
            TimeRemaining.transform.localScale *= 1.8f;
            TimeRemaining.color = Palette.White;
        }
        TimeRemaining.text = TimeSpan.FromSeconds(CameraTimer).ToString(@"mm\:ss\.ff");
        TimeRemaining.gameObject.SetActive(true);
    }
    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Close))]
    class PlanetSurveillanceMinigameClosePatch
    {
        public static void Postfix() => CameraClose();
    }
    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Close))]
    class SurveillanceMinigameClosePatch
    {
        public static void Postfix() => CameraClose();
    }
    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Begin))]
    class PlanetSurveillanceMinigameBeginPatch
    {
        public static void Postfix() => IsCameraCloseNow = false;
    }
    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Begin))]
    class SurveillanceMinigameBeginPatch
    {
        public static void Postfix() => IsCameraCloseNow = false;
    }
    [HarmonyPatch(typeof(FungleSurveillanceMinigame), nameof(FungleSurveillanceMinigame.Begin))]
    class FungleSurveillanceMinigameBeginPatch
    {
        public static void Prefix()
        {
            if (MapCustomHandler.IsMapCustom(MapCustomHandler.MapCustomId.TheFungle) &&
                MapCustom.TheFungleCameraOption.GetBool() &&
                ShipStatus.Instance.TryCast<FungleShipStatus>().LastBinocularPos == Vector2.zero)
            {
                ShipStatus.Instance.TryCast<FungleShipStatus>().LastBinocularPos = new(-16.9f, 0.35f);
            }
        }
        public static void Postfix(FungleSurveillanceMinigame __instance)
        {
            IsCameraCloseNow = false;
            if (!MapCustomHandler.IsMapCustom(MapCustomHandler.MapCustomId.TheFungle))
                return;
            if (!MapCustom.TheFungleCameraOption.GetBool())
                return;
            float speed = MapCustom.TheFungleCameraSpeed.GetFloat() / 10f;
            __instance.buttonMoveSpeed = speed;
            __instance.joystickMoveSpeed = speed;
            __instance.keyboardMoveSpeed = speed;
            __instance.mobileJoystickMoveSpeed = speed;
        }
    }
    [HarmonyPatch(typeof(FungleSurveillanceMinigame), nameof(FungleSurveillanceMinigame.Close))]
    class FungleSurveillanceMinigameClosePatch
    {
        public static void Postfix() => CameraClose();
    }

    [HarmonyPatch(typeof(FungleSurveillanceMinigame), nameof(FungleSurveillanceMinigame.Update))]
    class FungleSurveillanceMinigameUpdatePatch
    {
        public static void Prefix(FungleSurveillanceMinigame __instance)
        {
            if (!MapOption.CanUseCamera || PlayerControl.LocalPlayer.IsRole(RoleId.Vampire, RoleId.Dependents))
            {
                __instance.Close();
            }
        }
        public static void Postfix(FungleSurveillanceMinigame __instance)
        {
            CameraUpdate(__instance);
        }
    }
    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
    class PlanetSurveillanceMinigameUpdatePatch
    {
        public static void Postfix(PlanetSurveillanceMinigame __instance)
        {
            if (!MapOption.CanUseCamera || PlayerControl.LocalPlayer.IsRole(RoleId.Vampire, RoleId.Dependents))
            {
                __instance.Close();
            }
            CameraUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(SecurityLogGame), nameof(SecurityLogGame.Update))]
    class SecurityLogGameUpdatePatch
    {
        public static void Postfix(SecurityLogGame __instance)
        {
            if (!MapOption.CanUseVitalOrDoorLog || PlayerControl.LocalPlayer.IsRole(RoleId.Vampire, RoleId.Dependents))
            {
                __instance.Close();
            }
            CameraUpdate(__instance);
        }
    }
}