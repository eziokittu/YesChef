using UnityEngine;

namespace YesChef
{
    public sealed class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }
        }
    }
}
