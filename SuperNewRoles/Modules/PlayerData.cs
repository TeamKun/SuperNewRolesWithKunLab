using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperNewRoles.Modules;
public class PlayerData<T>
{
    private Dictionary<byte, T> _data;
    private Dictionary<PlayerControl, T> _playerdata;
    public T Local
    {
        get
        {
            return this[PlayerControl.LocalPlayer.PlayerId];
        }
        set
        {
            this[PlayerControl.LocalPlayer.PlayerId] = value;
        }
    }
    public T this[byte key]
    {
        get
        {
            return _data == null ? default :
                _data.TryGetValue(key, out T result)
                ? result : default;
        }
        set
        {
            (_data ?? (_data = new(1)))[key] = value;
            if (_playerdata != null)
            {
                PlayerControl player = ModHelpers.PlayerById(key);
                if (player != null)
                    _playerdata[player] = value;
            }
        }
    }
    public T this[PlayerControl key]
    {
        get
        {
            return _data == null
                || key == null
                ? default
                : _data.TryGetValue(key.PlayerId, out T result)
                ? result : default;
        }
        set
        {
            if (key != null)
            {
                (_data ?? (_data = new(1)))[key.PlayerId] = value;
                if (_playerdata != null)
                    _playerdata[key] = value;
            }
        }
    }
    public static implicit operator Dictionary<byte, T>(PlayerData<T> obj)
    {
        return obj._data ?? (obj._data = new());
    }
    public static implicit operator Dictionary<PlayerControl, T>(PlayerData<T> obj)
    {
        if (obj._playerdata == null) {
            Logger.Info("needplayerlistが無効なのにも関わらず、PlayerControlをKeyにしたDictionaryが要求されました。needplayerlistを有効に変更してください。");
            obj._playerdata = new(obj._data.Count);
            foreach (var value in obj._data)
            {
                PlayerControl p = ModHelpers.PlayerById(value.Key);
                if (p != null)
                    obj._playerdata[p] = value.Value;
            }
        }
        return obj._playerdata;
    }

    public Dictionary<byte, T> GetDicts()
    {
        return (Dictionary<byte, T>)this;
    }

    /// <summary>
    /// プレイヤーの情報を保存できるクラス。
    /// 例(intを保存したい場合)：PlayerData<int>
    /// </summary>
    /// <param name="needplayerlist">Dictionary<PlayerControl,T>型が必要かどうか</param>
    public PlayerData(bool needplayerlist = false)
    {
        //使用する際に初期化して、メモリの負担を軽く
        _data = null;
        _playerdata = needplayerlist ? new(1) : null;
    }
}
