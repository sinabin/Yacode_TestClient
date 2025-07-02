using System.Collections.ObjectModel;
using System.IO;
using Yacode_TestClient.Services;

namespace Yacode_TestClient.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly YacodeClientService _yacodeClient;

        [ObservableProperty] private int _counter = 0;

        [ObservableProperty] private bool _isConnected = false;

        [ObservableProperty] private string _printerIpAddress = "192.168.11.50";

        [ObservableProperty] private string _connectionStatus = "연결되지 않음";

        [ObservableProperty] private string _lastResponse = string.Empty;

        [ObservableProperty] private string logMessages = string.Empty;

        [ObservableProperty] private string printMessage = string.Empty;

        [ObservableProperty] private string imageFilePath = string.Empty;

        [ObservableProperty] private bool showResultInfo;

        [ObservableProperty] private string resultMessage = string.Empty;

        [ObservableProperty] private string resultSeverity = "Success"; // "Success", "Error", "Warning", "Info"

        [ObservableProperty] private int printYield; // 생산 카운트 필드

        [ObservableProperty] private ObservableCollection<string> recentTemplateNames = new();

        public DashboardViewModel(YacodeClientService yacodeClient)
        {
            _yacodeClient = yacodeClient;

            // 이벤트 구독
            _yacodeClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            _yacodeClient.MessageReceived += OnMessageReceived;
            _yacodeClient.ErrorOccurred += OnErrorOccurred;
            _yacodeClient.PrintYieldUpdated += OnPrintYieldUpdated;
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
            Application.Current.Dispatcher.Invoke(() => { AddLogMessage($"오류: {error}"); });
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

        private void OnPrintYieldUpdated(object? sender, int yield)
        {
            PrintYield = yield;
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

            string templateName = "100.ym"; // 실제 프린터에 업로드된 템플릿명

            // 1. StartPrinting 먼저 호출
            AddLogMessage($"🖨️ StartPrinting 명령 호출: {templateName}");
            var startResult = await _yacodeClient.StartPrintingAsync(templateName);

            if (!startResult)
            {
                SetResult("인쇄 시작 실패!", "Error");
                AddLogMessage("❌ 인쇄 시작 명령 실패");
                return;
            }

            AddLogMessage("✅ 인쇄 시작 명령 전송 완료");

            // 2. 텍스트와 이미지 준비
            var textArray = new List<Dictionary<string, object>>();
            byte[]? imageBytes = null;
            bool isRealImage = false;

            // 이미지 파일이 있는지 확인
            if (!string.IsNullOrWhiteSpace(ImageFilePath) && File.Exists(ImageFilePath))
            {
                imageBytes = File.ReadAllBytes(ImageFilePath);
                isRealImage = true;
                AddLogMessage("📦 실제 이미지 포함 전송 준비");
            }
            else
            {
                // 더미 이미지 생성 (1x1 픽셀 투명 BMP)
                imageBytes = CreateDummyImage();
                isRealImage = false;
                AddLogMessage("📦 더미 이미지 포함 전송 준비");
            }

            // 항상 DynImage1 포함 (실제 이미지 또는 더미 이미지)
            var imageMeta = new Dictionary<string, object>
            {
                { "metaname", "DynImage1" },
                { "is_image", 1 },
                { "metadata_pos", 0 },
                { "metadata_len", imageBytes.Length },
                { "hide_flag", isRealImage ? 0 : 1 }, // 더미 이미지면 숨김
                { "metaimage_dpi", 300 },
                { "metaimage_has_rolate", 0 },
                { "compress", 0 }
            };

            textArray.Add(imageMeta);

            // 텍스트 데이터는 항상 포함
            var textMeta = new Dictionary<string, object>
            {
                { "metaname", "DynText1" },
                { "is_image", 0 },
                { "metadata", PrintMessage },
                { "hide_flag", 0 }
            };

            textArray.Add(textMeta);

            // 3. Payload 구성 (프로토콜 문서 순서대로)
            // repeat_time을 -1로 하면 센서가 인식할 때 마다 연속으로 인쇄함
            var payload = new Dictionary<string, object>
            {
                { "text", textArray },
                { "repeat_times", -1 },
                { "app_mode", 1000 },
                { "direct", -1 },
                { "cover_flag", 0 },
                { "hide_flag", 0 }
            };

            AddLogMessage("📤 프린터로 데이터 전송 중...");

            // 4. 전송 (항상 이미지 데이터 포함)
            bool success = await _yacodeClient.SendRawDynamicDataAsync(payload, imageBytes);

            if (!success)
            {
                SetResult("프린터 전송 실패!", "Error");
                AddLogMessage("❌ 프린터 전송 실패");
                return;
            }

            AddLogMessage("✅ 프린터 전송 성공");
            SetResult("프린터 인쇄 시작!", "Success");
        }

        /// <summary>
        /// 1x1 픽셀 투명 BMP 이미지 생성
        /// </summary>
        private byte[] CreateDummyImage()
        {
            // 1x1 픽셀 24비트 BMP 헤더 + 투명 픽셀 데이터
            return new byte[]
            {
                // BMP 헤더 (54바이트)
                0x42, 0x4D, // "BM" 시그니처
                0x3A, 0x00, 0x00, 0x00, // 파일 크기 (58바이트)
                0x00, 0x00, // 예약
                0x00, 0x00, // 예약
                0x36, 0x00, 0x00, 0x00, // 이미지 데이터 오프셋 (54바이트)

                // DIB 헤더 (40바이트)
                0x28, 0x00, 0x00, 0x00, // DIB 헤더 크기
                0x01, 0x00, 0x00, 0x00, // 이미지 너비 (1픽셀)
                0x01, 0x00, 0x00, 0x00, // 이미지 높이 (1픽셀)
                0x01, 0x00, // 컬러 플레인 수
                0x18, 0x00, // 픽셀당 비트 수 (24비트)
                0x00, 0x00, 0x00, 0x00, // 압축 방식 (무압축)
                0x04, 0x00, 0x00, 0x00, // 이미지 데이터 크기
                0x13, 0x0B, 0x00, 0x00, // 가로 해상도
                0x13, 0x0B, 0x00, 0x00, // 세로 해상도
                0x00, 0x00, 0x00, 0x00, // 컬러 팔레트 수
                0x00, 0x00, 0x00, 0x00, // 중요한 컬러 수

                // 픽셀 데이터 (1픽셀, 24비트 BGR + 패딩)
                0xFF, 0xFF, 0xFF, 0x00 // 흰색 픽셀 + 패딩
            };
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

        [RelayCommand]
        private async Task LoadTemplateMetaInfoAsync()
        {
            AddLogMessage("📨 문서 메타정보 요청 중...");
            var result = await _yacodeClient.GetTemplateMetaInfoAsync();
            if (!result)
            {
                AddLogMessage("❌ 문서 메타정보 가져오기 실패");
            }
        }
    }
}