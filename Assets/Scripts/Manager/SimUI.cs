using UnityEngine;
using UnityEngine.UI;

public class SimUI: MonoBehaviour
{
    [SerializeField] Toggle hostToggle;
    public void UI_HostToggle()
    {
        Singleton<NetworkManager>.Instance.isHost = hostToggle.isOn;
    }
}
