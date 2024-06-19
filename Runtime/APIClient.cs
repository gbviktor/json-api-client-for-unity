using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

using UnityEngine;

namespace MontanaGames.JsonAPIClient
{

    public class APIClient
    {
        public bool IsConnected { get; private set; }

        #region Header for Editor
#if UNITY_EDITOR
        private const string EditorHeader = "x-unity-editor";

        public APIClient SetEditorAPIKey(string key)
        {
            SetDefaultHeader(EditorHeader, key);
            return this;
        }

#endif
        #endregion

        #region Bearer Token Header
        private string bearerToken;
        private const string Scheme = "Bearer";
        public string BearerToken
        {
            get => bearerToken;
            set
            {
                bearerToken = value;
                ApplyBearerTokenHeader();
            }
        }
        public APIClient SetBearerToken(string bearerToken)
        {
            BearerToken = bearerToken;
            return this;
        }
        private void ApplyBearerTokenHeader()
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Scheme, bearerToken);
        }
        #endregion

        #region Client Events
        private Action<int> onRequestNotOk;
        private Action onUnauthorized;
        private Action<string> onNetworkError;
        private Action<string> onServerError;

        public APIClient OnUnauthorized(Action onUnauthorized)
        {
            this.onUnauthorized = onUnauthorized;
            return this;
        }

        public APIClient OnNetworkError(Action<string> onNetworkError)
        {
            this.onNetworkError = onNetworkError;
            return this;
        }

        public APIClient OnServerError(Action<string> onServerError)
        {
            this.onServerError = onServerError;
            return this;
        }

        /// <summary>
        /// Set up an Event, what will be called, if responce code is not 200 (ok)
        /// </summary>
        /// <param name="onRequestNotOk">HTTP Status Code, you can use <see cref="HttpStatusCode"/> </param>
        /// <returns></returns>
        public APIClient OnRequestNotOk(Action<int> onRequestNotOk)
        {
            this.onRequestNotOk = onRequestNotOk;
            return this;
        }

        #endregion

        #region Serialization Settings
        private readonly JsonSerializer serializer;
        private readonly JsonSerializerSettings SerializerSettings;
        private string ToJsonString<T>(T data)
        {
            return JsonConvert.SerializeObject(data, SerializerSettings);
        }
        #endregion

        #region Init/Constructor 
        private readonly string baseUrl;
        private readonly HttpClient client;
        public APIClient(string baseUrl, JsonSerializerSettings SerializerSettings = default, JsonSerializer serializer = default) : base()
        {
            this.baseUrl = baseUrl;


            this.SerializerSettings = SerializerSettings ?? new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                NullValueHandling = NullValueHandling.Ignore
            };

            this.serializer = serializer ?? new JsonSerializer()
            {
                NullValueHandling = this.SerializerSettings.NullValueHandling,
                ReferenceLoopHandling = this.SerializerSettings.ReferenceLoopHandling,
            };

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };

            client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(19);
        }
        
        public APIClient SetDefaultHeader(string headerName, string headerValue)
        {
            client.DefaultRequestHeaders.Remove(headerName);
            client.DefaultRequestHeaders.Add(headerName, headerValue);
            return this;
        }
        public APIClient RemoveDefaultHeader(string headerName)
        {
            client.DefaultRequestHeaders.Remove(headerName);
            return this;
        }
        #endregion

        public async UniTask<ResponseType> GetAsync<ResponseType>(string path, Action onError = default)
        {
            try
            {
                var requestUrl = $"{baseUrl}/{path}";
                var response = await client.GetAsync(requestUrl);

                await using var getSteam = await response.Content.ReadAsStreamAsync();

                using var sr = new StreamReader(getSteam);
                using JsonReader reader = new JsonTextReader(sr);

                var statusCode = response.StatusCode;
                switch (statusCode)
                {
                    case HttpStatusCode.OK:
                    {
                        IsConnected = true;
                        var res = serializer.Deserialize<ResponseType>(reader);

                        var headers = response.Headers;
                        ApplyBearerTokenFromResponseHeader(headers);
                    
                        return res;
                    }
                    case HttpStatusCode.Unauthorized:
                        onUnauthorized?.Invoke();
                        break;
                    default:
                        onError?.Invoke();
                        onRequestNotOk?.Invoke((int)statusCode);
                        Debug.Log($"{response.StatusCode}:{response.Content}");
                        break;
                }

            } 
            catch (HttpRequestException ex)
            {
                onServerError?.Invoke($"Server error: {ex.Message}");
                Debug.LogError($"Server error: {ex.Message}");
            } 
            catch (Exception ex)
            {
                IsConnected = false;
                onNetworkError?.Invoke($"Network error: {ex.Message}");
                Debug.LogError($"Network error: {ex.Message}");
            }

            return default;
        }

        public async UniTask<ResponseType> SendAsync<RequestType, ResponseType>(string relativePath, RequestType data, Action onError = default)
        {
            try
            {
                var requestUrl = $"{baseUrl}/{relativePath}";

                var response = await client.PostAsync(requestUrl,
                    new StringContent(ToJsonString(data), Encoding.UTF8));

                var responseStream = await response.Content.ReadAsStreamAsync();

                using var sr = new StreamReader(responseStream);
                using JsonReader reader = new JsonTextReader(sr);

                var statusCode = response.StatusCode;
                switch (statusCode)
                {
                    case HttpStatusCode.OK:
                    {
                        IsConnected = true;
                        var responseObject = serializer.Deserialize<ResponseType>(reader);
                    
                        var headers = response.Headers;
                        ApplyBearerTokenFromResponseHeader(headers);
                    
                        return responseObject;
                    }
                    case HttpStatusCode.Unauthorized:
                        onUnauthorized?.Invoke();
                        break;
                    default:
                        onError?.Invoke();
                        onRequestNotOk?.Invoke((int)statusCode);
                        Debug.Log($"{statusCode}:{response.Content}");
                        break;
                }
            }
            catch (HttpRequestException ex)
            {
                onServerError?.Invoke($"Server error: {ex.Message}");
                Debug.LogError($"Server error: {ex.Message}");
            } 
            catch (Exception ex)
            {
                IsConnected = false;
                onNetworkError?.Invoke($"Network error: {ex.Message}");
                Debug.LogError($"Network error: {ex.Message}");
            }

            return default;
        }

        #region Utilities

        private void ApplyBearerTokenFromResponseHeader([NotNull] HttpResponseHeaders headers)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (headers.TryGetValues("X-Authorization", out var keys))
            {
                BearerToken = keys.FirstOrDefault();
            }
        }
        #endregion
    }
}
