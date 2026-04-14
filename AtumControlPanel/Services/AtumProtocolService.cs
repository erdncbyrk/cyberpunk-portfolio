using System.Net.Sockets;
using System.Text;

namespace AtumControlPanel.Services
{
    /// <summary>
    /// Implements the ATUM IOCP socket protocol for communicating with PreServer.
    ///
    /// Packet format:
    /// [Size(2B LE)] [EncodeFlag(1B)] [SeqNum(1B)] [MsgType(2B LE)] [Body...]
    ///
    /// Size = length of data AFTER the 4-byte header (MsgType + Body)
    /// EncodeFlag = 0x00 for non-encoded
    /// SeqNum = sequence number (computed with formula)
    /// </summary>
    public class AtumProtocolService
    {
        // Protocol constants from AtumProtocol.h
        private const ushort T_PA_ADMIN_CONNECT = 0xB000;
        private const ushort T_PA_ADMIN_CONNECT_OK = 0xB001;
        private const ushort T_PA_ADMIN_RELOAD_ADMIN_NOTICE_SYSTEM = 0xB016;
        private const ushort T_PA_ADMIN_UPDATE_STRATEGYPOINT_NOTSUMMONTIME = 0xB012;
        private const ushort T_PA_ADMIN_STRATRGYPOINT_INFO_CHANGE = 0xB01D;
        private const ushort T_PA_ADMIN_RELOAD_DECLARATIONOFWAR = 0xB01E;

        // Size constants
        private const int SIZE_MAX_ACCOUNT_NAME = 20;
        private const int SIZE_MAX_PASSWORD = 20;

        // Header sizes from SocketHeader.h
        private const int SIZE_PACKET_HEADER = 4;   // 2(size) + 1(encode) + 1(seq)
        private const int SIZE_FIELD_TYPE_HEADER = 2; // MessageType_t

        // Sequence number constants from SocketHeader.h
        private const int SEQNO_VAR_A = 1;
        private const int SEQNO_VAR_B = 2;
        private const int SEQNO_VAR_C = 119;

        private readonly string _serverIP;
        private readonly int _serverPort;
        private byte _seqNumber = 0;

        public string DebugLog { get; private set; } = "";

        public AtumProtocolService(string serverIP, int serverPort)
        {
            _serverIP = serverIP;
            _serverPort = serverPort;
        }

        public async Task<(bool ok, string info)> SendNoticeReloadAsync(string adminUID, string adminPWD)
        {
            var log = new StringBuilder();
            TcpClient? client = null;

            try
            {
                client = new TcpClient { NoDelay = true };

                // Step 0: TCP Connect
                log.AppendLine($"Connecting to {_serverIP}:{_serverPort}...");
                using var connectCts = new CancellationTokenSource(5000);
                try
                {
                    await client.ConnectAsync(_serverIP, _serverPort, connectCts.Token);
                }
                catch (OperationCanceledException)
                {
                    log.AppendLine("TIMEOUT on TCP connect");
                    DebugLog = log.ToString();
                    return (false, $"Connection timeout to {_serverIP}:{_serverPort}");
                }
                log.AppendLine("TCP connected OK");

                var stream = client.GetStream();

                // Step 1: Send T_PA_ADMIN_CONNECT
                // MSG_PA_ADMIN_CONNECT: UID(20B) + PWD(20B) + Padding(8B) = 48 bytes
                var body = new byte[48];
                WriteCString(body, 0, adminUID, SIZE_MAX_ACCOUNT_NAME);
                WriteCString(body, SIZE_MAX_ACCOUNT_NAME, adminPWD, SIZE_MAX_PASSWORD);

                var authPacket = BuildPacket(T_PA_ADMIN_CONNECT, body);
                log.AppendLine($"Sending AUTH ({authPacket.Length}B): {BytesToHex(authPacket, 20)}...");

                await stream.WriteAsync(authPacket, 0, authPacket.Length);
                await stream.FlushAsync();
                log.AppendLine("AUTH sent OK");

                // Step 2: Read response with timeout
                log.AppendLine("Waiting for auth response...");
                var (respBytes, respLen) = await ReadRawWithTimeoutAsync(stream, 3000);

                if (respLen > 0)
                {
                    log.AppendLine($"Received {respLen}B: {BytesToHex(respBytes!, Math.Min(respLen, 32))}");

                    // Parse response header
                    if (respLen >= SIZE_PACKET_HEADER)
                    {
                        ushort respSize = BitConverter.ToUInt16(respBytes!, 0);
                        byte respEncode = respBytes![2];
                        byte respSeq = respBytes![3];
                        log.AppendLine($"Resp: size={respSize}, encode=0x{respEncode:X2}, seq=0x{respSeq:X2}");

                        if ((respEncode & 0x80) != 0)
                        {
                            log.AppendLine("Response is ENCODED - auth likely accepted");
                        }
                        else if (respLen >= SIZE_PACKET_HEADER + 2)
                        {
                            ushort respMsgType = BitConverter.ToUInt16(respBytes!, SIZE_PACKET_HEADER);
                            log.AppendLine($"Resp msg type: 0x{respMsgType:X4}");
                        }
                    }
                }
                else
                {
                    log.AppendLine("No response (timeout)");
                    DebugLog = log.ToString();
                    return (false, $"No auth response from PreServer.\n\nDebug:\n{log}");
                }

                // Step 3: Send T_PA_ADMIN_RELOAD_ADMIN_NOTICE_SYSTEM (no body)
                var reloadPacket = BuildPacket(T_PA_ADMIN_RELOAD_ADMIN_NOTICE_SYSTEM, null);
                log.AppendLine($"Sending RELOAD ({reloadPacket.Length}B): {BytesToHex(reloadPacket, reloadPacket.Length)}");

                await stream.WriteAsync(reloadPacket, 0, reloadPacket.Length);
                await stream.FlushAsync();
                log.AppendLine("RELOAD sent OK");

                await Task.Delay(500);

                DebugLog = log.ToString();
                return (true, "Notice reload command sent to GameServer successfully!");
            }
            catch (Exception ex)
            {
                log.AppendLine($"EXCEPTION: {ex.Message}");
                DebugLog = log.ToString();
                return (false, $"Error: {ex.Message}\n\nDebug:\n{log}");
            }
            finally
            {
                try { client?.Close(); } catch { }
                client?.Dispose();
            }
        }

        /// <summary>
        /// Connect to PreServer, authenticate, and send StrategyPoint reload commands.
        /// When dataReloadOnly=false (default): sends both T_PA_ADMIN_UPDATE_STRATEGYPOINT_NOTSUMMONTIME
        ///   + T_PA_ADMIN_STRATRGYPOINT_INFO_CHANGE (for schedule changes - recalculates summon times).
        /// When dataReloadOnly=true: sends ONLY T_PA_ADMIN_STRATRGYPOINT_INFO_CHANGE
        ///   (for instant summon - loads DB values directly into memory without recalculating).
        /// </summary>
        public async Task<(bool ok, string info)> SendStrategyPointReloadAsync(string adminUID, string adminPWD, string dbName, bool dataReloadOnly = false)
        {
            TcpClient? client = null;
            try
            {
                client = new TcpClient { NoDelay = true };
                using var cts = new CancellationTokenSource(8000);

                await client.ConnectAsync(_serverIP, _serverPort, cts.Token);
                var stream = client.GetStream();

                // Auth
                var body = new byte[48];
                WriteCString(body, 0, adminUID, SIZE_MAX_ACCOUNT_NAME);
                WriteCString(body, SIZE_MAX_ACCOUNT_NAME, adminPWD, SIZE_MAX_PASSWORD);
                await SendAndFlush(stream, BuildPacket(T_PA_ADMIN_CONNECT, body));

                // Wait for auth response
                await ReadRawWithTimeoutAsync(stream, 2000);

                if (!dataReloadOnly)
                {
                    // Send T_PA_ADMIN_UPDATE_STRATEGYPOINT_NOTSUMMONTIME (triggers SetAllStrategyPointSummonTimeNew)
                    await SendAndFlush(stream, BuildPacket(T_PA_ADMIN_UPDATE_STRATEGYPOINT_NOTSUMMONTIME, null));
                }

                // Send T_PA_ADMIN_STRATRGYPOINT_INFO_CHANGE with DBName(20B)
                // This reloads schedule + map info + summon info from DB into server memory
                var dbBody = new byte[20];
                WriteCString(dbBody, 0, dbName, 20);
                await SendAndFlush(stream, BuildPacket(T_PA_ADMIN_STRATRGYPOINT_INFO_CHANGE, dbBody));

                await Task.Delay(300);
                return (true, "StrategyPoint reload sent!");
            }
            catch (OperationCanceledException) { return (false, "Timeout connecting to PreServer"); }
            catch (Exception ex) { return (false, ex.Message); }
            finally
            {
                try { client?.Close(); } catch { }
                client?.Dispose();
            }
        }

        /// <summary>
        /// Connect to PreServer, authenticate, and send Declaration of War reload command.
        /// This tells all FieldServers to reload war data from DB without restart.
        /// </summary>
        public async Task<(bool ok, string info)> SendDeclarationOfWarReloadAsync(string adminUID, string adminPWD)
        {
            TcpClient? client = null;
            try
            {
                client = new TcpClient { NoDelay = true };
                using var cts = new CancellationTokenSource(8000);

                await client.ConnectAsync(_serverIP, _serverPort, cts.Token);
                var stream = client.GetStream();

                // Auth
                var body = new byte[48];
                WriteCString(body, 0, adminUID, SIZE_MAX_ACCOUNT_NAME);
                WriteCString(body, SIZE_MAX_ACCOUNT_NAME, adminPWD, SIZE_MAX_PASSWORD);
                await SendAndFlush(stream, BuildPacket(T_PA_ADMIN_CONNECT, body));

                // Wait for auth response
                await ReadRawWithTimeoutAsync(stream, 2000);

                // Send T_PA_ADMIN_RELOAD_DECLARATIONOFWAR (no body)
                await SendAndFlush(stream, BuildPacket(T_PA_ADMIN_RELOAD_DECLARATIONOFWAR, null));

                await Task.Delay(300);
                return (true, "Declaration of War reload command sent to all FieldServers!");
            }
            catch (OperationCanceledException) { return (false, "Timeout connecting to PreServer"); }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
            finally
            {
                try { client?.Close(); } catch { }
                client?.Dispose();
            }
        }

        private static async Task SendAndFlush(NetworkStream stream, byte[] packet)
        {
            await stream.WriteAsync(packet, 0, packet.Length);
            await stream.FlushAsync();
        }

        /// <summary>
        /// Build IOCP-format ATUM packet:
        /// [Size(2B LE)] [EncodeFlag(1B)=0x00] [SeqNum(1B)] [MsgType(2B LE)] [Body]
        /// Size = sizeof(MsgType) + sizeof(Body)
        /// </summary>
        private byte[] BuildPacket(ushort msgType, byte[]? body)
        {
            int bodyLen = body?.Length ?? 0;
            int dataLen = SIZE_FIELD_TYPE_HEADER + bodyLen; // MsgType(2) + Body
            var packet = new byte[SIZE_PACKET_HEADER + dataLen]; // Header(4) + Data

            // Header
            BitConverter.GetBytes((ushort)dataLen).CopyTo(packet, 0); // Size
            packet[2] = 0x00; // EncodeFlag = not encoded
            packet[3] = NextSeqNumber(); // Sequence number

            // Message type (little-endian)
            BitConverter.GetBytes(msgType).CopyTo(packet, SIZE_PACKET_HEADER);

            // Body
            if (body != null && body.Length > 0)
                Array.Copy(body, 0, packet, SIZE_PACKET_HEADER + SIZE_FIELD_TYPE_HEADER, body.Length);

            return packet;
        }

        /// <summary>
        /// Compute next sequence number using the ATUM formula:
        /// tmpSeq = (seq + SEQNO_VAR_A) * SEQNO_VAR_B
        /// if (tmpSeq > SEQNO_VAR_C) tmpSeq = tmpSeq % SEQNO_VAR_C
        /// seq = ++tmpSeq
        /// </summary>
        private byte NextSeqNumber()
        {
            int tmpSeq = (_seqNumber + SEQNO_VAR_A) * SEQNO_VAR_B;
            if (tmpSeq > SEQNO_VAR_C)
                tmpSeq = tmpSeq % SEQNO_VAR_C;
            _seqNumber = (byte)(++tmpSeq);
            return _seqNumber;
        }

        private static void WriteCString(byte[] buf, int offset, string str, int maxLen)
        {
            var bytes = Encoding.ASCII.GetBytes(str);
            int copyLen = Math.Min(bytes.Length, maxLen - 1);
            Array.Copy(bytes, 0, buf, offset, copyLen);
        }

        private static async Task<(byte[]? data, int length)> ReadRawWithTimeoutAsync(NetworkStream stream, int timeoutMs)
        {
            try
            {
                var buf = new byte[1024];
                using var cts = new CancellationTokenSource(timeoutMs);
                int read = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                return (buf, read);
            }
            catch (OperationCanceledException) { return (null, 0); }
            catch { return (null, 0); }
        }

        private static string BytesToHex(byte[] data, int maxLen)
        {
            int len = Math.Min(data.Length, maxLen);
            var sb = new StringBuilder(len * 3);
            for (int i = 0; i < len; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(data[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
