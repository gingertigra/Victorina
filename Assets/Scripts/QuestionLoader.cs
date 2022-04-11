﻿using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


namespace Victorina
{
    public class QuestionLoader : MonoBehaviour
    {
        #region Fields

        public Toggle _endQuestion;

        private const string URLGetQuestion = "https://coxcombic-eliminato.000webhostapp.com/Victorina/Question/GetterQuestion.php";
        private const string URLSetGrade = "https://coxcombic-eliminato.000webhostapp.com/Victorina/Question/SetGrade.php";
        private const string LoadingQuestion = "загрузка вопроса";
        private const string GradeInfo = "Оцените сложность";
        private const string Next = "Продолжить";
        private const string RightResult = "Верно";
        private const string ErrorResult = "Ошибка";

        [SerializeField] private Text _question;
        [SerializeField] private Text _timeText;
        [SerializeField] private Text[] _answers;
        [SerializeField] private Text _status;
        [SerializeField] private Text _gradeText;
        [SerializeField] private Text _comment;
        [SerializeField] private Text _result;
        [SerializeField] private Button _startBtn;
        [SerializeField] private Button[] _answerButtons;
        [SerializeField] private Button _nextBtn;
        [SerializeField] private Image[] _gradeImages;
        [SerializeField] private Image[] _progressCells;
        [SerializeField] private Color _defaultColor;
        [SerializeField] private Color _activeColor;
        [SerializeField] private TMP_ColorGradient _colorGradient;

        [SerializeField] private GameObject _ratePanel;
        [SerializeField] private GameObject _progressPanel;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private GameObject _victory;
        [SerializeField] private GameObject _loose;
        [SerializeField] private GameObject _commentPanel;

        [SerializeField] private AudioSource _backAudio;
        [SerializeField] private AudioSource _winAudio;
        [SerializeField] private AudioSource _looseAudio;
        [SerializeField] private Animator _animatorReload;

        [SerializeField] private PlayerData _playerData;

        private HelpController _helpController;

        private string _commentText;

        private Queue<int> _questionIndexes = new Queue<int>();

        private int _currentStepProgress;

        private Coroutine _coroutine;
        private int _trueIndex = -1;
        private float _grade = 3.0f;
        private float _stepGrade = 0.1f;
        private int _userGradeQuestion;
        private int _indexCurrentQuestion;
        private Image _prevImgSignal;

        public bool IsLoadComplete { get; private set; }
        private bool _isLoose;



        #endregion


        #region UnityMethods

        private void Start()
        {
            _helpController = GetComponent<HelpController>();

            if (!_playerData.IsTrain)
                SetCurrentStep();
        }

        private void OnLoadNewQuestion()
        {
            _helpController?.Activator();
        }

        private void PreviousCellMark()
        {
            var cnt = 0;
            if (_currentStepProgress > 0)
                foreach (var cell in _progressCells)
                {
                    cell.color = Color.yellow;
                    cnt++;
                    if (cnt > _currentStepProgress)
                        return;
                }
            else foreach (var cell in _progressCells)
                    cell.color = _defaultColor;
        }

        public void SetCurrentStep()
        {
            if (!_endQuestion.isOn)
                _currentStepProgress = PlayerPrefs.GetInt("CurrentStep");
            else _currentStepProgress = _progressCells.Length - 1;
            PreviousCellMark();
        }

        #endregion


        #region PublicMethods
        public void LoadOneQuestion()
        {
            if (!_playerData.IsTrain)
                if (_isLoose) Loose();

            if (!_playerData.IsTrain)
                if (_currentStepProgress >= _progressCells.Length)
                {
                    Win();
                    return;
                }
            _question.text = LoadingQuestion;

            if (!_playerData.IsTrain)
                SetDefaultRateImage();
            StopAllCoroutines();

            var nextLevel = _currentStepProgress > 2 ? _currentStepProgress / 5 + 2 : 1;

            if (!_playerData.IsTrain)
            {
                StartCoroutine(PrevRaundPause(_currentStepProgress++));
            }
            else
                nextLevel = UnityEngine.Random.Range(0, 5);

            StartCoroutine(GetQuestion(nextLevel));
        }

        public void Win()
        {

            _resultPanel.SetActive(true);
            _loose.SetActive(false);
            _victory.SetActive(true);
            _backAudio.Stop();

            if (PlayerPrefs.GetInt("Music").Equals(1))
                _winAudio.Play();

            if (!_playerData.IsTrain)
            {
                GetComponent<ButtleBitController>().GetWinAndGameOver();
                _playerData.GetQuestionsCount();
                _playerData.GetRightAnswersCount();
            }
            PlayerPrefs.SetInt("CurrentStep", 0);
        }

        public void StartingTimer()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(TimerUpdate());
        }

        public void SendAnswer(int index)
        {
            if (index == _trueIndex)
            {
                _answers[index].color = Color.green;
                //_ratePanel.SetActive(true);
                if (_coroutine != null)
                    StopCoroutine(_coroutine);

                if (!_playerData.IsTrain)
                    _playerData.AddRightAnswersCount();

                _result.color = Color.green;
                _result.text = RightResult;
                _commentPanel.SetActive(true);

                if (!_playerData.IsTrain)
                    _playerData.RightError = false;
            }
            else if (_playerData.RightError)
            {
                _answers[index].color = Color.red;
                //_playerData.RightError = false;
            }
            else
                CommentLoose();

        }

        private void CommentLoose()
        {
            _result.color = Color.red;
            _result.text = ErrorResult;
            _commentPanel.SetActive(true);
            _isLoose = true;

            if (!_playerData.IsTrain)
                GetComponent<ButtleBitController>().GameOver();

        }

        private void Loose()
        {
            _playerData.GetQuestionsCount();
            _playerData.GetRightAnswersCount();

            _resultPanel.SetActive(true);
            _loose.SetActive(true);
            _backAudio.Stop();
            if (PlayerPrefs.GetInt("Music").Equals(1))
                _looseAudio.Play();
            PlayerPrefs.SetInt("CurrentStep", 0);
            _isLoose = false;
        }

        public void SetUserGrade(int value)
        {
            SetDefaultRateImage();

            for (var i = 0; i < value; i++)
            {
                _gradeImages[i].color = Color.yellow;
            }
            _userGradeQuestion = value;
            _nextBtn.GetComponentInChildren<Text>().text = Next;
            _nextBtn.interactable = true;
        }
        public void FiftyFifty()
        {
            var cnt = 0;
            if (_answerButtons.Length > 0)
                while (cnt < 2)
                {
                    var rnd = UnityEngine.Random.Range(0, _answers.Length);
                    if (rnd != _trueIndex)
                    {
                        if (_answerButtons[rnd].isActiveAndEnabled)
                        {
                            _answerButtons[rnd].gameObject.SetActive(false);
                            cnt++;
                        }
                    }
                }
            _helpController.IsTwoErrorVarintsComplete = true;
        }

        public void DelOneErrorVariant()
        {
            var cnt = 0;
            if (_answerButtons.Length > 0)
                while (cnt < 1)
                {
                    var rnd = UnityEngine.Random.Range(0, _answers.Length);
                    if (rnd != _trueIndex)
                    {
                        if (_answerButtons[rnd].isActiveAndEnabled)
                        {
                            _answerButtons[rnd].gameObject.SetActive(false);
                            cnt++;
                        }
                    }
                }
        }


        public void SendGrade()
        {
            if (_progressPanel)
                _progressPanel?.SetActive(true);
            StartCoroutine(SendUserGrade(URLSetGrade));
        }

        #endregion


        #region PrivateMethods

        private IEnumerator GetQuestion(int level = 0)
        {
            //_animatorReload.SetBool("IsLoading", true);
            _trueIndex = -1;
            var lvl = level;

            WWWForm form = new WWWForm();
            form.AddField("level", lvl);

            using (UnityWebRequest request = UnityWebRequest.Post(URLGetQuestion, form))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(request.error);
                    _status.text = "Отсутствует связ с сервером";
                }
                else
                {
                    QuestionCreated(request);
                }
            }
        }

        private void QuestionCreated(UnityWebRequest request)
        {
            var allText = request.downloadHandler.text;
            var texts = allText.Split('☺');

            if (texts.Length < 1)
            {
                _question.text = texts[0];
                return;
            }

            _indexCurrentQuestion = int.Parse(texts[0]);

            if (!IsNewIndex(_indexCurrentQuestion))
            {
                StartCoroutine(GetQuestion());
                return;
            }
            else IsLoadComplete = true;

            _question.text = texts[1];

            List<string> textsList = new List<string>();
            foreach (var text in texts)
                textsList.Add(text);

            textsList.RemoveAt(0);
            textsList.RemoveAt(0);

            _grade = float.Parse(textsList[textsList.Count - 2]);

            _gradeText.text = ((int)_grade).ToString();


            _commentText = textsList[textsList.Count - 1];
            textsList.RemoveAt(textsList.Count - 1);
            textsList.RemoveAt(textsList.Count - 1);

            while (textsList.Count > 0)
            {
                int rnd = UnityEngine.Random.Range(0, textsList.Count);
                _answers[textsList.Count - 1].text = textsList[rnd];
                textsList.RemoveAt(rnd);

                if (rnd == 0 && _trueIndex == -1)
                    _trueIndex = textsList.Count;
            }

            //_animatorReload.SetBool("IsLoading", false);

            SetDefaultColorAnswersText();

            //if (!_startBtn.gameObject.activeInHierarchy)
            //    _startBtn.gameObject.SetActive(true);

            _comment.text = _commentText;
            _commentPanel.SetActive(false);

            if (!_playerData.IsTrain)
                _playerData.AddQuestionCount();

            OnLoadNewQuestion();

            EquipMinFontSizeAllAnswers();
        }

        private void EquipMinFontSizeAllAnswers()
        {
            var maxLength = 0;
            var longerstText = _answers[0];

            foreach (var t in _answers)
            {
                t.resizeTextMaxSize = 100;

                if (t.text.Length > maxLength)
                {
                    maxLength = t.text.Length;
                    longerstText = t;
                }
            }

            var fontSize = longerstText.cachedTextGenerator.fontSizeUsedForBestFit;

            foreach (var t in _answers)
            {
                t.resizeTextMaxSize = fontSize;
            }
        }

        private bool IsNewIndex(int index)
        {
            bool isNewIndex = true;
            foreach (var ind in _questionIndexes)
                if (ind == index)
                    isNewIndex = false;
            if (isNewIndex)
                _questionIndexes.Enqueue(index);
            if (_questionIndexes.Count > 26)
                _questionIndexes.Dequeue();
            return isNewIndex;
        }

        private void SetDefaultColorAnswersText()
        {
            foreach (var answer in _answers)
                answer.color = Color.black;

            foreach (var answer in _answerButtons)
                if (!answer.isActiveAndEnabled)
                    answer.gameObject.SetActive(true);

            _ratePanel.SetActive(false);
            _nextBtn.interactable = false;
            _nextBtn.GetComponentInChildren<Text>().text = GradeInfo;
        }

        private IEnumerator TimerUpdate()
        {
            var tempTime = System.DateTime.MinValue;
            while (true)
            {

                _timeText.text = tempTime.ToString("T", DateTimeFormatInfo.InvariantInfo);
                tempTime = tempTime.AddSeconds(1);
                yield return new WaitForSeconds(1);
            }
        }

        private void SetDefaultRateImage()
        {
            foreach (var img in _gradeImages)
                img.color = Color.white;
        }

        private IEnumerator SendUserGrade(string url)
        {
            CreateGradeValue();
            var textGrade = _grade.ToString();

            WWWForm form = new WWWForm();
            form.AddField("grade", textGrade);
            form.AddField("id", _indexCurrentQuestion);

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                    Debug.Log("Оценку вопроса отправить не удалось");
            }

            yield return new WaitForSeconds(1);


            LoadOneQuestion();

        }

        private void CreateGradeValue()
        {
            _grade = _grade > _userGradeQuestion ? _grade - _stepGrade : _grade + _stepGrade;
            _grade = _grade < 1 ? 1 : _grade;
        }

        private IEnumerator PrevRaundPause(int currentStep)
        {
            var clip = 3;
            if (_prevImgSignal != null)
                _prevImgSignal.color = _defaultColor;
            _prevImgSignal = _progressCells[currentStep].GetComponentsInChildren<Image>()[1];
            _prevImgSignal.color = _activeColor;

            _progressCells[currentStep].GetComponentInChildren<TMP_Text>().colorGradientPreset = _colorGradient;

            if (!_playerData.IsTrain)
                while (clip > 0)
                {
                    _progressCells[currentStep].color = Color.green;
                    yield return new WaitForSeconds(0.5f);
                    _progressCells[currentStep].color = Color.yellow;
                    yield return new WaitForSeconds(0.5f);
                    clip--;
                }
            _progressPanel.SetActive(false);
            StartingTimer();

            PlayerPrefs.SetInt("CurrentStep", currentStep);

        }

        #endregion
    }
}