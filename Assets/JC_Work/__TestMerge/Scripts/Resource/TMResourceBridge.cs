using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// DH ResourceType 스타일의 API를 제공하면서 내부적으로 JC CurrencyManager에 위임.
    /// GameManager.Instance.Currency 가 초기화되어 있어야 함.
    /// </summary>
    public static class TMResourceBridge
    {
        public static CurrencyType ToCurrency(TMResourceType type)
        {
            switch (type)
            {
                case TMResourceType.Gold: return CurrencyType.Gold;
                case TMResourceType.Wood: return CurrencyType.Wood;
                case TMResourceType.Ore:  return CurrencyType.Ore;
                default:                  return CurrencyType.Gold;
            }
        }

        public static TMResourceType FromCurrency(CurrencyType type)
        {
            switch (type)
            {
                case CurrencyType.Gold: return TMResourceType.Gold;
                case CurrencyType.Wood: return TMResourceType.Wood;
                case CurrencyType.Ore:  return TMResourceType.Ore;
                default:                return TMResourceType.Gold;
            }
        }

        private static CurrencyManager Currency
        {
            get
            {
                if (GameManager.Instance == null)
                {
                    Debug.LogWarning("[TMResourceBridge] GameManager.Instance is null. 부트스트랩 확인 필요.");
                    return null;
                }
                return GameManager.Instance.Currency;
            }
        }

        public static int Get(TMResourceType type)
        {
            var c = Currency;
            return c != null ? c.Get(ToCurrency(type)) : 0;
        }

        public static void Add(TMResourceType type, int amount)
        {
            Currency?.Add(ToCurrency(type), amount);
        }

        public static void Set(TMResourceType type, int value)
        {
            Currency?.Set(ToCurrency(type), value);
        }

        public static bool Has(TMResourceType type, int amount)
        {
            var c = Currency;
            return c != null && c.Has(ToCurrency(type), amount);
        }

        public static bool TrySpend(TMResourceType type, int amount)
        {
            var c = Currency;
            if (c == null || !c.Has(ToCurrency(type), amount)) return false;
            c.Add(ToCurrency(type), -amount);
            return true;
        }
    }
}
