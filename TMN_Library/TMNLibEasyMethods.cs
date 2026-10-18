using UnityEngine;

namespace TMNLib
{
    public static class EasyMethods
    {
        public static bool IsEqualV3(Vector3 v1, Vector3 v2, float minDistance)
        {
            return Core.Utils.EasyMethods.IsEqualV3(v1, v2, minDistance);
        }

        public static string MoneyTextConverter(int moneyAmount)
        {
            return Core.Utils.EasyMethods.MoneyTextConverter(moneyAmount);
        }
    }
}
