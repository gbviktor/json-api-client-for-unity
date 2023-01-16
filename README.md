## Json API Client for Unity
This Package simplify async (UniTask) API requests from Unity 
 - Authorization with Bearer Scheme only, but you can implement other types by yourself

## Install with Unity Package Manager
- in Unity go to *Windows > Package Manager*
- press ` + ` and select ` Add package from git URL...`
```cmd
https://github.com/gbviktor/json-api-client-for-unity.git
```

## How to use

```csharp
[SerializeField] private string url = "http://localhost:8080";

//Cache client and set onUnathorized Event, to login automaticaly
private APIClient client => _client ??= new APIClient(url)
.OnUnauthorized(() =>
  {
    Debug.Log("Try Authorize with your");

    //var token = SteamAuth();
    client.SetBearerToken("put client token here");
  });
		
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


```

### Example 2 (Simple Get Call)

```csharp 

[ContextMenu("External API Call")]
public async Task SimpleSomeOtherGetApiCall()
{
	//cahce your APIClient to reuse resources, like first example in field Client
	var r = await new APIClient("https://api.agify.io").GetAsync<UserInfo>("?name=michael");

	Debug.Log($"Name: {r.Name}\r\nAge: {r.Age}\r\nCount: {r.Count}");
}

```

## Dependencies
- [UniTask](https://github.com/Cysharp/UniTask) 
- Newtonsoft.Json
