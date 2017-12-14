using UnityEngine;
using UnityEngine.UI;

public class PlayerCanvas : MonoBehaviour
{
    public static PlayerCanvas canvas;

    [Header("Component References")]
    [SerializeField] private Image _reticule;
    [SerializeField] private UIFader _damageImage;
    [SerializeField] private Text _gameStatusText;
    [SerializeField] private Text _healthValue;
    [SerializeField] private Text _killsValue;
    [SerializeField] private Text _logText;
    [SerializeField] private AudioSource _deathAudio;

    //Ensure there is only one PlayerCanvas
    private void Awake()
    {
        if (canvas == null)
            canvas = this;
        else if (canvas != this)
            Destroy(gameObject);
    }

    //Find all of our resources
    private void Reset()
    {
        _reticule = GameObject.Find("Reticule").GetComponent<Image>();
        _damageImage = GameObject.Find("DamagedFlash").GetComponent<UIFader>();
        _gameStatusText = GameObject.Find("GameStatusText").GetComponent<Text>();
        _healthValue = GameObject.Find("HealthValue").GetComponent<Text>();
        _killsValue = GameObject.Find("KillsValue").GetComponent<Text>();
        _logText = GameObject.Find("LogText").GetComponent<Text>();
        _deathAudio = GameObject.Find("DeathAudio").GetComponent<AudioSource>();
    }

    public void Initialize()
    {
        _reticule.enabled = true;
        _gameStatusText.text = "";
    }

    public void HideReticule()
    {
        _reticule.enabled = false;
    }

    public void FlashDamageEffect()
    {
        _damageImage.Flash();
    }

    public void PlayDeathAudio()
    {
        if (!_deathAudio.isPlaying)
            _deathAudio.Play();
    }

    public void SetKills(int amount)
    {
        _killsValue.text = amount.ToString();
    }

    public void SetHealth(int amount)
    {
        _healthValue.text = amount.ToString();
    }

    public void WriteGameStatusText(string text)
    {
        _gameStatusText.text = text;
    }

    public void WriteLogText(string text, float duration)
    {
        CancelInvoke();
        _logText.text = text;
        Invoke("ClearLogText", duration);
    }

    private void ClearLogText()
    {
        _logText.text = "";
    }
}