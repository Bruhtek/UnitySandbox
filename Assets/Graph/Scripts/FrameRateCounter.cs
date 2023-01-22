using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameRateCounter : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI display;

    [SerializeField, Range(0.1f, 2f)]
    private float sampleDuration = 1f;
    
    public enum DisplayMode { FPS, MS }
    
    [SerializeField]
    private DisplayMode displayMode = DisplayMode.FPS;
    
    private int _frames;
    private float _duration;
    private float _bestDuration = float.MaxValue;
    private float _worstDuration;
    
    private void Update()
    {
        float frameDuration = Time.unscaledDeltaTime;
        this._frames++;
        this._duration += frameDuration;
        
        if(frameDuration < this._bestDuration)
        {
            this._bestDuration = frameDuration;
        }
        if(frameDuration > this._worstDuration)
        {
            this._worstDuration = frameDuration;
        }
        
        if (this._duration >= this.sampleDuration)
        {
            if (this.displayMode == DisplayMode.FPS)
            {
                this.display.SetText(
                        "FPS\n{0:1}\n{1:1}\n{2:1}",
                        this._frames / this._duration,
                        1f / this._bestDuration,
                        1f / this._worstDuration
                );
            }
            else
            {
                this.display.SetText(
                        "MS\n{0:1}\n{1:1}\n{2:1}",
                        this._duration / this._frames * 1000f,
                        this._bestDuration * 1000f,
                        this._worstDuration * 1000f
                );
            }

            this._frames = 0;
            this._duration = 0f;
        }
    }
}
