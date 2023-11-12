using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Unity.Sync.Relay.Model;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Sync.Relay.Lobby
{

    /// <summary>
    /// 
    /// </summary>
    public class LobbyService
    {
        
        // // 每个接口，都可以有自己的默认超时时间
        // public static int DEFAULT_TIMEOUT = 2000;

        // 创建房间超时20秒
        public static int CREATE_OP_TIMEOUT = 20;

        public static string LobbyDomain = "https://uos.unity.cn/lobby";
        
        /// <summary>
        /// 异步列出房间列表（根据条件）
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IEnumerator AsyncListRoom(ListRoomRequest request, Action<ListRoomResponse> callback)
        {
            
            var url = LobbyDomain + "/api/rooms";
            // Dictionary<string, string> dict = request.ToDictionay();
            // byte[] ps = UnityWebRequest.SerializeSimpleForm(dict);
            //var uri = url + "?" + Encoding.UTF8.GetString(ps);
            var queryString = request.ToQueryString();
            var uri = String.IsNullOrEmpty(queryString) ? url : url + "?" + queryString;

            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // webRequest.SetRequestHeader("x-Region", ((int)region).ToString());
                // Request and wait for the desired page.
                
                webRequest.timeout = 10; // 单位为秒
                
                // webRequest.SetRequestHeader(APPID_KEY, RelaySettings.UosAppId);
                ConfigAuthentication(webRequest);
                yield return webRequest.SendWebRequest();
                var result = webRequest.downloadHandler.text;
                
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        // 比如创建房间个数超过limit
                        Debug.LogWarningFormat("list room on lobby fail!, response:{0}", result);
                        
                        callback?.Invoke(new ListRoomResponse(RelayCode.LobbyQueryRoomFailed));
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.LogFormat("list room on lobby, response: {0}", result);
                        var resp = JsonConvert.DeserializeObject<ListRoomResponseDto>(result);
                        var resp2 = ListRoomResponseDto.Create(resp);
                        resp2.Code = (uint)RelayCode.OK;
                        callback.Invoke(resp2);
                        break;
                    default:
                        Debug.LogWarningFormat("list room fail for unknown reason. current result {0} - {1}. Error : {2} ", webRequest.result, result, webRequest.error);
                        callback?.Invoke(new ListRoomResponse(RelayCode.LobbyQueryRoomFailed));
                        break;
                }
            }
            // yield break;
        }

        /// <summary>
        /// 异步查询房间信息
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IEnumerator AsyncQueryRoom(String roomId, Action<QueryRoomResponse> callback)
        {
            string url = LobbyDomain + "/api/rooms/" + roomId;

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.timeout = 10; // 单位为秒
                // webRequest.SetRequestHeader(APPID_KEY, RelaySettings.UosAppId);
                // webRequest.SetRequestHeader(AUTH_KEY, basicAuth);
                ConfigAuthentication(webRequest);
                yield return webRequest.SendWebRequest();
                var result = webRequest.downloadHandler.text;
                
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogWarningFormat("query room on lobby fail!, code: {0}, err1: {1}, err2: {2}", 
                            webRequest.error, webRequest.downloadHandler.error, webRequest.downloadHandler.text);
                        callback?.Invoke(new QueryRoomResponse(RelayCode.LobbyQueryRoomFailed));
                        break;

                    case UnityWebRequest.Result.Success:
                        Debug.LogFormat("join room on lobby success!, content: {0}", result);
                        var dto = JsonConvert.DeserializeObject<QueryRoomResponseDto>(result);
                        if (dto != null)
                        {
                            var resp = QueryRoomResponseDto.Create(dto);
                            resp.Code = (uint)RelayCode.OK;
                            callback?.Invoke(resp);
                        }
                        else
                        {
                            Debug.Log("parse query room data fail");
                            callback?.Invoke(new QueryRoomResponse(RelayCode.LobbyQueryRoomFailed));
                        }
                        break;
                    default:
                        callback?.Invoke(new QueryRoomResponse(RelayCode.LobbyQueryRoomFailed));
                        break;
                }
            }
            // yield break;
        }
        
        /// <summary>
        /// 异步创建房间
        /// </summary>
        /// <param name="req"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IEnumerator AsyncCreateRoom(CreateRoomRequest req, Action<CreateRoomResponse> callback)
        {
            var url = LobbyDomain + "/api/rooms";

            var checkCode = req.Check();
            if (checkCode != RelayCode.OK)
            {
                callback?.Invoke(new CreateRoomResponse(checkCode));
                yield break;
            }

            // *) 发生了变化
            var reqDto = CreateLobbyRoomRequestDto.Create(req);
            var jsonData = JsonConvert.SerializeObject(reqDto);

            CreateRoomResponse resp = null;
            using (UnityWebRequest webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                webRequest.timeout = CREATE_OP_TIMEOUT; // 单位为秒
                DownloadHandler downloadHandler = new DownloadHandlerBuffer();
                webRequest.downloadHandler = downloadHandler;
                webRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
                // webRequest.SetRequestHeader(APPID_KEY, RelaySettings.UosAppId);
                // webRequest.SetRequestHeader(AUTH_KEY, basicAuth);
                ConfigAuthentication(webRequest);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                yield return webRequest.SendWebRequest();
                var result = webRequest.downloadHandler.text;
                
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogWarningFormat("create room on lobby fail!, code: {0}, err1: {1}, err2: {2}", 
                            webRequest.error, webRequest.downloadHandler.error, webRequest.downloadHandler.text);
                        break;
                    
                    case UnityWebRequest.Result.Success:
                        Debug.LogFormat("create room on lobby success!, content: {0}", result);
                        CreateRoomResponseDto dto = JsonConvert.DeserializeObject<CreateRoomResponseDto>(result);
                        if (dto != null)
                        {
                            resp = CreateRoomResponseDto.Create(dto);
                        }
                        break;
                }
                webRequest.uploadHandler.Dispose();
            }
            
            if (resp == null || string.IsNullOrEmpty(resp.RoomUuid))
            {
                callback?.Invoke(new CreateRoomResponse(RelayCode.LobbyRoomUnknown));
                yield break;
            }

            LobbyRoomStatus curStatus = resp.Status;
            if (curStatus == LobbyRoomStatus.ServerAllocated)
            {
                Debug.Log("Create Room Successfully");
                resp.Code = (uint)RelayCode.OK;
                callback?.Invoke(resp);
                yield break;
            }
            else if (curStatus == LobbyRoomStatus.Created)
            {
                QueryRoomResponse resp2 = null;
                string roomId = resp.RoomUuid;
                // 进行下一步的尝试
                for (int i = 0; i < 5; i++)
                {
                    string url2 = LobbyDomain + "/api/rooms/" + roomId;
                    // 进行几次查询调用
                    using (UnityWebRequest webRequest = UnityWebRequest.Get(url2))
                    {
                        webRequest.timeout = 2; // 单位为秒
                        // webRequest.SetRequestHeader(APPID_KEY, RelaySettings.UosAppId);
                        // webRequest.SetRequestHeader(AUTH_KEY, basicAuth);
                        ConfigAuthentication(webRequest);
                        yield return webRequest.SendWebRequest();
                        var result = webRequest.downloadHandler.text;

                        switch (webRequest.result)
                        {
                            case UnityWebRequest.Result.ConnectionError:
                            case UnityWebRequest.Result.DataProcessingError:
                            case UnityWebRequest.Result.ProtocolError:
                                Debug.LogWarningFormat("join room on lobby fail!, code: {0}, err1: {1}, err2: {2}", 
                                    webRequest.error, webRequest.downloadHandler.error, webRequest.downloadHandler.text);
                                callback?.Invoke(new CreateRoomResponse(RelayCode.LobbyQueryRoomFailed));
                                break;

                            case UnityWebRequest.Result.Success:
                                Debug.LogFormat("join room on lobby success!, content: {0}", result);
                                QueryRoomResponseDto dto = JsonConvert.DeserializeObject<QueryRoomResponseDto>(result);
                                if (dto != null)
                                {
                                    resp2 = QueryRoomResponseDto.Create(dto);
                                }
                                break;
                        }
                    }

                    if (resp2 != null && resp2.Status == LobbyRoomStatus.ServerAllocated)
                    {
                        break;
                    }
                    yield return new WaitForSeconds(1);
                }

                if (resp2 == null)
                {
                    Debug.Log("Create room fail, try to query room, still fail");
                    callback?.Invoke(new CreateRoomResponse(RelayCode.LobbyCreateRoomFailed));
                    yield break;
                }

                CreateRoomResponse createRoomResponse = new CreateRoomResponse()
                {
                    Name = resp2.Name,
                    RoomUuid = resp2.RoomUuid,
                    OwnerId = resp2.OwnerId,
                    Namespace = resp2.Namespace,
                    JoinCode = resp2.JoinCode,
                    ServerInfo = resp2.ServerInfo,
                    Status = resp2.Status,
                    MaxPlayers = resp2.MaxPlayers,
                    Visibility = resp2.Visibility,
                    CustomProperties = resp2.CustomProperties,
                };
                createRoomResponse.Code = (uint)RelayCode.OK;
                callback?.Invoke(createRoomResponse);
            }
            else
            {
                Debug.LogFormat("Create Room Fail, lobby allocate room status: {0}", LobbyRoomStatusHelper.ValueOf(curStatus));
                callback?.Invoke(new CreateRoomResponse(RelayCode.LobbyCreateRoomFailed));    
            }
            // yield break;
        }
        
        /// <summary>
        /// 异步关闭房间
        /// </summary>
        /// <param name="roomUuid"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IEnumerator AsyncCloseRoom(string roomUuid, Action<CloseRoomResponse> callback)
        {
            // /api/rooms/:roomId/close
            var url = $"{LobbyDomain}/api/rooms/{roomUuid}/close";

            bool success = false;
            using (UnityWebRequest webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                webRequest.timeout = 10; // 单位为秒
                webRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
                // webRequest.SetRequestHeader(APPID_KEY, RelaySettings.UosAppId);
                // webRequest.SetRequestHeader(AUTH_KEY, basicAuth);
                ConfigAuthentication(webRequest);
                DownloadHandler downloadHandler = new DownloadHandlerBuffer();
                webRequest.downloadHandler = downloadHandler;
                // byte[] bodyRaw = Encoding.UTF8.GetBytes("");
                // webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                yield return webRequest.SendWebRequest();
                var result = webRequest.downloadHandler.text;
                
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogWarningFormat("close room on lobby fail!, code: {0}, err1: {1}, err2: {2}", 
                            webRequest.error, webRequest.downloadHandler.error, webRequest.downloadHandler.text);
                        
                        callback?.Invoke(new CloseRoomResponse(RelayCode.LobbyCloseRoomFailed));
                        break;
                    
                    case UnityWebRequest.Result.Success:
                        Debug.LogFormat("close room on lobby success!, content: {0}", result);
                        success = true;
                        break;
                }
            }

            if (success)
            {
                callback?.Invoke(new CloseRoomResponse(RelayCode.OK));
            }
            else
            {
                callback?.Invoke(new CloseRoomResponse(RelayCode.LobbyCloseRoomFailed));
            }
        }
        
        public static IEnumerator ChangeRoomStatus(String roomUuid, LobbyRoomStatus status, Action<ChangeRoomStatusResponse> callback)
        {
            var url = string.Format("{0}/api/rooms/{1}/status/{2}", LobbyDomain, roomUuid, LobbyRoomStatusHelper.ValueOf(status));

            if (status != LobbyRoomStatus.Ready && status != LobbyRoomStatus.Running)
            {
                Debug.Log("Only Support Ready and Running Status");
                yield break;
            }

            using (UnityWebRequest webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
            {
                webRequest.timeout = CREATE_OP_TIMEOUT; // 单位为秒
                DownloadHandler downloadHandler = new DownloadHandlerBuffer();
                webRequest.downloadHandler = downloadHandler;
                webRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
                ConfigAuthentication(webRequest);
                yield return webRequest.SendWebRequest();
                var result = webRequest.downloadHandler.text;
                
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogWarningFormat("change room status fail!, code: {0}, err1: {1}, err2: {2}", 
                            webRequest.error, webRequest.downloadHandler.error, webRequest.downloadHandler.text);
                        callback?.Invoke(new ChangeRoomStatusResponse(RelayCode.LobbyRoomUnknown));
                        break;
                    
                    case UnityWebRequest.Result.Success:
                        Debug.LogFormat("change room status success!, content: {0}", result);
                        callback?.Invoke(new ChangeRoomStatusResponse());
                        break;
                }
                webRequest.downloadHandler.Dispose();
            }
        }

        private static void ConfigAuthentication(UnityWebRequest webRequest)
        {
            // 随机生成一个
            string nonce = Guid.NewGuid().ToString();
            // 切记一定要是UtcNow
            long timestamp = EncipherUtility.GetUnixTimeStampSeconds(DateTime.UtcNow);
            string data = $"{RelaySettings.UosAppId}:{RelaySettings.UosAppSecret}:{timestamp}:{nonce}";
            string authData = EncipherUtility.hexString(EncipherUtility.sha256(data));
            
            webRequest.SetRequestHeader("X-APPID", RelaySettings.UosAppId);
            webRequest.SetRequestHeader("X-TIMESTAMP", $"{timestamp}");
            webRequest.SetRequestHeader("X-NONCE", nonce);
            webRequest.SetRequestHeader("Authorization", "Bearer " + authData);
            
            webRequest.SetRequestHeader("X-SDK-NAME", "SYNC_RELAY");
            webRequest.SetRequestHeader("X-SDK-VERSION", "1.0.13");
        }
    }

    public static class EncipherUtility
    {
        public static byte[] sha256(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            using (SHA256 mySHA256 = SHA256.Create())
            {
                byte[] hash = mySHA256.ComputeHash(bytes);
                return hash;
            }
        }

        public static string hexString(byte[] data)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                builder.Append(data[i].ToString("X2").ToLower());
            }
            return builder.ToString();
        }
        
        public static long GetUnixTimeStampSeconds(DateTime dt)
        {
            DateTime dateStart = new DateTime(1970, 1, 1, 0, 0, 0);
            return Convert.ToInt64((dt - dateStart).TotalSeconds);
        }

    }

}