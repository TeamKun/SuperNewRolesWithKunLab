using System.Collections.Generic;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;

namespace SuperNewRoles.KunLab;

public class InkyaUtil
{
    public const float Distance = 1f;
    public static bool 近くにプレイヤーがいる(PlayerControl control)
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            if (player.PlayerId == control.PlayerId)
            {
                continue;
            }
            if (Vector3.Distance(player.transform.position, control.transform.position) < Distance)
            {
                return true;
            }
        }

        return false;
    }

    public static List<byte> 近くにいるプレイヤー(PlayerControl control)
    {
        List<byte> result = new List<byte>();
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            if (player.PlayerId == control.PlayerId)
            {
                continue;
            }
            if (Vector3.Distance(player.transform.position, control.transform.position) < Distance)
            {
                result.Add(player.PlayerId);
            }
        }

        return result;
    }
}