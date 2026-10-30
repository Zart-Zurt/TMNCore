using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public static class WebGlLocalServer
    {
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static string _servedFolder = "";
        private static int _servedPort = 8080;
        private static bool _isRunning;

        public static bool IsRunning => _isRunning;
        public static int CurrentPort => _servedPort;
        public static string CurrentFolder => _servedFolder;

        public static void StartServer(string folderPath, int port = 8080)
        {
            StopServer();

            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"[AutoBuilder Local Server] Target folder does not exist: {folderPath}");
                return;
            }

            _servedFolder = folderPath;
            _servedPort = port;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_servedPort}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{_servedPort}/");
                _listener.Start();
                _isRunning = true;

                _serverThread = new Thread(ListenLoop)
                {
                    IsBackground = true
                };
                _serverThread.Start();

                var url = $"http://localhost:{_servedPort}/index.html";
                Debug.Log($"[AutoBuilder Local Server] WebGL Local Server running at {url}");
                Application.OpenURL(url);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder Local Server] Failed to start HTTP server: {exception.Message}");
                StopServer();
            }
        }

        public static void StopServer()
        {
            _isRunning = false;

            if (_listener != null)
            {
                try
                {
                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                    }

                    _listener.Close();
                }
                catch
                {
                }
                finally
                {
                    _listener = null;
                }
            }

            if (_serverThread != null && _serverThread.IsAlive)
            {
                try
                {
                    _serverThread.Join(500);
                }
                catch
                {
                }
                finally
                {
                    _serverThread = null;
                }
            }
        }

        private static void ListenLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (_isRunning)
                    {
                        Debug.LogWarning($"[AutoBuilder Local Server] Listener exception: {exception.Message}");
                    }
                    break;
                }
            }
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                var rawUrl = request.Url.AbsolutePath.TrimStart('/');
                if (string.IsNullOrEmpty(rawUrl))
                {
                    rawUrl = "index.html";
                }

                var decodedPath = Uri.UnescapeDataString(rawUrl);
                var targetFilePath = Path.Combine(_servedFolder, decodedPath);

                if (!File.Exists(targetFilePath))
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Close();
                    return;
                }

                var mime = GetMimeType(targetFilePath);
                response.ContentType = mime;

                if (targetFilePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) || targetFilePath.EndsWith(".unityweb", StringComparison.OrdinalIgnoreCase) && IsGzipFile(targetFilePath))
                {
                    response.AddHeader("Content-Encoding", "gzip");
                }
                else if (targetFilePath.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
                {
                    response.AddHeader("Content-Encoding", "br");
                }

                response.AddHeader("Access-Control-Allow-Origin", "*");

                var fileBytes = File.ReadAllBytes(targetFilePath);
                response.ContentLength64 = fileBytes.Length;
                response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                response.OutputStream.Flush();
            }
            catch (Exception exception)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                Debug.LogWarning($"[AutoBuilder Local Server] Request failed: {exception.Message}");
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch
                {
                }
            }
        }

        private static bool IsGzipFile(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length < 2)
                    {
                        return false;
                    }

                    var b1 = fs.ReadByte();
                    var b2 = fs.ReadByte();
                    return b1 == 0x1F && b2 == 0x8B;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetMimeType(string path)
        {
            var lower = path.ToLowerInvariant();
            if (lower.EndsWith(".js") || lower.EndsWith(".js.gz") || lower.EndsWith(".js.br"))
            {
                return "application/javascript";
            }

            if (lower.EndsWith(".wasm") || lower.EndsWith(".wasm.gz") || lower.EndsWith(".wasm.br"))
            {
                return "application/wasm";
            }

            if (lower.EndsWith(".html") || lower.EndsWith(".htm"))
            {
                return "text/html";
            }

            if (lower.EndsWith(".css"))
            {
                return "text/css";
            }

            if (lower.EndsWith(".json"))
            {
                return "application/json";
            }

            if (lower.EndsWith(".png"))
            {
                return "image/png";
            }

            if (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg"))
            {
                return "image/jpeg";
            }

            if (lower.EndsWith(".gif"))
            {
                return "image/gif";
            }

            if (lower.EndsWith(".ico"))
            {
                return "image/x-icon";
            }

            if (lower.EndsWith(".svg"))
            {
                return "image/svg+xml";
            }

            return "application/octet-stream";
        }
    }
}
