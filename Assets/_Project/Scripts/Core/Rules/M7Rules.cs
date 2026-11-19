using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 수혈 적합성 (문서 7.1 ④, D16). ABO만 다루고 Rh는 다루지 않는다.
    /// 혈액형을 모르면(Unknown) 적합 여부를 확인할 수 없으므로 부적합으로 본다 → 혈액 팩 경로.
    /// </summary>
    public static class BloodRules
    {
        public static bool CanDonate(BloodType donor, BloodType recipient)
        {
            if (donor == BloodType.Unknown || recipient == BloodType.Unknown)
            {
                return false;
            }

            if (donor == BloodType.O || recipient == BloodType.AB)
            {
                return true;
            }

            return donor == recipient;
        }
    }
}
