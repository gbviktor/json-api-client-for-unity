using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Text;

using Cysharp.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;

namespace MontanaGames.JsonAPIClient
{

    public class APIClient
    {
        public bool IsConnected
        {
            get;
            set;
        }

        #region Header for Editor
#if UNITY_EDITOR
        const string EditorHeader = "x-unity-editor";

        string EditorHeaderValue = "";

        public APIClient SetEditorAPIKey(string key)
        {
            EditorHeaderValue = key;
            ApplyEditorHeader();
            return this;
        }

#endif
        private void ApplyEditorHeader()
        {
#if UNITY_EDITOR
            client.DefaultRequestHeaders.Remove(EditorHeader);
            client.DefaultRequestHeaders.Add(EditorHeader, EditorHeaderValue);
#endif
        }
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
        private Action onUnathorized;

        public APIClient OnUnauthorized(Action onUnathorized)
        {
            this.onUnathorized = onUnathorized;
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

            ApplyEditorHeader();
        }

        #endregion

        public async UniTask<RESPONSE> GetAsync<RESPONSE>(string path, Action onError = default)
        {
            try
            {
                var requestUrl = $"{baseUrl}/{path}";
                var response = await client.GetAsync(requestUrl);

                using var getSteam = await response.Content.ReadAsStreamAsync();

                using StreamReader sr = new StreamReader(getSteam);
                using JsonReader reader = new JsonTextReader(sr);

                var statusCode = response.StatusCode;
                if (statusCode == HttpStatusCode.OK)
                {
                    IsConnected = true;
                    RESPONSE res = serializer.Deserialize<RESPONSE>(reader);
                    
                    if (response.Headers.TryGetValues("X-Authorization", out var keys))
                    {
                        BearerToken = keys.FirstOrDefault();
                    }

                    return res;
                }
                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    onUnathorized?.Invoke();
                } else
                {
                    onError?.Invoke();
                    onRequestNotOk?.Invoke((int)statusCode);
                    Debug.Log($"{response.StatusCode}:{response.Content}");
                }

            } catch (Exception er)
            {
                IsConnected = false;
                Debug.LogError(er);
            }

            return default;
        }

        public async UniTask<RESPONSE_TYPE> SendAsync<REQUEST_TYPE, RESPONSE_TYPE>(string relativePath, REQUEST_TYPE data)
        {
            try
            {
                var requestUrl = $"{baseUrl}/{relativePath}";

                var httpResponse = await client.PostAsync(requestUrl,
                    new StringContent(ToJsonString(data), Encoding.UTF8));

                var s = await httpResponse.Content.ReadAsStreamAsync();

                using StreamReader sr = new StreamReader(s);
                using JsonReader reader = new JsonTextReader(sr);

                var statusCode = httpResponse.StatusCode;
                if (statusCode == HttpStatusCode.OK)
                {
                    IsConnected = true;
                    RESPONSE_TYPE res = serializer.Deserialize<RESPONSE_TYPE>(reader);
                    
                    if (httpResponse.Headers.TryGetValues("X-Authorization", out var keys))
                    {
                        BearerToken = keys.FirstOrDefault();
                    }
                    
                    return res;
                } else if (statusCode == HttpStatusCode.Unauthorized)
                {
                    onUnathorized?.Invoke();
                } else
                {
                    Debug.Log($"{statusCode}:{httpResponse.Content}");
                }

            } catch (Exception er)
            {
                IsConnected = false;
                Debug.LogError(er);
            }

            return default;
        }
    }
}
