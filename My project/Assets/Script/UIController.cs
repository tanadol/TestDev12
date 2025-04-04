using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIMODE
{
    NONE = 0,
    LOGIN,
    SIGNUP,
    GAMEPLAY,
    COUNT
}

public class UserData
{
    public int user_id, diamond, heart;
    public string username;

    public UserData()
    {
        //check PlayerPrefs
        if (PlayerPrefs.HasKey("username"))
        {
            username = PlayerPrefs.GetString("username");
        }
    }

    public void clearValue()
    {
        PlayerPrefs.DeleteAll();
    }

    //set data
    public void setRespondData(ApiResponseData _data)
    {
        user_id = _data.user_id;
        username = _data.username;
        diamond = _data.diamond;
        heart = _data.heart;
        //save username to PlayerPrefs
        PlayerPrefs.SetString("username", username);
        PlayerPrefs.Save();
    }
}

//----------------------------------------------------
//For easy demo, I write all UI windows and component here, see each region for detail.
//MMF_Player is external lib(Commercial ware), added here for UI bouncing effect only.
//----------------------------------------------------
public class UIController : MonoBehaviour
 {
     //User value
     //for demo we put it here
     private UserData player;
     
    private UIMODE currentUIMode;
    private MainAPIConnect apiConnect;  //server connection object
    
    [Header("Login Window")] 
    public Image ui_login;
    public TMP_InputField ip_username,ip_password;
    public MMF_Player ui_login_feedback;
    
    [Header("Signup Window")]
    public Image ui_signup;
    public TMP_InputField ip_signup_username,
        ip_signup_password,
        ip_confirm_pwd;
    public MMF_Player ui_signup_feedback;
    
    [Header("Message Window")]
    public Image ui_msgbox;
    public TMP_Text t_msg;
    public MMF_Player ui_msgbox_feedback;

    [Header("Gameplay Window")] 
    public Image ui_gemplay;
    public Image img_heart_slider;
    public float slider_width_min,slider_width_max;
    public TMP_Text t_diamond_count;
    public MMF_Player heart_feedback,diamond_feedback;


    #region Start/Update
    
    // Start is called before the first frame update
    void Start()
    {
        player = new UserData();
        apiConnect = this.GetComponent<MainAPIConnect>();   //get server connection object
        //show sign in as default
        showWindow(UIMODE.LOGIN);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    #endregion

    #region Show/Close Windows

    void showWindow(UIMODE _mode)
    {
        showWindow((int)_mode);
    }

    //for displaying in Editor
    public void showWindow(int  _mode)
    {
        //hide all UI
        ui_login.gameObject.SetActive(false);
        ui_signup.gameObject.SetActive(false);
        ui_gemplay.gameObject.SetActive(false);
        
        //show selected mode
        currentUIMode = (UIMODE)_mode;
        switch (currentUIMode)
        {
            case UIMODE.LOGIN:
                //setup textfield value
                ip_username.text = player.username;
                ip_password.text = null;
                //display
                ui_login.gameObject.SetActive(true);
                StartCoroutine(DelayOneFrame(() =>
                {
                    ui_login_feedback.PlayFeedbacks(); //show bounce effect
                }));
                break;
            case UIMODE.SIGNUP:
                //cleanup value
                ip_signup_password.text = "";
                ip_signup_username.text = "";
                ip_confirm_pwd.text = "";
                //display
                ui_signup.gameObject.SetActive(true);
                StartCoroutine(DelayOneFrame(() =>
                {
                    ui_signup_feedback.PlayFeedbacks(); //show bounce effect
                }));
                break;
            case UIMODE.GAMEPLAY:
                ui_gemplay.gameObject.SetActive(true);
                refreshValueDisplay();
                break;
        }
    }

    public void showNotificationBox(string _key)
    {
        t_msg.text = UI_Localization.Instance.GetLocalizedText(_key);
        ui_msgbox.gameObject.SetActive(true);
        StartCoroutine(DelayOneFrame(() =>
        {
            ui_msgbox_feedback.PlayFeedbacks(); //show bounce effect
        }));
    }

    public void closeNotificationWindow()
    {
        ui_msgbox.gameObject.SetActive(false);
    }
    
    public void closeSignupWindow()
    {
        showWindow(UIMODE.LOGIN);
    }

    private IEnumerator DelayOneFrame(Action callback)
    {
        yield return null; // Wait for one frame
        callback?.Invoke();
    }

    #endregion
    
    #region Login Windows

    //push login button
    public void push_login()
    {
        SoundManager.Instance.playSoundAction();
        //try login
        if (ValidateUserNameAndPassword())
        {
            apiConnect.LoginUser(ip_username.text,
                ip_password.text,
                (_success, message,respond) =>
                {
                    //in case of different display
                    if (_success)
                    {
                        player.setRespondData(respond); //set player data
                        showWindow(UIMODE.GAMEPLAY);
                    }
                    else
                    {
                        showNotificationBox(message);
                    }
                });
        }
    }
    
    #endregion

    #region Sign up Windows

    //push signup button
    public void push_signup()
    {
        SoundManager.Instance.playSoundAction();
           //try signin
           if (ValidateUserNameAndPassword())
           {
               apiConnect.RegisterUser(ip_signup_username.text,
                   ip_signup_password.text,
                   (_success, message,respond) =>
                   {
                       //in case of different display
                       if (_success)
                       {
                           player.clearValue(); //clear cache value
                           showNotificationBox(message);
                           showWindow(UIMODE.LOGIN);
                       }
                       else
                       {
                           showNotificationBox(message);
                       }
                   });
           }
    }
    
    #endregion

    #region Gameplay Windows

    public void push_logout(){
        showWindow(UIMODE.LOGIN);
    }
    
    public void push_addheart(bool _add){
            apiConnect.heartChange(player.user_id,
                (_add)?10:-10,
                (_success, message,respond) =>
                {
                    //in case of different display
                    if (_success)
                    {
                        player.setRespondData(respond); //set player data
                        refreshValueDisplay();
                        StartCoroutine(DelayOneFrame(() =>
                        {
                            heart_feedback.PlayFeedbacks(); //show bounce effect
                        }));
                    }
                    else
                    {
                        showNotificationBox(message);
                    }
                });
    }
    
    public void push_adddiamond(bool _add){
        apiConnect.diamondChange(player.user_id,
            (_add)?1000:-1000,
            (_success, message,respond) =>
            {
                //in case of different display
                if (_success)
                {
                    player.setRespondData(respond); //set player data
                    refreshValueDisplay();
                    StartCoroutine(DelayOneFrame(() =>
                    {
                        diamond_feedback.PlayFeedbacks(); //show bounce effect
                    }));
                }
                else
                {
                    showNotificationBox(message);
                }
            });
    }
    
    void refreshValueDisplay()
    {
        if(player == null)return;
        t_diamond_count.text = player.diamond.ToString();
        //set slider width
        img_heart_slider.rectTransform.sizeDelta = new Vector2(
            (((slider_width_max - slider_width_min)/100f) * (player.heart)) + slider_width_min
                , img_heart_slider.rectTransform.sizeDelta.y);
        img_heart_slider.gameObject.SetActive(player.heart>0);  //hide if heart == 0;
    }

    #endregion

    #region Validation

    bool ValidateUserNameAndPassword()
    {
        switch (currentUIMode)
        {
            case UIMODE.LOGIN:
                if (!(ip_username.text.Length > 0 && ip_password.text.Length > 0))
                {
                    showNotificationBox("nousernamorpassword");  //cant be blank
                    return false;
                }
                
                break;
            case UIMODE.SIGNUP:
                if (!System.Text.RegularExpressions.Regex.IsMatch(ip_signup_username.text, @"^[a-zA-Z0-9]+$")
                    || ip_signup_username.text.Length < 4)
                {
                    showNotificationBox("invalidusername");  //username must be alphabet or number and >= 4 chars
                    return false;
                }

                if (ip_signup_password.text.Length < 6)
                {
                    showNotificationBox("invalidpassword");  //password must be 6 character long
                    return false;
                }

                if (ip_signup_password.text != ip_confirm_pwd.text)
                {
                    showNotificationBox("invalidpasswordmismatch");  //password&confirm not match
                    return false;
                }
                break;
        }
        return true;
    }


    #endregion
    
    #region Sound

    public void playActionSound()
    {
        SoundManager.Instance.playSoundAction();
    }
    public void playCancelSound()
    {
        SoundManager.Instance.playSoundCancel();
    }

    #endregion    

}
