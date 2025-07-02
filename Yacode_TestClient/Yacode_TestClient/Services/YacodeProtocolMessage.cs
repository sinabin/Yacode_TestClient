using System.Text;
using System.Text.Json;

namespace Yacode_TestClient.Services
{
    /// <summary>
    /// Yacode 프로토콜 메시지 클래스
    /// </summary>
    public class YacodeProtocolMessage
    {
        // 헤더 상수
        public static readonly byte[] HEADER = { 0xEB, 0x01 };
        
        // 프로토콜 마크 정의
        public static class ProtocolMarks
        {
            public static readonly byte[] GET_SYSTEM_STATUS = { 0x00, 0x01 };
            public static readonly byte[] GET_PRINTING_STATUS = { 0x00, 0x02 };
            public static readonly byte[] GET_PRINTING_LOG = { 0x00, 0x03 };
            public static readonly byte[] SET_DYNAMIC_DATA = { 0x00, 0x04 };
            public static readonly byte[] START_PRINTING = { 0x00, 0x05 };
            public static readonly byte[] STOP_PRINTING = { 0x00, 0x06 };
            public static readonly byte[] GET_PRINTING_CACHE = { 0x00, 0x07 };
            public static readonly byte[] SPECIAL_PARAMETER_SETTING = { 0x00, 0x08 };
            public static readonly byte[] REGISTER_LOG_CALLBACK = { 0x00, 0x09 };
            public static readonly byte[] LOGOUT_LOG_CALLBACK = { 0x00, 0x0A };
            public static readonly byte[] GET_CACHE_QUANTITY = { 0x00, 0x12 };
            public static readonly byte[] CLEAN_UP_CACHE = { 0x00, 0x14 };
            public static readonly byte[] SUSPENSION_OF_PRINTING = { 0x00, 0x15 };
            public static readonly byte[] CONTINUE_TO_PRINT = { 0x00, 0x16 };
            public static readonly byte[] TEST_INFORMATION = { 0x00, 0x63 };
        }

        public byte[] Header { get; set; }
        public byte[] ProtocolMark { get; set; }
        public int DataLength { get; set; }
        public string Data { get; set; }
        
        public byte[]? RawData { get; set; } 

        public YacodeProtocolMessage()
        {
            Header = HEADER;
            Data = string.Empty;
        }

        /// <summary>
        /// 메시지를 바이트 배열로 변환
        /// </summary>
        public byte[] ToByteArray()
        {
            byte[] dataBytes;
            
            // 시스템 상태 요청 등 데이터가 없는 경우
            if (string.IsNullOrEmpty(Data))
            {
                dataBytes = new byte[0];
            }
            else
            {
                // JSON 데이터 + null terminator
                dataBytes = Encoding.UTF8.GetBytes(Data + "\0");
            }
            
            // 데이터 길이를 네트워크 바이트 순서 (빅 엔디안)으로 변환
            var dataLength = BitConverter.GetBytes(dataBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(dataLength);
            }

            var result = new List<byte>();
            result.AddRange(Header);
            result.AddRange(ProtocolMark);
            result.AddRange(dataLength);
            
            if (dataBytes.Length > 0)
            {
                result.AddRange(dataBytes);
            }

            return result.ToArray();
        }

        /// <summary>
        /// 바이트 배열에서 메시지 파싱
        /// </summary>
        public static YacodeProtocolMessage? FromByteArray(byte[] data)
        {
            if (data.Length < 8) return null;

            var message = new YacodeProtocolMessage();
            
            // 헤더 확인
            if (data[0] != HEADER[0] || data[1] != HEADER[1])
                return null;

            message.Header = new byte[] { data[0], data[1] };
            message.ProtocolMark = new byte[] { data[2], data[3] };

            // 데이터 길이 (빅 엔디안)
            var lengthBytes = new byte[] { data[4], data[5], data[6], data[7] };
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }
            message.DataLength = BitConverter.ToInt32(lengthBytes, 0);

            // 데이터
            if (data.Length >= 8 + message.DataLength)
            {
                var dataBytes = new byte[message.DataLength];
                Array.Copy(data, 8, dataBytes, 0, message.DataLength);
                
                // null 문자 제거
                var nullIndex = Array.IndexOf(dataBytes, (byte)0);
                if (nullIndex >= 0)
                {
                    dataBytes = dataBytes.Take(nullIndex).ToArray();
                }
                
                message.Data = Encoding.UTF8.GetString(dataBytes);
            }

            return message;
        }

        /// <summary>
        /// 시스템 상태 요청 메시지 생성
        /// </summary>
        public static YacodeProtocolMessage CreateSystemStatusRequest()
        {
            return new YacodeProtocolMessage
            {
                ProtocolMark = ProtocolMarks.GET_SYSTEM_STATUS,
                Data = string.Empty
            };
        }

        /// <summary>
        /// 인쇄 상태 요청 메시지 생성
        /// </summary>
        public static YacodeProtocolMessage CreatePrintingStatusRequest(int groupId = 0)
        {
            var data = new { group_id = groupId };
            return new YacodeProtocolMessage
            {
                ProtocolMark = ProtocolMarks.GET_PRINTING_STATUS,
                Data = JsonSerializer.Serialize(data)
            };
        }

        /// <summary>
        /// 테스트 정보 요청 메시지 생성
        /// </summary>
        public static YacodeProtocolMessage CreateTestInformationRequest()
        {
            return new YacodeProtocolMessage
            {
                ProtocolMark = ProtocolMarks.TEST_INFORMATION,
                Data = string.Empty
            };
        }

        /// <summary>
        /// 인쇄 로그 요청 메시지 생성
        /// </summary>
        public static YacodeProtocolMessage CreatePrintingLogRequest(int groupId = 0)
        {
            var data = new { group_id = groupId };
            return new YacodeProtocolMessage
            {
                ProtocolMark = ProtocolMarks.GET_PRINTING_LOG,
                Data = JsonSerializer.Serialize(data)
            };
        }

        /// <summary>
        /// 캐시 정보 요청 메시지 생성
        /// </summary>
        public static YacodeProtocolMessage CreatePrintingCacheRequest(int groupId = 0)
        {
            var data = new { group_id = groupId };
            return new YacodeProtocolMessage
            {
                ProtocolMark = ProtocolMarks.GET_PRINTING_CACHE,
                Data = JsonSerializer.Serialize(data)
            };
        }
    }
}