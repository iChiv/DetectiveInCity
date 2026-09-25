using System.Collections.Generic;

namespace Detective
{
    public static class DetectiveRegionCatalog
    {
        public readonly struct DistrictInfo
        {
            public readonly string DistrictId;
            public readonly string DisplayName;
            public readonly int FirstEnterMinutes;

            public DistrictInfo(string districtId, string displayName, int firstEnterMinutes)
            {
                DistrictId = districtId;
                DisplayName = displayName;
                FirstEnterMinutes = firstEnterMinutes;
            }
        }

        public readonly struct RegionInfo
        {
            public readonly string RegionId;
            public readonly string DisplayName;
            public readonly string DistrictId;
            public readonly string ScenePath;
            public readonly string Description;
            public readonly bool IsOutdoor;

            public RegionInfo(string regionId, string displayName, string districtId, string sceneName, string description, bool isOutdoor)
            {
                RegionId = regionId;
                DisplayName = displayName;
                DistrictId = districtId;
                ScenePath = $"Assets/Scenes/Regions/{sceneName}.unity";
                Description = description;
                IsOutdoor = isOutdoor;
            }
        }

        public static readonly DistrictInfo[] Districts =
        {
            new("prologue", "红灯区小巷", -1),
            new("redlight", "红灯区", -1),
            new("financial", "金融街", 23 * 60 + 30),
            new("industrial", "旧工业区", 25 * 60 + 30),
            new("residential", "住宅区", 27 * 60 + 30),
        };

        public static readonly RegionInfo[] Regions =
        {
            new("D0_Alley", "红灯区小巷", "prologue", "D0_Alley", "雨夜的垃圾堆旁，一切从这里开始。", true),
            new("D1_RedLight", "红灯区街道", "redlight", "D1_RedLight", "霓虹与暴雨，撑透明伞的女人在等你。", true),
            new("I1_Bar", "酒吧", "redlight", "I1_Bar", "爵士乐与威士忌，老酒保什么都知道。", false),
            new("D2_Financial", "金融街街道", "financial", "D2_Financial", "警戒线外的金融街，死者公寓就在街角。", true),
            new("I2_Apartment", "死者公寓", "financial", "I2_Apartment", "案发现场：门禁、尸体与一间上了锁的卧室。", false),
            new("I2_Coffee", "咖啡店", "financial", "I2_Coffee", "凌晨两点的浓缩咖啡，和一张餐巾纸。", false),
            new("D3_Industrial", "工业区外场", "industrial", "D3_Industrial", "被撬开的铁门，涂鸦写着：NC-2077 是叛徒。", true),
            new("I3_Warehouse", "废弃仓库", "industrial", "I3_Warehouse", "线人瘦猴和他的情报生意。", false),
            new("I3_Basement", "工厂地下室", "industrial", "I3_Basement", "干涸的血迹与一台加密的电脑。", false),
            new("I3_Rooftop", "工厂天台", "industrial", "I3_Rooftop", "狙击手的枪口，和一句警告。", true),
            new("D4_Residential", "住宅区街道", "residential", "D4_Residential", "信箱、流浪猫与一盏刻着七道痕的路灯。", true),
            new("I4_YourHome", "你的公寓", "residential", "I4_YourHome", "镜子里的伤，和你不肯记起的过去。", false),
            new("I4_VictimHome", "死者家中", "residential", "I4_VictimHome", "哭泣的妻子，和遗物里的笔记。", false),
        };

        public static bool TryGetRegion(string regionId, out RegionInfo region)
        {
            foreach (RegionInfo candidate in Regions)
            {
                if (candidate.RegionId == regionId)
                {
                    region = candidate;
                    return true;
                }
            }

            region = default;
            return false;
        }

        public static bool TryGetDistrict(string districtId, out DistrictInfo district)
        {
            foreach (DistrictInfo candidate in Districts)
            {
                if (candidate.DistrictId == districtId)
                {
                    district = candidate;
                    return true;
                }
            }

            district = default;
            return false;
        }

        public static IEnumerable<RegionInfo> RegionsOf(string districtId)
        {
            foreach (RegionInfo region in Regions)
            {
                if (region.DistrictId == districtId)
                {
                    yield return region;
                }
            }
        }
    }
}
