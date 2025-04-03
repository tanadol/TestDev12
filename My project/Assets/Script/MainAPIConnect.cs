using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public enum APICALL
{
    NONE = 0,
    SIGNUP,
    LOGIN,
    HEARTCHANGE,
    DIAMONDCHANGE,
    COUNT
};

[System.Serializable]

public class ApiResponseData
{
    public string status;
    public string message;
    //for user_data return
    public int user_id;
    public string username;
    public int diamond;
    public int heart;
}

public class MainAPIConnect : MonoBehaviour
{
    //to add more api
    //1. add APICALL enum value
    //2. create new static string
    //3. adding it to apiArray
    private static string baseUrl = "https://test-piggy.codedefeat.com/worktest/dev12/";
    private static string signupUrl = "sign_up.php";
    private static string loginUrl = "login.php";
    private static string heartChangeUrl = "add_heart.php";
    private static string diamonsChangeUrl = "add_diamond.php";
    private string[] apiArray =  {"",signupUrl,loginUrl,heartChangeUrl,diamonsChangeUrl};   //follow APICALL sequence;
    
    private string s_username, s_password;
    
    public delegate void ApiResponse(bool _success,string _message,ApiResponseData _responseData);
    //public event ApiResponse OnApiResponse;

    #region public Call API

    public void RegisterUser(string username, string password,ApiResponse _OnApiResponse)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);
        StartCoroutine(callPOSTCoroutine(APICALL.SIGNUP, form, _OnApiResponse));
    }
    
    
    public void LoginUser(string username, string password,ApiResponse _OnApiResponse)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);
        StartCoroutine(callPOSTCoroutine(APICALL.LOGIN, form, _OnApiResponse));
    }
    
    public void heartChange(int user_id, int amount,ApiResponse _OnApiResponse)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", user_id);
        form.AddField("heart_change", amount);
        StartCoroutine(callPOSTCoroutine(APICALL.HEARTCHANGE, form, _OnApiResponse));
    }
    
    public void diamondChange(int user_id, int amount,ApiResponse _OnApiResponse)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", user_id);
        form.AddField("diamond_change", amount);
        StartCoroutine(callPOSTCoroutine(APICALL.DIAMONDCHANGE, form, _OnApiResponse));
    }
    
    #endregion

    #region Internal work

    private IEnumerator callPOSTCoroutine(APICALL _api, WWWForm form, ApiResponse _OnApiResponse)
    {
        string url = baseUrl+apiArray[(int)_api];

        UnityWebRequest request = UnityWebRequest.Post(url, form);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Raw Response: " + request.downloadHandler.text);

            try
            {
                // Parse the JSON response using Unity's JsonUtility
                ApiResponseData response = JsonUtility.FromJson<ApiResponseData>(request.downloadHandler.text);

                if (response != null)
                {
                    if (response.status == "success")
                    {
                        _OnApiResponse?.Invoke(true, response.message, response);
                        Debug.Log("Success: " + response.message);
                    }
                    else
                    {
                        _OnApiResponse?.Invoke(false, response.message, response);
                        Debug.LogError("Error: " + response.message);
                    }
                }
                else
                {
                    _OnApiResponse?.Invoke(false, "Failed to parse JSON response.", null);
                    Debug.LogError("Failed to parse JSON response.");
                }
            }
            catch (System.Exception ex)
            {
                _OnApiResponse?.Invoke(false, "Exception during JSON parsing: " + ex.Message, null);
                Debug.LogError("Exception during JSON parsing: " + ex.Message);
            }
        }
        else
        {
            _OnApiResponse?.Invoke(false, "networkerror", null);
            Debug.LogError("Network Error: " + request.error);
        }
    }

    #endregion
}
