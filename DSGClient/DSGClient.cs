using System.Net.Sockets;
using System.Text;

namespace DSGClient
{
    /**
     * DSG client class.
     * */
    public sealed class DSGClient : IAsyncDisposable
    {
        public event Action<string, string>? MessageReceived;
        public event Action<string, bool> StatusChanged;


        // Connection state
        private volatile bool _connected;
        public bool IsConnected => _connected;
        
        // Environment and gateway tag to prefix log messages
        private string GATEWAY_TAG => "[" + _gateway.EnvironmentName + ":" + _gateway.GatewayName + "] ";

        // Constants
        private const int LENGTH_OUTERHEADER = 14;  // Length of outer header
        private const int LENGTH_INNERHEADER = 16;  // Length of inner header

        // Connection properties
        private readonly Gateway _gateway;
        public string GatewayName => _gateway.GatewayName;
        private readonly List<MessageType> _messageTypes;
        private readonly int _heartbeatSeconds;

        // Connection objects
        private TcpClient _client;
        private NetworkStream _stream;

        /// <summary>
        /// Lock for sending messages.
        /// Ensures that only one message is sent at a time 
        /// so that the network stream is being written correctly and sequentially 
        /// with messages one by one.
        /// </summary>
        private SemaphoreSlim _sendLock; // = new(1, 1);

        // Cancellation tokens
        private CancellationTokenSource? _cts;              // cancel all
        private CancellationTokenSource? _connectionCts;    // cancel per-connection only

        // Task handles
        private Task _readTask;         // Handle to the read task.
        private Task _heartbeatTask;    // Handle to the heartbeat task.

        /**
         * Constructor of the DSG Client class.
         * 
         * @param gateway: The gateway to connect to.
         * @param messageTypes: The message types to subscribe to.
         * @param heartbeatSeconds: The heartbeat interval in seconds.
         * */
        public DSGClient(Gateway gateway, List<MessageType> messageTypes, int heartbeatSeconds = 2)
        {
            _gateway = gateway;
            _messageTypes = messageTypes;
            _heartbeatSeconds = heartbeatSeconds;
        }

        /**
         * Send a Download message to the DSG asynchronously.
         * 
         * @param partitionId: The partition ID to download.
         * @param startingSequenceNumber: The starting sequence number to download.
         * @param endingSequenceNumber: The ending sequence number to download.
         * */
        public async Task DownloadAsync(string partitionId, string startingSequenceNumber, string endingSequenceNumber)
        {
            await SendAsync(XmlManager.BuildMessageDownload(partitionId, startingSequenceNumber, endingSequenceNumber, _messageTypes));
            Console.WriteLine(GATEWAY_TAG + "<< Download request sent"
                + " from sequence number " + startingSequenceNumber + " to " + endingSequenceNumber
                + " for messages: " + _messageTypes.Select(x => x.MessageName).Aggregate((a, b) => a + ", " + b)
                + ".");
        }

        /**
         * Send a Heartbeat message to the DSG asynchronously.
         * */
        public async Task SendHeartbeatAsync() => await SendAsync(XmlManager.BuildMessageHeartbeat());

        /**
         * Send a Login message to the DSG asynchronously.
         * */
        public async Task LoginAsync()
        {
            try
            {
                await SendAsync(XmlManager.BuildMessageLogin(_gateway.Username, _gateway.Password, _messageTypes));
                Console.WriteLine(GATEWAY_TAG + "<< Login sent.");
                StatusChanged.Invoke(_gateway.GatewayName, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{GATEWAY_TAG}<< Failed to send login: {ex.Message}");
            }
        }

        /**
         * Send a Logout message to the DSG asynchronously.
         * */
        public async Task LogoutAsync()
        {
            try
            {
                await SendAsync(XmlManager.BuildMessageLogout(_gateway.Username));
                Console.WriteLine(GATEWAY_TAG + "<< Logout sent.");
                StatusChanged.Invoke(_gateway.GatewayName, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{GATEWAY_TAG}<< Failed to send logout: {ex.Message}");
            }
        }

        /**
         * Keep sending heartbeat messages to the DSG.
         * 
         * @param ct: CancellationToken to cancel the loop when the application is stopping.
         * */
        private async Task LoopHeartbeatAsync(CancellationToken ct)
        {
            // Exit if heartbeat interval is not set or is set to 0.
            if (_heartbeatSeconds <= 0) return;

            try
            {
                // Set up a periodic timer to send heartbeats.
                var timer = new PeriodicTimer(TimeSpan.FromSeconds(_heartbeatSeconds));

                // Loop sending heartbeats until cancelled.
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        await SendHeartbeatAsync();
                        Console.WriteLine(GATEWAY_TAG + "<< Heartbeat sent.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{GATEWAY_TAG}<< Heartbeat error: {ex.Message}");
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        /**
         * Keep reading messages from the DSG.
         * 
         * @param ct: CancellationToken to cancel the loop when the application is stopping.
         * */
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
                        Console.WriteLine(GATEWAY_TAG + ">> Connection closed by server.");
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
                            Console.WriteLine($"{GATEWAY_TAG}>> Invalid payload length: {payloadLength}. Dropping buffer.");
                            accumulator.Clear();
                            break;
                        }
                        if (accumulator.Count < fullMessageLength) break;   // Wait for full payload

                        // Extract payload, starting at the end of outer header to the payload length
                        byte[] payload = accumulator.GetRange(LENGTH_OUTERHEADER, payloadLength).ToArray();
                        accumulator.RemoveRange(0, fullMessageLength);      // Remove the full message from accumulator

                        Console.WriteLine($"{GATEWAY_TAG}>> Message Id: {messageId}, Sequence Number: {sequenceNumber}");

                        MessageReceived?.Invoke(_gateway.GatewayName, messageId.ToString());


                        if (payload.Length == 0)
                        {
                            Console.WriteLine(GATEWAY_TAG + ">> Empty payload, skipping...");
                            continue;
                        }

                        // Find XML start
                        int xmlStart = Array.IndexOf(payload, (byte)'<');
                        if (xmlStart < 0)
                        {
                            Console.WriteLine(GATEWAY_TAG + ">> No XML found in payload, skipping...");
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
                            Console.WriteLine($"{GATEWAY_TAG}>> Failed to decode XML: {ex.Message}");
                            continue;
                        }
                        // Show the XML string
                        Console.WriteLine(GATEWAY_TAG + ">> XML Received:" + Environment.NewLine + xml + Environment.NewLine);

                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(GATEWAY_TAG + ">> Read loop canceled.");
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine($"{GATEWAY_TAG}>> Object disposed. {ode.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{GATEWAY_TAG}>> Error in read loop: {ex.Message}");
            }
            finally
            {
                _connected = false;
            }
        }

        /**
         * Send a message to the DSG asynchronously.
         * 
         * @param payload: The message to send.
         * */
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

        /**
         * Start the DSG client asynchronously and run until appStop is cancelled.
         * 
         * @param appStop: CancellationToken to cancel the DSG client when the application is stopping.
         * */
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
            _client = new TcpClient();
            Console.WriteLine($"Connecting to {_gateway.GatewayName} of environment {_gateway.EnvironmentName} at {_gateway.Host}:{_gateway.Port}...");
            await _client.ConnectAsync(_gateway.Host, _gateway.Port);
            _stream = _client.GetStream();
            _connected = true;
            Console.WriteLine(GATEWAY_TAG + "<< Connected.");

            // Start the read loop on a background thread, let it run until _cts is cancelled, and keep a handle to that task in _readTask.
            _readTask = Task.Run(() => LoopReadAsync(_cts.Token), _cts.Token);
            
            // Start the heartbeat loop on a background thread, let it run until _cts is cancelled, and keep a handle to that task in _heartbeatTask.
            _heartbeatTask = Task.Run(() => LoopHeartbeatAsync(_cts.Token), _cts.Token);

            // Send the login message.
            await LoginAsync();
        }

        /**
         * Stop the DSG client asynchronously.
         * */
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


        /**
         * Dispose the DSG client asynchronously.
         * */
        public async ValueTask DisposeAsync() => await StopAsync();

        /**
         * Dispose all the open resources.
         * */
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
