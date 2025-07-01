using System.Collections.ObjectModel;
using System.IO;
using Yacode_TestClient.Services;

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
        private string _printerIpAddress = "192.168.11.50";

        [ObservableProperty]
        private string _connectionStatus = "연결되지 않음";

        [ObservableProperty]
        private string _lastResponse = string.Empty;

        [ObservableProperty]
        private string logMessages = string.Empty;
        
        [ObservableProperty]
        private string printMessage = string.Empty;

        [ObservableProperty]
        private string imageFilePath = string.Empty;
        
        [ObservableProperty]
        private bool showResultInfo;

        [ObservableProperty]
        private string resultMessage = string.Empty;

        [ObservableProperty]
        private string resultSeverity = "Success"; // "Success", "Error", "Warning", "Info"
        
        [ObservableProperty]
        private ObservableCollection<string> recentTemplateNames = new();

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
        
        [RelayCommand]
        private void SelectImage()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImageFilePath = openFileDialog.FileName;
            }
        }

        
        [RelayCommand]
        private async Task SendToPrinter()
        {
            if (string.IsNullOrWhiteSpace(PrintMessage))
            {
                SetResult("메시지를 입력해주세요.", "Warning");
                AddLogMessage("⚠️ 전송 실패: 메시지가 비어 있습니다.");
                return;
            }

            var payload = new Dictionary<string, object>
            {
                { "message", PrintMessage }
            };

            if (!string.IsNullOrWhiteSpace(ImageFilePath) && File.Exists(ImageFilePath))
            {
                var base64Image = Convert.ToBase64String(File.ReadAllBytes(ImageFilePath));
                payload["image"] = base64Image;
                payload["image_format"] = Path.GetExtension(ImageFilePath).Trim('.');
                AddLogMessage("📦 이미지 포함하여 전송 준비 완료");
            }
            else
            {
                AddLogMessage("✉️ 텍스트만 포함하여 전송 준비 완료");
            }

            AddLogMessage("📤 프린터로 데이터 전송 중...");

            // 1. 동적 콘텐츠 전송
            var success = await _yacodeClient.SendDynamicContentAsync("text+image", payload);

            if (!success)
            {
                SetResult("프린터 전송 실패!", "Error");
                AddLogMessage("❌ 프린터 전송 실패");
                return;
            }

            AddLogMessage("✅ 프린터 전송 성공");

            // 2. Start Printing 호출
            string templateName = "100.ym"; // 실제 프린터에 업로드된 템플릿 이름
            AddLogMessage($"🖨️ StartPrinting 명령 호출: {templateName}");
            var startResult = await _yacodeClient.StartPrintingAsync(templateName);

            if (startResult)
            {
                SetResult("프린터 인쇄 시작!", "Success");
                AddLogMessage("✅ 인쇄 시작 명령 전송 완료");
            }
            else
            {
                SetResult("인쇄 시작 실패!", "Error");
                AddLogMessage("❌ 인쇄 시작 명령 실패");
            }
        }



        private void SetResult(string message, string severity)
        {
            ResultMessage = message;
            ResultSeverity = severity;
            ShowResultInfo = true;
        }
        [RelayCommand]
        private void ClearResult()
        {
            ShowResultInfo = false;
            ResultMessage = string.Empty;
        }
        
        [RelayCommand]
        private async Task LoadRecentTemplatesAsync()
        {
            if (!_yacodeClient.IsConnected)
            {
                AddLogMessage("❌ 프린터가 연결되지 않았습니다.");
                return;
            }

            AddLogMessage("📥 최근 템플릿 목록 조회 중...");

            var names = await _yacodeClient.GetPrintingLogTemplateNamesAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                RecentTemplateNames.Clear();
                foreach (var name in names.Distinct())
                    RecentTemplateNames.Add(name);
            });

            AddLogMessage("📋 최근 템플릿 목록 불러오기 완료");
        }
    }
}