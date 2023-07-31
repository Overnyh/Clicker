using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class Clicker : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void ShowAdv();

    [DllImport("__Internal")]
    private static extern void ShowRew();

    [DllImport("__Internal")]
    private static extern void SaveExtern(string str);

    [DllImport("__Internal")]
    private static extern void LoadExtern();

    [DllImport("__Internal")]
    private static extern string GetLang();


    private SaveData _globalData;

    public bool IsTimer { get; set; } = true;

    [SerializeField] private TextMeshProUGUI clickCounterUI;
    [SerializeField] private GameObject updateClickUI;
    [SerializeField] private GameObject updateToiletUI;
    [SerializeField] private GameObject updateBackUI;
    [SerializeField] private GameObject clickUI;
    [SerializeField] private GameObject backUI;
    [SerializeField] private GameObject x2UI;
    [SerializeField] private GameObject effect;
    [SerializeField] private GameObject clickText;
    [SerializeField] private GameObject clickTextCanvas;
    [SerializeField] private GameObject addWindow;
    [SerializeField] private TextMeshProUGUI addText;
    [SerializeField] private string FonsFolder;
    [SerializeField] private string GirlFolder;

    private TextMeshProUGUI _updateClickText;
    private TextMeshProUGUI _updateToiletText;
    private TextMeshProUGUI _updateBackText;


    private int _scoreAdd;
    private int _score;

    private List<int> _updateClickCost = new List<int>() { 125, 375, 625, 1000, 2000, 3000, 5000, 12500, 25000 };
    private List<int> _updateToiletCost = new List<int>() { 150, 350, 750, 2500, 25000 };
    private List<int> _updateBackCost = new List<int>() { 10, 50, 250, 500, 1000, 5000 };

    private Queue<string> _toiletImg = new Queue<string>(new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", });
    private Queue<string> _backImg = new Queue<string>(new[] { "2", "3", "4", "5", "6", "7" });


    private int ClickCost => _updateClickCost.Count == 0 ? 0 : _updateClickCost[0];
    private int ToiletCost => _updateToiletCost.Count == 0 ? 0 : _updateToiletCost[0];
    private int BackCost => _updateBackCost.Count == 0 ? 0 : _updateBackCost[0];

    private string ToiletImg => _toiletImg.Count == 0 ? "" : _toiletImg.Dequeue();
    private string BackImg => _backImg.Count == 0 ? "" : _backImg.Dequeue();

    private int _multipl = 1;
    private float _timeLeft;

    private bool canShowAdw;

    private void Start()
    {
        _score = 0;
        _scoreAdd = 1;
        canShowAdw = true;
        InitButtons();
        _globalData = new SaveData();
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowAdv();
        LoadExtern();
#endif
        StartCoroutine(SetLang());
        StartCoroutine(AdwTimeOut());
        
    }

    private void SaveData()
    {
        _globalData.ClickCount = _score;
        var data = JsonUtility.ToJson(_globalData);
        print(data);
#if UNITY_WEBGL && !UNITY_EDITOR
        SaveExtern(data);
#endif
    }

    private void LoadData(string jsonData)
    {
        _globalData = JsonUtility.FromJson<SaveData>(jsonData);
        _score = _globalData.ClickCount;
        for (int i = 0; i < _globalData.ClickAdd; i++)
        {
            NextClick();
        }

        for (int i = 0; i < _globalData.BackId; i++)
        {
            NextBack();
        }

        for (int i = 0; i < _globalData.ToiletId; i++)
        {
            NextToilet();
        }
        UIUpdate();
        StartCoroutine(SaveTimer());
    }

    IEnumerator SetLang()
    {
        yield return LocalizationSettings.InitializationOperation;
#if UNITY_WEBGL && !UNITY_EDITOR
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[GetLang() == "ru" ? 0 : 1];
#endif
    }

    private void InitButtons()
    {
        _updateClickText = updateClickUI.GetComponentInChildren<TextMeshProUGUI>();
        _updateToiletText = updateToiletUI.GetComponentInChildren<TextMeshProUGUI>();
        _updateBackText = updateBackUI.GetComponentInChildren<TextMeshProUGUI>();
        SetActiveButtons();
        UpdateButtonCost();
    }

    private void SetActiveButtons()
    {
        updateClickUI.SetActive(ClickCost != 0);
        updateClickUI.GetComponent<Button>().interactable = _score >= ClickCost;
        _updateClickText.color = new Color(_updateClickText.color.r, _updateClickText.color.g, _updateClickText.color.b,
            Math.Max(0.4f, _score >= ClickCost ? 1f : 0f));

        updateToiletUI.SetActive(ToiletCost != 0);
        updateToiletUI.GetComponent<Button>().interactable = _score >= ToiletCost;
        _updateToiletText.color = new Color(_updateToiletText.color.r, _updateClickText.color.g,
            _updateClickText.color.b, Math.Max(0.4f, _score >= ToiletCost ? 1f : 0f));

        updateBackUI.SetActive(BackCost != 0);
        updateBackUI.GetComponent<Button>().interactable = _score >= BackCost;
        _updateBackText.color = new Color(_updateBackText.color.r, _updateBackText.color.g, _updateBackText.color.b,
            Math.Max(0.4f, _score >= BackCost ? 1f : 0f));
    }

    private void UpdateButtonCost()
    {
        _updateClickText.text = ClickCost.ToString() + '\n' + _updateClickText.text.Split('\n')[1];
        _updateToiletText.text = ToiletCost.ToString() + '\n' + _updateToiletText.text.Split('\n')[1];
        _updateBackText.text = BackCost.ToString() + '\n' + _updateBackText.text.Split('\n')[1];
    }

    public void UIUpdate()
    {
        clickCounterUI.text = _score.ToString();
        SetActiveButtons();
        UpdateButtonCost();
    }

    public void GetClick()
    {
        ClickEffect(Camera.main.ScreenToWorldPoint(Input.mousePosition), _scoreAdd * _multipl);
        _score += _scoreAdd * _multipl;
        UIUpdate();
    }

    private void ClickEffect(Vector2 pos, int plus)
    {
        Instantiate(effect, pos, Quaternion.identity, transform);
        Instantiate(clickText, clickTextCanvas.transform).GetComponent<TextMeshProUGUI>().text = "+" + plus;
    }

    private void NextClick()
    {
        _updateClickCost.RemoveAt(0);
        _scoreAdd += 1;
    }

    public void UpdateClick()
    {
        if (_score >= ClickCost)
        {
            _score -= ClickCost;
            NextClick();
            
            _globalData.ClickAdd++;
            UIUpdate();
            StartCoroutine(StartAdd());
        }
    }

    private void NextToilet()
    {
        _updateToiletCost.RemoveAt(0);
        clickUI.GetComponent<Image>().sprite = Resources.Load<Sprite>(GirlFolder + ToiletImg);
    }

    public void UpdateToilet()
    {
        if (_score >= ToiletCost)
        {
            _score -= ToiletCost;
            NextToilet();
            
            _globalData.ToiletId++;
            UIUpdate();
            StartCoroutine(StartAdd());
        }
    }

    private void NextBack()
    {
        _updateBackCost.RemoveAt(0);
        backUI.GetComponent<Image>().sprite = Resources.Load<Sprite>(FonsFolder + BackImg);
    }

    public void UpdateBack()
    {
        if (_score >= BackCost)
        {
            _score -= BackCost;
            NextBack();
            
            _globalData.BackId++;
            UIUpdate();
            StartCoroutine(StartAdd());
        }
    }

    public void GetX2()
    {
        StartCoroutine(StartRew());
    }

    public void SetX2()
    {
        _multipl = 2;
        _timeLeft = 45;
        x2UI.GetComponent<Button>().interactable = false;
        StartCoroutine(StartTimer());
    }

    private IEnumerator StartTimer()
    {
        while (_timeLeft > 0)
        {
            if (IsTimer){
                _timeLeft -= Time.deltaTime;
                UpdateTimeText();
            }
            yield return null;
        }

        StopX2();
    }

    public void StopX2()
    {
        _multipl = 1;
        x2UI.GetComponentInChildren<TextMeshProUGUI>().text = "X2";
        x2UI.GetComponent<Button>().interactable = true;
    }

    private IEnumerator SaveTimer()
    {
        while (true)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveData();
#endif
            yield return new WaitForSeconds(5.0f);
        }
    }

    private void UpdateTimeText()
    {
        if (_timeLeft < 0)
            _timeLeft = 0;

        float minutes = Mathf.FloorToInt(_timeLeft / 60);
        float seconds = Mathf.FloorToInt(_timeLeft % 60);
        x2UI.GetComponentInChildren<TextMeshProUGUI>().text = string.Format("{0:00} : {1:00}", minutes, seconds);
    }

    public IEnumerator AdwTimeOut()
    {
        canShowAdw = false;
        yield return new WaitForSeconds(120.0f);
        canShowAdw = true;
    }
    
    public IEnumerator StartRew()
    {
        SetActiveAllBtn(false);
        addWindow.SetActive(true);
        for (int i = 3; 0 < i; i--)
        {
            addText.text = i+"...";
            yield return new WaitForSeconds(1.0f);
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowRew();
#endif
        addWindow.SetActive(false);
        SetActiveAllBtn(true);
        SetActiveButtons();
    }

    public IEnumerator StartAdd()
    {
        if (canShowAdw)
        {
            SetActiveAllBtn(false);
            addWindow.SetActive(true);
            for (int i = 3; 0 < i; i--)
            {
                addText.text = i+"...";
                yield return new WaitForSeconds(1.0f);
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            ShowAdv();
#endif
            addWindow.SetActive(false);
            StartCoroutine(AdwTimeOut());

            SetActiveAllBtn(true);
            SetActiveButtons();
        }
    }
    
    public void SetActiveAllBtn(bool status)
    {
        updateClickUI.GetComponent<Button>().interactable = status;
        updateToiletUI.GetComponent<Button>().interactable = status;
        updateBackUI.GetComponent<Button>().interactable = status;
        x2UI.GetComponent<Button>().interactable = status;
        clickUI.GetComponent<Button>().interactable = status;
    }
}