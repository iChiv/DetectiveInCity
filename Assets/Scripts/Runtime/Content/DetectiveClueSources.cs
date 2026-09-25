using System.Collections.Generic;
namespace Detective
{
    public static class DetectiveClueSources
    {
        static readonly Dictionary<string,string> sources=new();
        static DetectiveClueSources()
        {
            Add("红灯区小巷","badge","bloody_note");
            Add("红灯区·神秘女子","mysterious_woman_testimony","my_anxiety","she_knows_me");
            Add("红灯区·酒吧","bar_timeline","trenchcoat_man");Add("红灯区·电话亭","anonymous_threat");
            Add("金融街·案发公寓","door_record_2245","bullet_hole_9mm","corpse_chest_wound","memory_blank","surveillance_blindspot","victim_no_pain","takeout_box","torn_photo","sleeping_pills","trenchcoat","diary_threatened","safe_code_2077","recording_threat");
            Add("金融街·保安与现场取证","guard_bribed","deleted_footage","trenchcoat_came","trenchcoat_knows_you","fingerprint_evidence");
            Add("金融街·乞丐","fleeing_industrial");Add("金融街·咖啡馆","napkin_notes","self_doubt","napkin_killer");
            Add("金融街·公寓门口对峙","coatman_confessed","dropped_key");Add("工业区·入口调查","gate_forced","tire_tracks","graffiti_traitor");
            Add("工业区·废弃仓库","trenchcoat_impersonates","trenchcoat_to_residential","info_trade","trenchcoat_file","your_file");
            Add("工业区·工厂地下室","blood_trail","lockpick_tool","former_partners","was_undercover","mission_failed");Add("工业区·工厂天台","higher_ups_involved","your_past");
            Add("住宅区街道","cat_collar","letter_warning","streetlamp_scratches");Add("住宅区·你的公寓","mirror_bruise","sleeping_pills_empty","dismissal_notice","your_diary_doubt");
            Add("住宅区·死者家属","victim_thought_you_mad","last_meeting_trenchcoat","kept_notes","victim_notes");
        }
        static void Add(string source,params string[] ids){foreach(var id in ids)sources["clue_"+id]=source;}
        public static string Get(string id)=>sources.TryGetValue(id,out var source)?source:"调查与证人交谈";
    }
}
