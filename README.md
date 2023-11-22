# Unity C# JSON APIClient

## Features

- **Integration:** Custom header support.
- **Bearer Token Authentication:** Easy bearer token management for secure API communication.
- **Asynchronous API Calls:** Utilizes `UniTask` for efficient asynchronous operations.
- **Customizable Serialization:** Flexible JSON serialization settings.
- **Error Handling:** Built-in methods to handle unauthorized access and non-OK responses.
- **Header Management:** Functions to set and remove default headers.
## Install with Unity Package Manager
- in Unity go to *Windows > Package Manager*
- press ` + ` and select ` Add package from git URL...`
```cmd
https://github.com/gbviktor/json-api-client-for-unity.git
```

- Ensure you have the necessary dependencies: `Newtonsoft.Json` and `Cysharp.Threading.Tasks`.
 
## Usage
### Initialization

```csharp
var client = new APIClient("https://api.yourserver.com");
client.SetBearerToken("your-bearer-token");
```

### Making GET Requests

```
var response = await client.GetAsync<YourResponseType>("endpoint");
// Handle response
```
### Making POST Requests

```cs
var requestData = new YourRequestType();
var response = await client.SendAsync<YourRequestType, YourResponseType>("endpoint", requestData);
// Handle response
```
### Error Handling

```cs
client.OnUnauthorized(() => {
    // Handle unauthorized access
});

client.OnRequestNotOk((statusCode) => {
    // Handle another response codes
});

```

### Setting Custom Headers

```cs
client.SetDefaultHeader("Custom-Header", "value");
```

## Notes

- This client is designed for Unity projects and uses Unity-specific features like `Debug.Log`.
- Make sure to handle exceptions and errors as per your project's requirements.
- Customize the client as needed to fit the specific needs of your API.

## Examples

### Example (Simple Get Call)

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
