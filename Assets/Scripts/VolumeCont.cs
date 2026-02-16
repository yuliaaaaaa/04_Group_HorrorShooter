using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeCont : MonoBehaviour
{
    private const string MUSIC_VOLUME_KEY = "musicVolume";
    private const string MUSIC_STATUS = "musicStatus";

    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioSource audioSource;

    public GameObject BT_Song;
    public Sprite SongOn;
    public Sprite SongOff;

    public int PlaySongID = 1;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        Load();
        volumeSlider.value = audioSource.volume;

        if (PlaySongID == 1)
        {
            BT_Song.GetComponent<Image>().sprite = SongOn;
            volumeSlider.interactable = true;
        }
        else if (PlaySongID == 0)
        {
            BT_Song.GetComponent<Image>().sprite = SongOff;
            volumeSlider.interactable = false;
        }
    }

    public void OnClickBT_Song()
    {
        if (PlaySongID == 0)
        {
            PlaySongID = 1;
            BT_Song.GetComponent<Image>().sprite = SongOn;
            audioSource.Play();
            volumeSlider.interactable = true;
        }
        else if (PlaySongID == 1)
        {
            PlaySongID = 0;
            BT_Song.GetComponent<Image>().sprite = SongOff;
            audioSource.Stop();
            volumeSlider.interactable = false;
        }
        Saving();
    }

    public void ChangeVolume()
    {
        audioSource.volume = volumeSlider.value;
        Saving();
    }

    private void Saving()
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volumeSlider.value);
        PlayerPrefs.SetInt(MUSIC_STATUS, PlaySongID);
    }

    private void Load()
    {
        audioSource.volume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        PlaySongID = PlayerPrefs.GetInt(MUSIC_STATUS, 1);
    }
}