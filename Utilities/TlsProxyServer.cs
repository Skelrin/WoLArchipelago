using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

namespace WoLArchipelago
{
    public class TlsProxyServer
    {
        private TcpListener listener;
        private string targetHost;
        private int targetPort;
        public int LocalPort { get; private set; }

        public TlsProxyServer(string targetHost, int targetPort)
        {
            this.targetHost = targetHost;
            this.targetPort = targetPort;
        }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            LocalPort = ((IPEndPoint)listener.LocalEndpoint).Port;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (true)
                {
                    try
                    {
                        TcpClient clientSocket = listener.AcceptTcpClient();
                        ThreadPool.QueueUserWorkItem(__ => HandleClient(clientSocket));
                    }
                    catch { break; }
                }
            });
        }

        private void HandleClient(TcpClient clientSocket)
        {
            try
            {
                TcpClient serverSocket = new TcpClient(targetHost, targetPort);
                
                TlsClientProtocol tlsProtocol = new TlsClientProtocol(serverSocket.GetStream(), new SecureRandom());
                tlsProtocol.Connect(new ArchipelagoTlsClient());

                Stream clientStream = clientSocket.GetStream();
                Stream serverStream = tlsProtocol.Stream;

                ThreadPool.QueueUserWorkItem(_ => CopyStream(clientStream, serverStream));
                CopyStream(serverStream, clientStream);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Proxy TLS] Erreur : " + ex.Message);
            }
        }

        private void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
                output.Flush();
            }
        }

        public void Stop() => listener?.Stop();
    }

    public class ArchipelagoTlsClient : DefaultTlsClient
    {
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