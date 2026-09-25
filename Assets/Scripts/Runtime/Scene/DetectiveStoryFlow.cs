using UnityEngine;
namespace Detective
{
    // Persistent tutorial and readable investigation follow-ups, using existing clue events.
    public sealed class DetectiveStoryFlow : MonoBehaviour
    {
        string pendingReading;
        void OnEnable(){DetectiveGameState.OnClueCollected+=Collected;DetectiveGameState.OnReasoningDiscovered+=Reasoned;}
        void OnDisable(){DetectiveGameState.OnClueCollected-=Collected;DetectiveGameState.OnReasoningDiscovered-=Reasoned;}
        void Reasoned(string id)
        {
            if(id=="prologue_note")DetectiveToastUI.Instance?.Show("第一次推理完成。结论已保存在“推理出来的线索”。之后可以尝试两条线索组合。",6);
            if(id=="gate_note")DetectiveToastUI.Instance?.Show("双卡推理完成。已知结论可以重复查看，不会重复奖励或扣除专注力。",6);
        }
        void Collected(string id)
        {
            if(id==DetectiveClueIds.BloodyNote && !DetectiveGameState.HasFlag("tutorial_note_hint"))
            {DetectiveGameState.SetFlag("tutorial_note_hint");DetectiveToastUI.Instance?.Show("按 Tab 查看线索。单击读全文，把染血纸条拖到槽位可尝试第一次推理。",7);}
            if(id==DetectiveClueIds.DoorRecord && !DetectiveGameState.HasFlag("tutorial_pair_hint"))
            {DetectiveGameState.SetFlag("tutorial_pair_hint");DetectiveToastUI.Instance?.Show("门禁时间与纸条约定不同。按 Tab 试着组合这两条线索。错误组合会扣专注力，已知结论可免费回看。",7);}
            if(id==DetectiveClueIds.FingerprintEvidence)pendingReading="fingerprint";
            if(id==DetectiveClueIds.MissionFailed)pendingReading="computer";
        }
        void Update()
        {
            if(pendingReading==null || InteractionController.DialogueBlock || DetectiveInspectionUI.IsShowing)return;
            string reading=pendingReading;pendingReading=null;
            if(reading=="computer")DetectiveInspectionUI.Get()?.Read("三年前的邮件", "发件人：警局高层\n收件人：NC-2077\n日期：三年前\n\n你的卧底任务立即终止。目标已发现你。建议迅速撤离。\n\n附件记录：任务失败。\n\n【你曾是卧底】与【任务失败】已记录，可在思维面板再次阅读。");
            else DetectiveInspectionUI.Get()?.Read("现场取证完成", "你请保安封锁出入口，记录现场情况，并对可疑接触面进行指纹采集与封装。\n\n物证已交送检验，取得【物证：指纹】。\n这一过程计入刚才选择的15分钟，不重复扣时。");
        }
    }
}
