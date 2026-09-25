using System.Collections;
using UnityEngine;
namespace Detective
{
    public sealed class DetectiveSceneBeat : MonoBehaviour
    {
        public enum Beat { GuardStepsAside, CorpseCloth, WakeUp, SniperAim }
        [SerializeField] private Beat beat;
        [SerializeField] private Transform subject;
        [SerializeField] private Vector3 destination;
        bool running;
        void OnEnable(){DetectiveGameState.OnClueCollected+=OnClue;}
        void OnDisable(){DetectiveGameState.OnClueCollected-=OnClue;}
        void OnClue(string id){if(beat==Beat.CorpseCloth && id==DetectiveClueIds.CorpseChestWound && !running)StartCoroutine(Reveal());}
        void Start()
        {
            if(beat==Beat.GuardStepsAside && DetectiveGameState.HasFlag("guard_stepped_aside") && subject!=null)subject.position=destination;
            if(beat==Beat.CorpseCloth && DetectiveInvestigationState.IsCollected(DetectiveClueIds.CorpseChestWound) && subject!=null)subject.gameObject.SetActive(false);
        }
        void Update()
        {
            if(running || subject==null)return;
            if(DetectiveRegionLoader.Instance==null || DetectiveRegionLoader.Instance.IsLoading)return;
            if(beat==Beat.WakeUp)
            {
                if(DetectiveGameState.HasFlag("intro_wakeup_done"))return;
                StartCoroutine(Wake());return;
            }
            if(beat!=Beat.GuardStepsAside || DetectiveGameState.HasFlag("guard_stepped_aside"))return;
            var player=GameObject.Find("DetectivePlayer");if(player!=null && Vector3.Distance(player.transform.position,subject.position)<4.5f)StartCoroutine(StepAside());
        }
        IEnumerator StepAside()
        {
            running=true;DetectiveGameState.SetFlag("guard_stepped_aside");DetectiveToastUI.Instance?.Show("保安：警官，请进。我就在这里，有事叫我。",4);
            var start=subject.position;for(float t=0;t<1;t+=Time.deltaTime*.8f){subject.position=Vector3.Lerp(start,destination,t);yield return null;}subject.position=destination;
        }
        IEnumerator Reveal()
        {
            running=true;if(subject==null)yield break;var start=subject.position;
            for(float t=0;t<1;t+=Time.deltaTime*1.5f){subject.position=start+new Vector3(0,.45f*t,.65f*t);yield return null;}subject.gameObject.SetActive(false);
        }
        IEnumerator Wake()
        {
            running=true;DetectiveGameState.SetFlag("intro_wakeup_done");var player=GameObject.Find("DetectivePlayer");if(player==null)yield break;
            var mesh=player.GetComponentInChildren<MeshFilter>();if(mesh==null || mesh.transform==player.transform)yield break;
            bool previous=InteractionController.DialogueBlock;InteractionController.DialogueBlock=true;
            var visual=mesh.transform;var original=visual.localRotation;
            for(float t=0;t<1;t+=Time.deltaTime*.65f){visual.localRotation=Quaternion.Slerp(Quaternion.Euler(0,0,85)*original,original,Mathf.SmoothStep(0,1,t));yield return null;}
            visual.localRotation=original;InteractionController.DialogueBlock=previous;
            DetectiveToastUI.Instance?.Show("雨水让你清醒。先检查身边的警徽和纸条。",5);
        }
    }
}
