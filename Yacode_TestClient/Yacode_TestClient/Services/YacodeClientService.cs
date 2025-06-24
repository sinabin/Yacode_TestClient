using System.Net.Sockets;

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
    }
}