using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a UI Slider to one SoundManager volume channel: loads the saved value
/// on start and writes changes back live. Put this on each volume slider.
/// </summary>
[RequireComponent(typeof(Slider))]
public class MenuVolumeSlider : MonoBehaviour
{
    public enum Channel { Music, Fallen, PlayerAnim }
    public Channel channel = Channel.Music;

    Slider slider;

    void Awake() => slider = GetComponent<Slider>();

    void Start()
    {
        var sm = SoundManager.Instance;
        if (sm != null)
        {
            float v = channel switch
            {
                Channel.Music      => sm.MusicVolume,
                Channel.Fallen     => sm.FallenVolume,
                _                  => sm.PlayerAnimVolume,
            };
            slider.SetValueWithoutNotify(v);
        }
        slider.onValueChanged.AddListener(OnChanged);
    }

    void OnChanged(float v)
    {
        var sm = SoundManager.Instance;
        if (sm == null) return;
        switch (channel)
        {
            case Channel.Music:      sm.SetMusicVolume(v);      break;
            case Channel.Fallen:     sm.SetFallenVolume(v);     break;
            case Channel.PlayerAnim: sm.SetPlayerAnimVolume(v); break;
        }
    }
}
