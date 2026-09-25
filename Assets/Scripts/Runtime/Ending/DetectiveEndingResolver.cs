using System.Collections.Generic;
using System.Reflection;

namespace Detective
{
    public static class DetectiveEndingResolver
    {
        public const string EndingChoiceAFlag = "ending_choice_a";
        public const string EndingChoiceBFlag = "ending_choice_b";
        public const string EndingChoiceCFlag = "ending_choice_c";
        public const string EndingChoiceDFlag = "ending_choice_d";

        public static readonly int TotalClueCount = CountClueIds();

        public static EndingType Evaluate()
        {
            if (DetectiveGameState.HasFlag(EndingChoiceAFlag))
            {
                return EndingType.A_PerfectTruth;
            }

            if (DetectiveGameState.HasFlag(EndingChoiceBFlag))
            {
                return EndingType.B_Justice;
            }

            if (DetectiveGameState.HasFlag(EndingChoiceCFlag))
            {
                return EndingType.C_StreetExecution;
            }

            if (DetectiveGameState.HasFlag(EndingChoiceDFlag))
            {
                return EndingType.D_YouAreKiller;
            }

            return EndingType.E_Unsolved;
        }

        private static int CountClueIds()
        {
            var values = new HashSet<string>();
            foreach (FieldInfo field in typeof(DetectiveClueIds).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(string) && field.IsLiteral)
                {
                    values.Add((string)field.GetValue(null));
                }
            }

            return values.Count;
        }
    }
}
