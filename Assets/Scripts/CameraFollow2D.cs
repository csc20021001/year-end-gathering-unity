using UnityEngine;

namespace YearEnd
{
         [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public bool follow = true;
        public Vector2 worldMin = new Vector2(-22.2f, -12.3f);
        public Vector2 worldMax = new Vector2(22.2f, 12.3f);
        public float smoothing = 0.13f;
        private Vector3 velocity;
        private Camera cam;
        private bool firstFrame=true;
        private void Awake() { cam=GetComponent<Camera>(); }
        private void LateUpdate()
        {
            if (!follow || target==null || cam==null) return;
            float halfH=cam.orthographicSize;
            float halfW=halfH*cam.aspect;
            float minX=worldMin.x+halfW,maxX=worldMax.x-halfW;
            float minY=worldMin.y+halfH,maxY=worldMax.y-halfH;
            Vector3 desired=target.position;desired.z=transform.position.z;
            desired.x=minX<=maxX?Mathf.Clamp(desired.x,minX,maxX):(worldMin.x+worldMax.x)*.5f;
            desired.y=minY<=maxY?Mathf.Clamp(desired.y,minY,maxY):(worldMin.y+worldMax.y)*.5f;
            if(firstFrame) { transform.position=desired; firstFrame=false; }
            else transform.position=Vector3.SmoothDamp(transform.position,desired,ref velocity,smoothing);
        }
    }
}
