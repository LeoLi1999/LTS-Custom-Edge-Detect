using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("LTS/Edge Detect")]
public class LTSVolumeEdgeDetect : VolumeComponent, IPostProcessComponent
{
    public BoolParameter useEdgeDetect = new BoolParameter(false, true);
    public BoolParameter useDepthNormal = new BoolParameter(true, true);
    [Tooltip("只有在上一项开启时才会生效")]
    public BoolParameter useDecodeDepthNormal = new BoolParameter(true, true);
    public ColorParameter edgeColor = new ColorParameter(Color.black, true, true, true, false);
    public ColorParameter backgroundColor = new ColorParameter(Color.white, true, true, true, false);
    public ClampedFloatParameter edgeOnly = new ClampedFloatParameter(0f, 0f, 1f, false);
    
    public bool IsActive() => active;
    public bool IsTileCompatible() => false;
}
