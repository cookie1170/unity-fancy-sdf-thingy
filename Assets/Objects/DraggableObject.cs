using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects
{
	public class DraggableObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		private bool _isBeingDragged;

		private void FixedUpdate()
		{
			if (!_isBeingDragged) return;
			
			
		}
		
		public void OnPointerDown(PointerEventData eventData)
		{
			Debug.Log("IPointerDownHandler triggered!");
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			Debug.Log("IPointerUpHandler triggered!");
		}
	}
}
