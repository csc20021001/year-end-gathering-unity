using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace YearEnd
{
    public sealed class MinigameSwipeTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public MiniGameUI2D owner;
        public int index;
        public Image image;
        private Vector2 start;
        public void OnPointerDown(PointerEventData data) { start=data.position; }
        public void OnPointerUp(PointerEventData data) { if(owner!=null) owner.RegisterSwipe(index,data.position-start); }
        public void SetDone(bool value) { if(image!=null) image.color=value?new Color(.53f,.76f,.50f,.85f):new Color(.83f,.73f,.47f,.85f); }
    }
}
