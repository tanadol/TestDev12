using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public enum SOUNDMODE
{
    SOUNDMODE_BGM,
    SOUNDMODE_ENV,
    SOUNDMODE_SFX
}

public enum SOUNDSFXFIGHTINGCODE
{
    NONE = 0,
    DROP_FLOOR_A,
    DROP_FLOOR_B,
    DROP_FLOOR_C,
    PUNCH_0,
    PUNCH_1,
    PUNCH_2,
    PUNCH_3,
    PUNCH_4,
    PUNCH_5,
    JUMP_0,
    JUMP_1,
    JUMP_2,
    HIT_0,
    HIT_1,
    HIT_2,
    BLOCKED_0,
    BLOCKED_1,
    BLOCKED_2
}

public class SoundManager : MonoBehaviour
{
    //A unique string identifier for this object, must be shared across scenes to work correctly
    //private string instanceName;
    [SerializeField] private AudioSource as_BGM, as_ENV;
    [SerializeField] private List<AudioSource> as_SFX, as_SFXLoop;
    private Dictionary<string, AudioClip> clips;
    private string currentBGMPath, currentENVPath;

    private float volume_SFX = 0.70f, volume_BGM = 0.10f, volume_ENV = 0.70f;

    //fade
    private float FADETIME = 0.4f;
    private bool fadeOutBGM, fadeInBGM;
    private float fadeOutTime, fadeInTime; //default is 2 seconds

    public static SoundManager Instance { get; private set; }


    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Check if there is already an instance of this class
        if (Instance != null && Instance != this)
        {
            // If another instance exists, destroy this one
            Destroy(gameObject);
            return;
        }

        // Set this instance as the singleton instance
        Instance = this;

        // Optional: Make this object persist between scenes
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    // for singleton-like behaviour: we need the first object created to check for other objects and delete them in the scene during a transition
    // since Awake() callback preceded OnSceneLoaded(), place initialization code in Start()
    void Start()
    {

        this.gameObject.name += "_" + random4Text();

        /*
        volume_BGM = GS.Setting.getVolumeBGM();
        volume_SFX = GS.Setting.getVolumeSFX();
        volume_ENV = GS.Setting.getVolumeSFX();
       */

        SceneManager.activeSceneChanged += OnSceneChange;
        SceneManager.sceneLoaded += OnSceneLoaded;
        //instanceName = "CanvasSoundManager";
        as_BGM.loop = true;
        as_BGM.volume = volume_BGM;
        as_ENV.loop = true;
        as_ENV.volume = volume_ENV;
        clips = new Dictionary<string, AudioClip>();
        for (int i = 0; i < as_SFX.Count; i++)
        {
            as_SFX[i].loop = false;
            as_SFX[i].volume = volume_SFX;
        }
    }

    void Update()
    {
        if (fadeOutBGM && as_BGM.isPlaying)
        {
            //fade BGM
            fadeOutTime -= Time.deltaTime;
            as_BGM.volume = volume_BGM * (fadeOutTime / FADETIME);
            if (fadeOutTime <= 0)
            {
                //stop & restore state
                as_BGM.Stop();
                as_BGM.volume = volume_BGM;
                fadeOutBGM = false;
            }
        }
        else if (fadeInBGM)
        {
            //fade BGM
            fadeInTime += Time.deltaTime;
            as_BGM.volume = volume_BGM * (fadeInTime / FADETIME);
            if (fadeInTime >= FADETIME)
            {
                as_BGM.volume = volume_BGM;
                fadeInBGM = false;
            }
        }
    }

    #region fade

    public void fadeoutBGM()
    {
        fadeOutTime = FADETIME;
        fadeOutBGM = true;
    }

    public void fadeinBGM()
    {
        as_BGM.Play();
        fadeInTime = 0;
        fadeInBGM = true;
    }

    #endregion

    #region Volume

    public void setVolume(SOUNDMODE mode, float vol)
    {
        if (vol > 1f) vol = 1;
        if (vol < 0f) vol = 0;
        if (mode == SOUNDMODE.SOUNDMODE_BGM)
        {
            volume_BGM = vol;
            //sest to  AudiosSource
            as_BGM.volume = volume_BGM;
        }
        else if (mode == SOUNDMODE.SOUNDMODE_ENV)
        {
            volume_ENV = vol;
            //sest to  AudiosSource
            as_ENV.volume = volume_ENV;
        }
        else if (mode == SOUNDMODE.SOUNDMODE_SFX)
        {
            volume_SFX = vol;
            //sest to all AudiosSource
            for (int i = 0; i < as_SFX.Count; i++)
            {
                as_SFX[i].volume = volume_SFX;
            }
        }
    }

    public float getCurrentVolume(SOUNDMODE mode)
    {
        switch (mode)
        {
            case SOUNDMODE.SOUNDMODE_BGM:
                return as_BGM.volume;
            case SOUNDMODE.SOUNDMODE_ENV:
                return as_ENV.volume;
            case SOUNDMODE.SOUNDMODE_SFX:
                return as_SFX[0].volume;

        }

        return 1;
    }

    public float getVolume(SOUNDMODE mode)
    {
        switch (mode)
        {
            case SOUNDMODE.SOUNDMODE_BGM:
                return volume_BGM;
            case SOUNDMODE.SOUNDMODE_ENV:
                return volume_ENV;
            case SOUNDMODE.SOUNDMODE_SFX:
                return volume_SFX;

        }

        return 1;
    }

    #endregion

    /// <summary>
    /// SFX is play through 5 audio source
    /// </summary>

    #region SFX



    public void playSFXAndSaved(string path)
    {
        playSFXAndSaved(path,
            SoundManager.Instance != null ? SoundManager.Instance.getCurrentVolume(SOUNDMODE.SOUNDMODE_SFX) : 0.0f);
    }

    //common clip is saved withing array
    public void playSFXAndSaved(string path, float _volume)
    {
        //check if path is already loaded
        //AudioClip clip ;
        if (clips.ContainsKey(path))
        {
            //if clip is currently playing
            for (int i = 0; i < as_SFX.Count; i++)
            {
                if (as_SFX[i].clip == clips[path]
                    && as_SFX[i].isPlaying)
                {
                    //is playing, do nothing(prevent repeat playing)
                    return;
                }
            }

            //play clip
            playSFXClipOnFreeChannel(clips[path], _volume);
        }
        else
        {
            //load clip
            //Debug.Log("playSFX: Sounds/" + path);
            StartCoroutine(LoadAudioSFXAsync("Sounds/" + path, false, _volume));
        }
    }

    /// <summary>
    /// Play sfx allow playing overlap&repleatly
    /// </summary>
    /// <param name="path"></param>
    /// <param name="_volume"></param>
    public void playSFXAllowRedundantAndSaved(string path)
    {
        playSFXAllowRedundantAndSaved(path,
            SoundManager.Instance != null ? SoundManager.Instance.getCurrentVolume(SOUNDMODE.SOUNDMODE_SFX) : 0.0f);

    }

    public void playSFXAllowRedundantAndSaved(string path, float _volume)
    {
        //check if path is already loaded
        if (clips.ContainsKey(path))
        {
            //play clip
            playSFXClipOnFreeChannel(clips[path], _volume);
        }
        else
        {
            //load clip
            //Debug.Log("playSFX: Sounds/" + path);
            StartCoroutine(LoadAudioSFXAsync("Sounds/" + path, false, _volume));
        }
    }

    private void playSFXClipOnFreeChannel(AudioClip clip)
    {
        playSFXClipOnFreeChannel(clip,
            SoundManager.Instance != null ? SoundManager.Instance.getCurrentVolume(SOUNDMODE.SOUNDMODE_SFX) : 0.0f);
    }

    private void playSFXClipOnFreeChannel(AudioClip clip, float _volume)
    {
        for (int i = 0; i < as_SFX.Count; i++)
        {
            if (!as_SFX[i].isPlaying)
            {
                as_SFX[i].volume = _volume;
                as_SFX[i].clip = clip;
                as_SFX[i].Play();
                break;
            }
        }
    }

    //common clip is saved withing array
    public void playSFXLoopAndSaved(string path)
    {
        playSFXLoopAndSaved(path,
            SoundManager.Instance != null ? SoundManager.Instance.getCurrentVolume(SOUNDMODE.SOUNDMODE_SFX) : 0.0f);
    }

    public void playSFXLoopAndSaved(string path, float _volume)
    {
        //check if path is already loaded
        AudioClip clip;
        if (clips.ContainsKey(path))
        {
            //play clip
            clip = clips[path];
            //if AudioSourceis playing this clip then just skip playing it
            foreach (AudioSource source in as_SFXLoop)
            {
                if (source.isPlaying && source.clip == clip)
                {
                    return;
                }
            }

            //if clip is not currently playing then play it
            playSFXLoopClipOnFreeChannel(clip, _volume);
        }
        else
        {
            //load clip
            StartCoroutine(LoadAudioSFXAsync("Sounds/" + path, true, _volume));
        }
    }

    public void stopSFXLoop(string path)
    {
        AudioClip clip;
        if (clips.ContainsKey(path))
        {
            //play clip
            clip = clips[path];
            for (int i = 0; i < as_SFXLoop.Count; i++)
            {
                if (!ReferenceEquals(as_SFXLoop[i], null)
                    && as_SFXLoop[i].clip == clip)
                {
                    as_SFXLoop[i].Stop();
                    clips.Remove(path);
                    return;
                }
            }
        }
    }

    public void stopAllSFXLoop()
    {
        for (int i = 0; i < as_SFXLoop.Count; i++)
        {
            if (!ReferenceEquals(as_SFXLoop[i], null))
                as_SFXLoop[i].Stop();
        }
    }

    public void pauseSFXOneTime(string path)
    {
        AudioClip clip;
        if (clips.ContainsKey(path))
        {
            //play clip
            clip = clips[path];
            for (int i = 0; i < as_SFX.Count; i++)
            {
                if (!ReferenceEquals(as_SFX[i], null)
                    && as_SFX[i].clip == clip)
                {
                    as_SFX[i].Pause();
                    return;
                }
            }
        }
    }

    public void resumeSFXOneTime(string path)
    {
        AudioClip clip;
        if (clips.ContainsKey(path))
        {
            //play clip
            clip = clips[path];
            for (int i = 0; i < as_SFX.Count; i++)
            {
                if (!ReferenceEquals(as_SFX[i], null)
                    && as_SFX[i].clip == clip)
                {
                    as_SFX[i].UnPause();
                    return;
                }
            }
        }
    }

    private void playSFXLoopClipOnFreeChannel(AudioClip clip)
    {
        playSFXLoopClipOnFreeChannel(clip,
            SoundManager.Instance != null ? SoundManager.Instance.getCurrentVolume(SOUNDMODE.SOUNDMODE_SFX) : 0.0f);
    }

    private void playSFXLoopClipOnFreeChannel(AudioClip clip, float _volume)
    {
        for (int i = 0; i < as_SFXLoop.Count; i++)
        {
            if (!ReferenceEquals(as_SFXLoop[i], null)
                && !as_SFXLoop[i].isPlaying)
            {
                as_SFXLoop[i].volume = _volume;
                as_SFXLoop[i].clip = clip;
                as_SFXLoop[i].loop = true;
                as_SFXLoop[i].Play();
                break;
            }
        }
    }

    #endregion

    /// <summary>
    /// Only Single BGM can be played at a time(looping)
    /// it will be played until next playBGM or stopBGM is being called
    /// </summary>

    #region BGM

    /// <param name="path">path inside Sounds/ in resource folder</param>
    public void playBGM(string songPath)
    {
        //BGM/bgm-title
        //songPath = getFilteredSongPath(songPath);

        if (String.Equals(currentBGMPath, songPath))
        {
            if (!as_BGM.isPlaying)
            {
                //song is currently stop
                //fadein current song
                fadeinBGM();
            }
        }
        else
        {

            //not same file
            if (as_BGM.isPlaying)
            {
                //already playing
                //we fade out old song and delay start a new one
                fadeoutBGM();
                StopCoroutine(startNewBGM(songPath));
                StartCoroutine(startNewBGM(songPath));
            }
            else
            {
                //No song is playing
                //start a new one
                if (!ReferenceEquals(as_BGM.clip, null)) as_BGM.clip.UnloadAudioData(); //unload old song
                currentBGMPath = songPath;
                as_BGM.loop = true;
                StartCoroutine(LoadAudioAsync("Sounds/" + songPath, as_BGM, "playBGMDelay"));
            }
        }
    }

    IEnumerator startNewBGM(string songPath)
    {
        yield return new WaitForSeconds(2.1f);
        //start a new one
        //Debug.LogError("FFF");
        if (!ReferenceEquals(as_BGM.clip, null)) as_BGM.clip.UnloadAudioData(); //unload old song
        currentBGMPath = songPath;
        as_BGM.loop = true;
        StartCoroutine(LoadAudioAsync("Sounds/" + songPath, as_BGM, "playBGMDelay"));
    }



    void playBGMDelay()
    {
        as_BGM.Play();
        if (!as_BGM.isPlaying) Invoke("playBGMDelay", FADETIME);
    }

    public void stopBGM()
    {
        if (as_BGM.isPlaying)
        {
            as_BGM.Stop();
        }
    }

    #endregion

    /// <summary>
    /// Environment is single-looping sound
    /// it will be played until scene is change or stopENV is called
    /// </summary>

    #region ENV

    /// <param name="path">path inside Sounds/ in resource folder</param>
    public void playENV(string path)
    {
        if (as_ENV == null) return;
        if (String.Equals(currentENVPath, path)) return; //already played
        if (as_ENV.isPlaying)
        {
            as_ENV.Stop();
        }

        currentENVPath = path;

        as_ENV.loop = true;
        if (path.StartsWith("Sound/"))
        {
            StartCoroutine(LoadAudioAsync(path, as_ENV, "playENVDelay"));
        }
        else
        {
            StartCoroutine(LoadAudioAsync("Sounds/" + path, as_ENV, "playENVDelay"));
        }
    }

    private IEnumerator LoadAudioAsync(string path, AudioSource _source, string _invokename)
    {
        bool completed = false;
        UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<AudioClip> resourceRequest = new();

        Addressables.LoadResourceLocationsAsync(path).Completed += locationsHandle =>
        {
            if (locationsHandle.Result.Count > 0)
            {
                Addressables.LoadAssetAsync<AudioClip>(path).Completed += assetHandle =>
                {
                    resourceRequest = assetHandle;
                    completed = true;
                    if (completed)
                    {
                        _source.clip = resourceRequest.Result as AudioClip;

                        if (!String.IsNullOrEmpty(_invokename))
                        {
                            Invoke(_invokename, 0.1f);
                        }
                    }
                    else
                    {
                        Debug.Log("Asset \"" + path + "\" not found!");
                    }
                };
            }
        };

        yield return resourceRequest;
    }

    private IEnumerator LoadAudioSFXAsync(string path, bool isLoop, float _volume)
    {
        //Debug.Log(path);
        bool completed = false;
        UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<AudioClip> req = new();

        Addressables.LoadResourceLocationsAsync(path).Completed += locationsHandle =>
        {
            if (locationsHandle.Result.Count > 0)
            {
                Addressables.LoadAssetAsync<AudioClip>(path).Completed += assetHandle =>
                {
                    req = assetHandle;
                    completed = true;
                    if (completed)
                    {
                        AudioClip clip = req.Result as AudioClip;

                        if (!ReferenceEquals(clip, null))
                        {
                            if (!clips.ContainsKey(path.Replace("Sounds/", "")))
                                clips.Add(path.Replace("Sounds/", ""), clip);

                            if (isLoop)
                            {
                                playSFXLoopClipOnFreeChannel(clip, _volume);
                            }
                            else
                            {
                                playSFXClipOnFreeChannel(clip, _volume);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Asset \"" + path + "\" not found!");
                    }
                };
            }
        };

        yield return req;
    }

    void playENVDelay()
    {
        if (ReferenceEquals(as_ENV, null)) return;
        as_ENV.Play();
        Debug.Log("as_ENV.isPlaying:" + as_ENV.isPlaying);
        if (!as_ENV.isPlaying) Invoke("playENVDelay", 0.1f);
    }


    public void resumeENV()
    {
        if (!ReferenceEquals(as_ENV, null)
            && !as_ENV.isPlaying)
        {
            as_ENV.Play();
        }
    }

    public void stopENV()
    {
        if (!ReferenceEquals(as_ENV, null)
            && as_ENV.isPlaying)
        {
            as_ENV.Stop();
        }
    }

    #endregion

    #region Instance&Destroy

    void OnSceneChange(Scene pscene, Scene nscene)
    {
        stopENV();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // delete any potential duplicates that might be in the scene already, keeping only this one 
    }

    #endregion

    #region Convenient function

    public void playSoundAction()
    {
        this.playSFXAndSaved("push_ok");
    }

    public void playSoundCancel()
    {
        this.playSFXAndSaved("push_cancel");
    }

    public void playSoundTick()
    {
        this.playSFXAndSaved("tick");
    }

    #endregion

    #region Speed setup

    /*
     * as_BGM, as_ENV;
    [SerializeField] private List<AudioSource> as_SFX, as_SFXLoop
     */
    public void setSpeed(float _speed)
    {
        as_BGM.pitch = _speed;
        as_ENV.pitch = _speed;
        for (int i = 0; i < as_SFXLoop.Count; i++)
        {
            as_SFXLoop[i].pitch = _speed;
        }
        for (int i = 0; i < as_SFX.Count; i++)
        {
            as_SFX[i].pitch = _speed;
        }
    }

    #endregion


    #region SFX path from code

    public void playSFXByCode(SOUNDSFXFIGHTINGCODE _sfxCode)
    {
        if(_sfxCode == SOUNDSFXFIGHTINGCODE.NONE)return;
        string path = SoundManager.Instance.getPathFromCode(_sfxCode);
        if (!string.IsNullOrEmpty(path))
            SoundManager.Instance.playSFXAndSaved(path);
    }

    public string getPathFromCode(SOUNDSFXFIGHTINGCODE _sfxCode)
    {
        switch (_sfxCode)
        {
            case SOUNDSFXFIGHTINGCODE.DROP_FLOOR_A:
                return "SFX/BodyFall_0";
            case SOUNDSFXFIGHTINGCODE.PUNCH_0:
                return "SFX/Punch_0";
            case SOUNDSFXFIGHTINGCODE.JUMP_0:
                return "SFX/Jump_0";
            case SOUNDSFXFIGHTINGCODE.HIT_0:
                return "SFX/Hit_0";
            case SOUNDSFXFIGHTINGCODE.BLOCKED_0:
                return "SFX/Blocked_0";
        }

        return "";
    }

    #endregion

    #region Util
    
    private string random4Text()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] result = new char[4];
        System.Random random = new System.Random();

        for (int i = 0; i < 4; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }

        return new string(result);
        
        #endregion
    }

 }
