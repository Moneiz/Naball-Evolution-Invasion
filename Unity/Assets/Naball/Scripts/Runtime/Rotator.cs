using UnityEngine;

namespace Naball
{
    /// <summary>Rotation permanente (sensor Always relié à un actuator Motion qui ne fait que tourner).</summary>
    public class Rotator : MonoBehaviour
    {
        public Vector3 radiansPerSecond;

        void Update() => transform.Rotate(radiansPerSecond * (Mathf.Rad2Deg * Time.deltaTime), Space.Self);
    }
}
