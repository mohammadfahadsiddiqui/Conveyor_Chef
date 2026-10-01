using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// <see cref="DesignFrame"/> leaves this object and everything under it exactly as authored;
    /// on a canvas, it leaves the whole canvas alone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DesignFrameIgnore : MonoBehaviour
    {
    }
}
