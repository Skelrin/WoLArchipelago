using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

namespace WoLArchipelago
{
    /// <summary>
    /// Local TCP-to-TLS proxy using BOuncyCastle allowing WebSocket connections to secure Archipelago servers,
    /// such as archipelago.gg only accepting WebSocket connections over TLS1.2 or TLS 1.3.
    /// </summary>
    public class TlsProxyServer
    {
        private readonly string _targetHost;
        private readonly int _targetPort;

        private TcpListener _listener;
        private bool _isRunning;

        public int LocalPort { get; private set; }

        public TlsProxyServer(string targetHost, int targetPort)
        {
            _targetHost = targetHost;
            _targetPort = targetPort;
        }

        /// <summary>
        /// Starts listening locally on an available port and proxies traffic to the TLS target host.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            LocalPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _isRunning = true;

            ThreadPool.QueueUserWorkItem(delegate
            {
                while (_isRunning)
                {
                    try
                    {
                        TcpClient clientSocket = _listener.AcceptTcpClient();
                        ThreadPool.QueueUserWorkItem(delegate { HandleClient(clientSocket); });
                    }
                    catch
                    {
                        // Interrupted when listener is stopped
                        break;
                    }
                }
            });
        }

        /// <summary>
        /// Stops the local TCP listener.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                }
                catch
                {
                    // Ignore listener stop exceptions
                }
                _listener = null;
            }
        }

        private void HandleClient(TcpClient clientSocket)
        {
            try
            {
                using TcpClient serverSocket = new TcpClient(_targetHost, _targetPort);
                TlsClientProtocol tlsProtocol = new TlsClientProtocol(serverSocket.GetStream(), new SecureRandom());
                tlsProtocol.Connect(new ArchipelagoTlsClient());

                Stream clientStream = clientSocket.GetStream();
                Stream serverStream = tlsProtocol.Stream;

                // Bidirectional stream copying
                ThreadPool.QueueUserWorkItem(delegate { CopyStream(clientStream, serverStream); });
                CopyStream(serverStream, clientStream);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[TLS Proxy] Client connection error: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    clientSocket.Close();
                }
                catch
                {
                    // Socket cleanup fallback
                }
            }
        }

        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            try
            {
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                    output.Flush();
                }
            }
            catch
            {
                // Stream closed or connection broken
            }
        }
    }

    /// <summary>
    /// Custom TLS client implementation for Archipelago WebSocket connections.
    /// Overrides default authentication to bypass strict SSL/TLS certificate validation.
    /// </summary>
    public class ArchipelagoTlsClient : DefaultTlsClient
    {
        /// <summary>
        /// Provides a custom TLS authentication handler that bypasses certificate verification.
        /// Archipelago servers and proxy endpoints use self-signed
        /// or custom certificates that fail validation under legacy .NET 3.5 trust stores.
        /// </summary>
        public override TlsAuthentication GetAuthentication()
        {
            return new AlwaysValidTlsAuthentication();
        }
    }

    public class AlwaysValidTlsAuthentication : TlsAuthentication
    {
        public void NotifyServerCertificate(Certificate serverCertificate)
        {
            
        }

        public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
        {
            return null;
        }
    }
}