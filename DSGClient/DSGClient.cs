using System.Net.Sockets;
using System.Text;

namespace DSGClient
{
    public sealed class DSGClient : IAsyncDisposable
    {
        private volatile bool _connected;
        private readonly Gateway _gateway;
        private readonly List<MessageType> _messageTypes;
        private int _heartbeatSeconds;
        private string _startingSequenceNumber;
        private string _endingSequenceNumber;
        private TcpClient _client;
        private NetworkStream _stream;

        /// <summary>
        /// Lock for sending messages.
        /// Ensures that only one message is sent at a time 
        /// so that the network stream is being written correctly and sequentially 
        /// with messages one by one.
        /// </summary>
        private SemaphoreSlim _sendLock; // = new(1, 1);

        public event Action<string, string>? MessageReceived;
        public event Action<string, bool> StatusChanged;

        public bool IsConnected => _connected;
        public string GatewayName => _gateway.GatewayName;
        private string GATEWAY_TAG => "[" + _gateway.EnvironmentName + ":" + _gateway.GatewayName + "] ";

        private const int LENGTH_OUTERHEADER = 14;  // Length of outer header
        private const int LENGTH_INNERHEADER = 16;  // Length of inner header

        private CancellationTokenSource? _cts;              // cancel all
        private CancellationTokenSource? _connectionCts;    // cancel per-connection only

        private Task _readTask;         // Handle to the read task.
        private Task _heartbeatTask;    // Handle to the heartbeat task.

        public DSGClient(Gateway gateway, List<MessageType> messageTypes, string startingSequenceNumber, string endingSequenceNumber, int heartbeatSeconds = 2)
        {
            _gateway = gateway;
            _messageTypes = messageTypes;
            _startingSequenceNumber = startingSequenceNumber;
            _endingSequenceNumber = endingSequenceNumber;
            _heartbeatSeconds = heartbeatSeconds;
        }

        public async Task DownloadAsync()
        {
            try
            {
                await SendAsync(XmlManager.BuildMessageDownload(_gateway.PartitionId, _startingSequenceNumber, _endingSequenceNumber, _messageTypes));
                LogHelper.Info(GATEWAY_TAG + "<< Download request sent"
                    + " from sequence number " + _startingSequenceNumber + " to " + _endingSequenceNumber
                    + " for messages: " + _messageTypes.Select(x => x.Name).Aggregate((a, b) => a + ", " + b)
                    + ".");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"{GATEWAY_TAG}<< Failed to download: {ex.Message}");
            }
        }

        public async Task SendHeartbeatAsync() => await SendAsync(XmlManager.BuildMessageHeartbeat());

        public async Task LoginAsync()
        {
            try
            {
                await SendAsync(XmlManager.BuildMessageLogin(_gateway.Username, _gateway.Password, _messageTypes));
                LogHelper.Info(GATEWAY_TAG + "<< Login sent.");
                StatusChanged.Invoke(_gateway.GatewayName, true);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"{GATEWAY_TAG}<< Failed to send login: {ex.Message}");
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await SendAsync(XmlManager.BuildMessageLogout(_gateway.Username));
                LogHelper.Info(GATEWAY_TAG + "<< Logout sent.");
                StatusChanged.Invoke(_gateway.GatewayName, false);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"{GATEWAY_TAG}<< Failed to send logout: {ex.Message}");
            }
        }

        private async Task LoopHeartbeatAsync(CancellationToken ct)
        {
            if (_heartbeatSeconds <= 0) return;
            try
            {
                var timer = new PeriodicTimer(TimeSpan.FromSeconds(_heartbeatSeconds)); // Set up a periodic timer to send heartbeats.

                while (await timer.WaitForNextTickAsync(ct)) // Loop sending heartbeats until cancelled.
                {
                    try
                    {
                        await SendHeartbeatAsync();
                        LogHelper.Info(GATEWAY_TAG + "<< Heartbeat sent.");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"{GATEWAY_TAG}<< Heartbeat error: {ex.Message}");
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task LoopReadAsync(CancellationToken ct)
        {
            List<byte> accumulator = new(); // Initialize an accumulator for receiving message bytes
            byte[] buffer = new byte[8192];	// Read 8kb at a time

            try
            {
                while (!ct.IsCancellationRequested) // Loop reading until cancelled
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, ct); // Read 8kb at a time
                    if (bytesRead == 0)
                    {
                        LogHelper.Warn(GATEWAY_TAG + ">> Connection closed by server.");
                        _connected = false;
                        break;
                    }

                    accumulator.AddRange(buffer.Take(bytesRead));   // Add bytes to accumulator

                    while (true) // Loop as long as we have a full message
                    {
                        if (accumulator.Count < LENGTH_OUTERHEADER) break;  // Break if not at least outer header

                        // Outer header: 14 bytes
                        // -------------------------------------------------------------------------------
                        // |    4 bytes    |    4 bytes    |    4 bytes    |    1 byte    |    1 byte    |
                        // -------------------------------------------------------------------------------
                        // |   Sequence #  | Payload length|   Message ID  | Partition ID |  Msg format  |
                        // -------------------------------------------------------------------------------
                        int sequenceNumber = BitConverter.ToInt32(accumulator.GetRange(0, 4).ToArray(), 0);
                        int payloadLength = BitConverter.ToInt32(accumulator.GetRange(4, 4).ToArray(), 0);
                        int messageId = BitConverter.ToInt32(accumulator.GetRange(8, 4).ToArray(), 0);
                        byte partitionId = accumulator[12];
                        byte messageFormat = accumulator[13];

                        int fullMessageLength = LENGTH_OUTERHEADER + payloadLength; // Calculate full message length
                        if (payloadLength < 0 || fullMessageLength > 4_000_000)
                        {
                            LogHelper.Warn($"{GATEWAY_TAG}>> Invalid payload length: {payloadLength}. Dropping buffer.");
                            accumulator.Clear();
                            break;
                        }
                        if (accumulator.Count < fullMessageLength) break;   // Wait for full payload

                        // Extract payload, starting at the end of outer header to the payload length
                        byte[] payload = accumulator.GetRange(LENGTH_OUTERHEADER, payloadLength).ToArray();
                        accumulator.RemoveRange(0, fullMessageLength);      // Remove the full message from accumulator

                        LogHelper.Info($"{GATEWAY_TAG}>> Message Id: {messageId}, Sequence Number: {sequenceNumber}");
                        MessageReceived?.Invoke(_gateway.GatewayName, messageId.ToString());

                        if (payload.Length == 0)
                        {
                            LogHelper.Warn(GATEWAY_TAG + ">> Empty payload, skipping...");
                            continue;
                        }

                        // Find XML start
                        int xmlStart = Array.IndexOf(payload, (byte)'<');
                        if (xmlStart < 0)
                        {
                            LogHelper.Warn(GATEWAY_TAG + ">> No XML found in payload, skipping...");
                            continue;
                        }

                        // Show inner header 
                        //if (xmlStart > 0)
                        //{
                        //    byte[] innerHeader = payload.Take(LENGTH_INNERHEADER).ToArray();
                        //    //Console.WriteLine("Inner header (hex): " + BitConverter.ToString(innerHeader));
                        //}

                        // Decode the XML to string
                        string xml;
                        try
                        {
                            xml = Encoding.UTF8.GetString(payload, xmlStart, payload.Length - xmlStart);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error($"{GATEWAY_TAG}>> Failed to decode XML: {ex.Message}");
                            continue;
                        }
                        // Show the XML string
                        LogHelper.Info(GATEWAY_TAG + ">> XML Received:" + Environment.NewLine + xml + Environment.NewLine);
                        //LogHelper.Xml(GATEWAY_TAG + ">> XML Received:" + Environment.NewLine, XmlManager.GetXmlFromString(xml));
                        // Save Message to file named <MessageId>.txt
                        if (messageId != 0 && !string.IsNullOrWhiteSpace(xml))
                        {
                            using (StreamWriter sw = new StreamWriter($"{messageId}.txt", true))
                            {
                                sw.WriteLine($"{xml}");
                            }
                        }
                            
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.Warn(GATEWAY_TAG + ">> Read loop canceled.");
            }
            catch (ObjectDisposedException ode)
            {
                LogHelper.Warn($"{GATEWAY_TAG}>> Object disposed. {ode.Message}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"{GATEWAY_TAG}>> Error in read loop: {ex.Message}");
            }
            finally
            {
                _connected = false;
            }
        }

        private async Task SendAsync(byte[] payload)
        {
            if (_stream is null || !_connected)
                throw new InvalidOperationException(GATEWAY_TAG + "<< Not connected.");

            // Wait for a free send slot.
            await _sendLock.WaitAsync();

            // Send the message
            try
            {
                await _stream.WriteAsync(payload, 0, payload.Length, _cts!.Token);
                await _stream.FlushAsync(_cts!.Token);
            }
            finally
            {
                // Release the send slot
                _sendLock.Release();
            }
        }

        public async Task StartAsync(CancellationToken appStop)
        {
            // Clean up any old state (in case StopAsync wasn’t called)
            Cleanup();

            // Create per-connection CTS and link with appStop
            _connectionCts = new CancellationTokenSource();
            _cts = (appStop.CanBeCanceled && !appStop.IsCancellationRequested) 
                    ? CancellationTokenSource.CreateLinkedTokenSource(appStop, _connectionCts.Token) // not already cancelled, so link to appStop
                    : _connectionCts; // already cancelled, so use per-connection CTS

            // Create a semaphore to ensure that only one message is sent at a time
            _sendLock = new(1, 1);

            // Create a TCP client and connect to the DSG.
            await ConnectAsync();

            // Start the read and heartbeat loops in background, let those run until _cts is cancelled, and keep a handle to each task.
            _readTask = Task.Run(() => LoopReadAsync(_cts.Token), _cts.Token);
            _heartbeatTask = Task.Run(() => LoopHeartbeatAsync(_cts.Token), _cts.Token);

            // Send the login message.
            await LoginAsync();
        }

        private async Task ConnectAsync()
        {
            // Create a TCP client and connect to the DSG.
            _client = new TcpClient();
            LogHelper.Info($"Connecting to {_gateway.GatewayName} of environment {_gateway.EnvironmentName} at {_gateway.Host}:{_gateway.Port}...");
            await _client.ConnectAsync(_gateway.Host, _gateway.Port);
            _stream = _client.GetStream();
            _connected = true;
            LogHelper.Info(GATEWAY_TAG + "<< Connected.");
        }

        public async Task StopAsync()
        {
            try
            {
                // Cancel first so that read/heartbeat loops break immediately
                _connectionCts?.Cancel();

                // Close connection instantly
                try { _client?.Close(); } catch { }
                try { _stream?.Close(); } catch { }

                // Try sending logout without blocking Stop
                if (_connected && _cts is { IsCancellationRequested: false })
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await LogoutAsync();
                            await Task.Delay(50); // tiny delay to flush
                        }
                        catch { }
                    });
                }

                // Don't wait indefinitely for tasks; allow max 200 ms
                if (_readTask != null || _heartbeatTask != null)
                {
                    var tasks = new List<Task>();
                    if (_readTask != null) tasks.Add(_readTask);
                    if (_heartbeatTask != null) tasks.Add(_heartbeatTask);
                    await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(200));
                }
            }
            finally
            {
                _connected = false;   // <--- Make sure this happens before notifying UI
                StatusChanged?.Invoke(_gateway.GatewayName, false); // <--- Notify UI immediately
                Cleanup();
            }
        }

        public async ValueTask DisposeAsync() => await StopAsync();

        private void Cleanup()
        {
            try
            {
                _client?.Close();
                _stream?.Close();
            }
            //catch { /* ignore */ }
            finally
            {
                _stream?.Dispose();
                _client?.Dispose();
                _sendLock?.Dispose();
                _readTask?.Dispose();
                _heartbeatTask?.Dispose();
                _cts?.Dispose();
                _connectionCts?.Dispose();

                _client = null;
                _stream = null;
                _sendLock = null;
                _readTask = null;
                _heartbeatTask = null;
                _cts = null;
                _connectionCts = null;
                _connected = false;
            }
        }
    }
}
