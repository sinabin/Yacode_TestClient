using Yacode_TestClient.Services;
using System.Windows;

namespace Yacode_TestClient.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly YacodeClientService _yacodeClient;

        [ObservableProperty]
        private int _counter = 0;

        [ObservableProperty]
        private bool _isConnected = false;

        [ObservableProperty]
        private string _printerIpAddress = "169.254.14.230";

        [ObservableProperty]
        private string _connectionStatus = "연결되지 않음";

        [ObservableProperty]
        private string _lastResponse = string.Empty;

        [ObservableProperty]
        private string _logMessages = string.Empty;

        public DashboardViewModel(YacodeClientService yacodeClient)
        {
            _yacodeClient = yacodeClient;
            
            // 이벤트 구독
            _yacodeClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            _yacodeClient.MessageReceived += OnMessageReceived;
            _yacodeClient.ErrorOccurred += OnErrorOccurred;
        }

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }

        [RelayCommand]
        private async Task ConnectToPrinter()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PrinterIpAddress))
                {
                    AddLogMessage("IP 주소를 입력해주세요.");
                    return;
                }

                AddLogMessage($"프린터 연결 시도: {PrinterIpAddress}");
                ConnectionStatus = "연결 중...";

                var success = await _yacodeClient.ConnectAsync(PrinterIpAddress);
                
                if (success)
                {
                    AddLogMessage("프린터 연결 성공!");
                }
                else
                {
                    AddLogMessage("프린터 연결 실패!");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"연결 오류: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DisconnectFromPrinter()
        {
            try
            {
                AddLogMessage("프린터 연결 해제 중...");
                await _yacodeClient.DisconnectAsync();
                AddLogMessage("프린터 연결 해제 완료");
            }
            catch (Exception ex)
            {
                AddLogMessage($"연결 해제 오류: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task GetSystemStatus()
        {
            if (!IsConnected)
            {
                AddLogMessage("프린터가 연결되지 않았습니다.");
                return;
            }

            try
            {
                AddLogMessage("시스템 상태 요청 중...");
                await _yacodeClient.GetSystemStatusAsync();
            }
            catch (Exception ex)
            {
                AddLogMessage($"시스템 상태 요청 오류: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task GetPrintingStatus()
        {
            if (!IsConnected)
            {
                AddLogMessage("프린터가 연결되지 않았습니다.");
                return;
            }

            try
            {
                AddLogMessage("인쇄 상태 요청 중...");
                await _yacodeClient.GetPrintingStatusAsync();
            }
            catch (Exception ex)
            {
                AddLogMessage($"인쇄 상태 요청 오류: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task GetTestInformation()
        {
            if (!IsConnected)
            {
                AddLogMessage("프린터가 연결되지 않았습니다.");
                return;
            }

            try
            {
                AddLogMessage("테스트 정보 요청 중...");
                await _yacodeClient.GetTestInformationAsync();
            }
            catch (Exception ex)
            {
                AddLogMessage($"테스트 정보 요청 오류: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ClearLog()
        {
            LogMessages = string.Empty;
        }

        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = isConnected;
                ConnectionStatus = isConnected ? "연결됨" : "연결되지 않음";
            });
        }

        private void OnMessageReceived(object? sender, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LastResponse = message;
                AddLogMessage($"응답 수신: {message}");
            });
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddLogMessage($"오류: {error}");
            });
        }

        private void AddLogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogMessages += $"[{timestamp}] {message}\n";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _yacodeClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}