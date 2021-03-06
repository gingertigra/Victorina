using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace Victorina
{
    public class TicketManager : MonoBehaviour
    {
        private const string TicketEnter = "???? ?? ??????";
        [SerializeField] private PlayerData _playerData;
        [SerializeField] private Text _priceBitTicket;
        [SerializeField] private Text _money;
        [SerializeField] private GameObject _ticketPricePanel;
        [SerializeField] private Button _enterBtn;
        [SerializeField] private GameObject _welcomePanel;
        [SerializeField] private GameObject _progressPanel;
        [SerializeField] private Animator _animator;
        [SerializeField] private AudioSource _audioSource;

        private QuestionLoader _questionLoader;



        private void Start()
        {
            _playerData.InitComplete += InitComplete;

            _playerData.Init();

            _questionLoader = gameObject.GetComponent<QuestionLoader>();
            _money.text = _playerData.Bit.ToString();
            SetPriceTicket(_playerData.PriceBitTicket.ToString());
            if (_playerData.TicketsBit > 0)
                _ticketPricePanel.SetActive(false);
        }

        private void InitComplete()
        {
            if (_enterBtn)
                CreateClikEvent(_enterBtn);
        }

        private void CreateClikEvent(Button button)
        {
            if (!_playerData.IsPlayed && _playerData.TicketsBit <= 0)
                button.onClick.AddListener(BuyingTicket);
            else if (!_playerData.IsPlayed && _playerData.TicketsBit > 0)
            {
                button.GetComponentInChildren<Text>().text = TicketEnter;
                button.onClick.AddListener(EnterOnTicket);
            }
            else if (_playerData.IsPlayed)
            {
                _ticketPricePanel?.SetActive(false);
                button.GetComponentInChildren<Text>().text = "?????????? ????";
                button.onClick.AddListener(ContinuePlay);
            }
        }

        private void ContinuePlay()
        {
            StartCoroutine(WaitLoadFirstQuestion(false));
        }

        private void EnterOnTicket()
        {
            _playerData.ConsumeItem("BitTicket");
            BuyingPlayTocken();
            AnimateEquipTicket();
            StartCoroutine(WaitLoadFirstQuestion(false));
        }

        private void AnimateEquipTicket()
        {
            _animator.enabled = true;
            _animator.Play("TicketTrash");
            _audioSource.Play();
        }

        public void BuyingTicket()
        {
            if (_playerData.Bit < _playerData.PriceBitTicket)
            {
                Debug.Log("???????????? ??????? ??? ??????? ??????");
                return;
            }

            _money.text = (int.Parse(_money.text) - _playerData.PriceBitTicket).ToString();

            PurchaseItemRequest request = new PurchaseItemRequest
            {
                CatalogVersion = "Tickets",
                ItemId = "BitTicket",
                VirtualCurrency = "BT",
                Price = (int)_playerData.PriceBitTicket,

            };
            PlayFabClientAPI.PurchaseItem(request, result => OnBuyingTicketComplete(), error => Debug.Log(error));
        }

        private void BuyingPlayTocken()
        {
            PurchaseItemRequest request = new PurchaseItemRequest
            {
                CatalogVersion = "Tickets",
                ItemId = "PlayToken",
                VirtualCurrency = "BT",
                Price = 0,

            };
            PlayFabClientAPI.PurchaseItem(request, PlayTockenComplete, error => Debug.Log(error));
        }

        private void PlayTockenComplete(PurchaseItemResult result)
        {
            _playerData.ConsumeItem("BitTicket");
            StartCoroutine(WaitLoadFirstQuestion(false));
        }

        private void OnBuyingTicketComplete()
        {
            BuyingPlayTocken();
            Debug.Log("????? ??????");
            _money.text = _playerData.Bit.ToString();
        }

        private void SetPriceTicket(string price)
        {
            _priceBitTicket.text = price;
        }

        private IEnumerator WaitLoadFirstQuestion(bool isLoadComplete)
        {
            _questionLoader?.LoadOneQuestion();
            while (!isLoadComplete)
            {
                isLoadComplete = _questionLoader.IsLoadComplete;
                yield return new WaitForSeconds(1);
            }
            _progressPanel.SetActive(true);
            _welcomePanel.SetActive(false);
        }

    }

}