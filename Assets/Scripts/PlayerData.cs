using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Victorina
{
    [CreateAssetMenu(fileName = "DataPlayer")]
    public class PlayerData : ScriptableObject
    {
        public string CreatedDateTimePlayfabProfile;
        public string CustomId;
        public string PlayFabId;
        public string TitlePlayerAccountId;
        public string ErrorInformation;
        public string Item;
        public string RechargedBonusTime;
        public string GuidID;
        public string Name;
        public string Email;
        public string Password;
        public int Bit = 0;
        [SerializeField] private bool _isBonusReady;
        public DateTime RechargedBonusT;

        private int bonusRechargeSeconds;

        public Action<bool> BonusComplete;
        public bool IsPlayed;
        public int TicketsBit;

        public string[] ItemCatalog;
        private readonly Dictionary<string, CatalogItem> _catalog = new Dictionary<string, CatalogItem>();

        public string[] CurrencyVirtual;
        private readonly Dictionary<string, int> _virtualCurrency = new Dictionary<string, int>();

        public bool IsBonusReady { get => _isBonusReady; private set => _isBonusReady = value; }
        public int BonusRechargeSeconds { get {
                PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), GetInventoryComplete, OnFailure);
                return bonusRechargeSeconds; } set => bonusRechargeSeconds = value; }

        public void Init()
        {
            GetAccauntUserInfo();
        }

        internal void AddMoney(int val)
        {
            AddUserVirtualCurrencyRequest request = new AddUserVirtualCurrencyRequest
            {
                Amount = val,
                VirtualCurrency = "BT"
            };

            //PlayFabClientAPI.AddUserVirtualCurrency(request, complete => Debug.Log(complete), error => Debug.Log(error));

        }

        public void Reset()
        {
            GuidID = string.Empty;
            Name = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            Bit = 0;
        }

        private void GetAccauntUserInfo()
        {
            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), OnCompletePlayFabAccountInfo, OnFailure);
            PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest(), OnGetCatalogSuccess, OnFailure);
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), GetInventoryComplete, OnFailure);
        }


        private void OnCompletePlayFabAccountInfo(GetAccountInfoResult info)
        {
            CreatedDateTimePlayfabProfile = info.AccountInfo.Created.ToString();
            CustomId = info.AccountInfo.CustomIdInfo.CustomId;
            PlayFabId = info.AccountInfo.PlayFabId;
            TitlePlayerAccountId = info.AccountInfo.TitleInfo.TitlePlayerAccount.Id;
            Email = info.AccountInfo.PrivateInfo.Email;

            Debug.Log("���������� �� �������� Playfab ��������");
        }

        private void GetInventoryComplete(GetUserInventoryResult result)
        {
            _virtualCurrency.Clear();
            CurrencyVirtual = new string[result.VirtualCurrency.Count];
            var cnt = 0;
            foreach (var pair in result.VirtualCurrency)
            {
                CurrencyVirtual[cnt++] = pair.Key + " = "+ pair.Value;
                _virtualCurrency.Add(pair.Key, pair.Value);
            }
            int bitValue;
            var isGetBit = _virtualCurrency.TryGetValue("BT", out bitValue);
            if (isGetBit)
                Bit = bitValue;
            int bonus;
            var isGetBonus = _virtualCurrency.TryGetValue("BS", out bonus);
            if (isGetBonus)
            {
                VirtualCurrencyRechargeTime BSRechargedTimes;
                var isT = result.VirtualCurrencyRechargeTimes.TryGetValue("BS", out BSRechargedTimes);
                if (isT)
                {
                    BonusRechargeSeconds = BSRechargedTimes.SecondsToRecharge;
                    //var tempTime = BSRechargedTimes.SecondsToRecharge;
                    //RechargedBonusT += DateTime.Now.AddSeconds(tempTime) - DateTime.Now;
                }
                if (bonus > 0)
                {
                    BonusRechargeSeconds = BSRechargedTimes.SecondsToRecharge;
                    _isBonusReady = true;
                }
            }
        }

        private void OnGetCatalogSuccess(GetCatalogItemsResult result)
        {
            HandleCatalog(result.Catalog);
            Debug.Log($"������� ������� ��������");

        }

        private void HandleCatalog(List<CatalogItem> catalog)
        {
            ItemCatalog = new string[catalog.Count];
            var cnt = 0;
            foreach (var item in catalog)
            {
                ItemCatalog[cnt++] = item.ItemId;
                _catalog.Add(item.ItemId, item);
            }
        }
        private void OnFailure(PlayFabError obj)
        {
            ErrorInformation = obj.GenerateErrorReport();
            Debug.Log(ErrorInformation);
        }

        public void GetIsCompletetedBonus()
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), IsBonusComplete, Debug.Log);
        }

        private void IsBonusComplete(GetUserInventoryResult result)
        {
            int bonusValue;
            if (result.VirtualCurrency.TryGetValue("BS", out bonusValue))
            {
                _isBonusReady = bonusValue > 0 ? true : false;
            }
        }

        public void GetBonus()
        {
            Bit += 100;
            IsBonusReady = false;
            PurchaseItemRequest request = new PurchaseItemRequest
            {
                CatalogVersion = "Bonuses",
                ItemId = "bonusBundels",
                VirtualCurrency = "BS",
                Price = 1,

            };
            PlayFabClientAPI.PurchaseItem(request, result => OnBonusComplete(), error => Debug.Log(error));
        }

        private void OnBonusComplete()
        {
            BonusComplete?.Invoke(true);
            BonusRechargeSeconds = 60;
        }


        public void GetUserToBonusRechargedTime() =>
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnRechargedTime, OnFailure);

        private void OnRechargedTime(GetUserInventoryResult result)
        {
            VirtualCurrencyRechargeTime BSRechargedTimes;
            var isT = result.VirtualCurrencyRechargeTimes.TryGetValue("BS", out BSRechargedTimes);
            if (isT)
            {
                var tempTime = BSRechargedTimes.SecondsToRecharge;
                RechargedBonusT += DateTime.Now.AddSeconds(tempTime) - DateTime.Now;
            }
        }
    }
}