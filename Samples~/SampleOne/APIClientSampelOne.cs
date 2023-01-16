using System.Threading.Tasks;

using MontanaGames.API;

using Newtonsoft.Json;

using UnityEngine;

public class APIClientSampelOne : MonoBehaviour
{
    public APIClient _client;
    [SerializeField] private string apiBaseUrl = "http://localhost:8085";

    //Cache client and set onUnathorized Event, to login automaticaly
    private APIClient client => _client ??= new APIClient(apiBaseUrl)
        .OnUnauthorized(() =>
        {
            Debug.Log("Try Authorize");

            //client.SetBearerToken("some token from api call");
            if (client.BearerToken == "put client token here")
            {
                //your token is invalid, break execution to avoid recursive call
            }
        });

    //send data by make POST request
    [ContextMenu("Update User with POST method")]
    public async Task UpdateUserInfoByID()
    {
        var demoUser = new UserInfo() { Age = 33, Count = 11, Name = "Demo User" };

        var r = await client.SendAsync<UserInfo, ResponseStatus>($"users", demoUser);

        Debug.Log($"User Updated: {r.Success}");
    }

    //make GET request
    [ContextMenu("Test API Date")]
    public async Task GetUserInfoByID(int uid)
    {
        var r = await client.GetAsync<UserInfo>($"users/{uid}");

        Debug.Log($"Name: {r.Name}\r\nAge: {r.Age}\r\nCount: {r.Count}");
    }
    //some test struct
    public struct ResponseStatus
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
    //some test struct
    public struct UserInfo
    {
        [JsonProperty("age")]
        public int Age { get; set; }
        [JsonProperty("count")]
        public int Count { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
