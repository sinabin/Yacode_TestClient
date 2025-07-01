using System.Net.Sockets;
using System.Text.Json;

namespace Yacode_TestClient.Services
{
    public class YacodeClientService : IDisposable
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private bool _isConnected = false;
        private readonly object _lockObject = new object();

        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<string>? MessageReceived;
        public event EventHandler<string>? ErrorOccurred;

        public bool IsConnected 
        { 
            get 
            { 
                lock (_lockObject)
                {
                    return _isConnected && _tcpClient?.Connected == true;
                }
            } 
        }

        /// <summary>
        /// 프린터에 연결
        /// </summary>
        /// <param name="ipAddress">프린터 IP 주소</param>
        /// <param name="port">포트 (기본값: 20001)</param>
        /// <returns>연결 성공 여부</returns>
        public async Task<bool> ConnectAsync(string ipAddress, int port = 20001)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isConnected)
                    {
                        return true;
                    }
                }

                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ipAddress, port);
                
                if (_tcpClient.Connected)
                {
                    _stream = _tcpClient.GetStream();
                    
                    lock (_lockObject)
                    {
                        _isConnected = true;
                    }
                    
                    ConnectionStatusChanged?.Invoke(this, true);
                    
                    // 연결 확인을 위해 시스템 상태 요청
                    var testMessage = YacodeProtocolMessage.CreateSystemStatusRequest();
                    await SendMessageAsync(testMessage);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"연결 실패: {ex.Message}");
                await DisconnectAsync();
            }
            
            return false;
        }

        /// <summary>
        /// 프린터 연결 해제
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    _isConnected = false;
                }

                if (_stream != null)
                {
                    await _stream.FlushAsync();
                    _stream.Close();
                    _stream = null;
                }

                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient = null;
                }

                ConnectionStatusChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"연결 해제 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지 전송
        /// </summary>
        public async Task<bool> SendMessageAsync(YacodeProtocolMessage message)
        {
            try
            {
                if (!IsConnected || _stream == null)
                {
                    ErrorOccurred?.Invoke(this, "프린터가 연결되지 않았습니다.");
                    return false;
                }

                var messageBytes = message.ToByteArray();
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                await _stream.FlushAsync();

                // 응답 수신
                await ReceiveResponseAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 응답 수신
        /// </summary>
        private async Task ReceiveResponseAsync()
        {
            try
            {
                if (_stream == null) return;

                var buffer = new byte[4096];
                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                
                if (bytesRead > 0)
                {
                    var responseData = new byte[bytesRead];
                    Array.Copy(buffer, responseData, bytesRead);
                    
                    var responseMessage = YacodeProtocolMessage.FromByteArray(responseData);
                    if (responseMessage != null)
                    {
                        MessageReceived?.Invoke(this, responseMessage.Data);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"응답 수신 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 시스템 상태 요청
        /// </summary>
        public async Task<bool> GetSystemStatusAsync()
        {
            var message = YacodeProtocolMessage.CreateSystemStatusRequest();
            return await SendMessageAsync(message);
        }

        /// <summary>
        /// 인쇄 상태 요청
        /// </summary>
        public async Task<bool> GetPrintingStatusAsync(int groupId = 0)
        {
            var message = YacodeProtocolMessage.CreatePrintingStatusRequest(groupId);
            return await SendMessageAsync(message);
        }

        /// <summary>
        /// 테스트 정보 요청
        /// </summary>
        public async Task<bool> GetTestInformationAsync()
        {
            var message = YacodeProtocolMessage.CreateTestInformationRequest();
            return await SendMessageAsync(message);
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
        
        
        /// <summary>
        /// 프린터에게 Start Printing 명령 전송
        /// </summary>
        public async Task<bool> StartPrintingAsync(string templateFileName = "100.ym")
        {
            try
            {
                var payload = new
                {
                    template = templateFileName
                };

                var message = new YacodeProtocolMessage
                {
                    ProtocolMark = YacodeProtocolMessage.ProtocolMarks.START_PRINTING,
                    Data = JsonSerializer.Serialize(payload)
                };

                return await SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"StartPrinting 명령 전송 실패: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendDynamicContentAsync(string contentType, object contentData)
        {
            try
            {
                var payload = new
                {
                    type = contentType,      // 예: "text", "image"
                    data = contentData       // 예: 문자열 or Base64 인코딩된 이미지
                };

                var message = new YacodeProtocolMessage
                {
                    ProtocolMark = YacodeProtocolMessage.ProtocolMarks.SET_DYNAMIC_DATA,
                    Data = JsonSerializer.Serialize(payload)
                };

                return await SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"콘텐츠 전송 실패: {ex.Message}");
                return false;
            }
        }
        
        public async Task<List<string>> GetRecentTemplateNamesAsync()
        {
            var templateNames = new List<string>();

            try
            {
                var request = new YacodeProtocolMessage
                {
                    ProtocolMark = YacodeProtocolMessage.ProtocolMarks.GET_PRINTING_CACHE,
                    Data = JsonSerializer.Serialize(new { print_head_id = 0 })
                };

                string? responseJson = null;

                TaskCompletionSource<string> tcs = new();

                void Handler(object? sender, string data)
                {
                    responseJson = data;
                    tcs.TrySetResult(data);
                }

                MessageReceived += Handler;

                var sent = await SendMessageAsync(request);
                if (!sent)
                {
                    MessageReceived -= Handler;
                    return templateNames;
                }

                // 최대 3초 대기
                var task = await Task.WhenAny(tcs.Task, Task.Delay(3000));
                MessageReceived -= Handler;

                if (task != tcs.Task || string.IsNullOrWhiteSpace(responseJson))
                {
                    ErrorOccurred?.Invoke(this, "템플릿 캐시 응답 수신 실패");
                    return templateNames;
                }

                // 파싱
                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("template_name", out var nameProp) &&
                            nameProp.ValueKind == JsonValueKind.String)
                        {
                            var name = nameProp.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                                templateNames.Add(name!);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"템플릿 목록 가져오기 실패: {ex.Message}");
            }

            return templateNames;
        }

        public async Task<List<string>> GetPrintingLogTemplateNamesAsync()
        {
            var templateNames = new List<string>();

            try
            {
                var request = new YacodeProtocolMessage
                {
                    ProtocolMark = YacodeProtocolMessage.ProtocolMarks.GET_PRINTING_LOG,
                    Data = JsonSerializer.Serialize(new { group_id = 0 })
                };

                string? responseJson = null;
                TaskCompletionSource<string> tcs = new();

                void Handler(object? sender, string data)
                {
                    responseJson = data;
                    tcs.TrySetResult(data);
                }

                MessageReceived += Handler;

                var sent = await SendMessageAsync(request);
                if (!sent)
                {
                    MessageReceived -= Handler;
                    return templateNames;
                }

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(3000));
                MessageReceived -= Handler;

                if (completedTask != tcs.Task || string.IsNullOrWhiteSpace(responseJson))
                {
                    ErrorOccurred?.Invoke(this, "프린팅 로그 응답 수신 실패");
                    return templateNames;
                }

                // 응답 로그 출력 (디버깅용)
                Console.WriteLine($"응답 원문: {responseJson}");

                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        foreach (var prop in element.EnumerateObject())
                        {
                            // "template_name" 또는 "filename" 또는 "file_name" 등 추정
                            if (prop.Name.ToLower().Contains("name") && prop.Value.ValueKind == JsonValueKind.String)
                            {
                                var name = prop.Value.GetString();
                                if (!string.IsNullOrWhiteSpace(name) && !templateNames.Contains(name))
                                    templateNames.Add(name!);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"프린팅 로그 조회 실패: {ex.Message}");
            }

            return templateNames;
        }


    }
}