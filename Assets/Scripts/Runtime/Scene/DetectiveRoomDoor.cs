using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace Detective
{
    public sealed class DetectiveRoomDoor : MonoBehaviour,IInteractable
    {
        [SerializeField] private Transform panel;
        [SerializeField] private GameObject label;
        public string InteractionId=>"bedroom_door";
        public string DisplayName=>"卧室门";
        public string InteractionLabel=>"打开";
        public Transform InteractionPoint=>transform;
        public float InteractionRange=>1.8f;
        public bool CanInteract=>!DetectiveGameState.HasFlag("bedroom_open");
        void Start(){if(!CanInteract)ApplyOpen();}
        public void Interact(GameObject interactor)
        {
            if(!CanInteract)return;
            DetectiveGameState.SetFlag("bedroom_open");StartCoroutine(Swing());
        }
        IEnumerator Swing()
        {
            var collider=GetComponent<Collider>();if(collider!=null)collider.enabled=false;
            var obstacle=GetComponent<NavMeshObstacle>();if(obstacle!=null)obstacle.enabled=false;
            if(label!=null)label.SetActive(false);
            for(float t=0;t<1;t+=Time.deltaTime*2){panel.localRotation=Quaternion.Euler(0,90*Mathf.SmoothStep(0,1,t),0);yield return null;}
            ApplyOpen();
        }
        void ApplyOpen(){if(panel!=null)panel.localRotation=Quaternion.Euler(0,90,0);var c=GetComponent<Collider>();if(c!=null)c.enabled=false;var o=GetComponent<NavMeshObstacle>();if(o!=null)o.enabled=false;if(label!=null)label.SetActive(false);}
    }
}
