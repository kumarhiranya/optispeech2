using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace Optispeech.Documentation {

    /// <summary>
    /// Creates a web server and hosts the documentation website on it while the program is running. This server will, assuming the ports aren't opened,
    /// only be accessible on this computer. This allows the documentation to be a website the user can see in the browser, without the need to make it
    /// public to the world wide web. 
    /// </summary>
    public class DocumentationServer : MonoBehaviour {

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static DocumentationServer Instance = default;

        /// <summary>
        /// The server the documentation is being hosted on
        /// </summary>
        private SimpleHTTPServer myServer;

        [HideInDocumentation]
        private void Awake() {
            if (Instance == null || Instance == this) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
                return;
            }
        }

        [HideInDocumentation]
        private void Start() {
            string docsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs"));
            if (Directory.Exists(docsPath)) {
                myServer = new SimpleHTTPServer(docsPath);
                Debug.Log($"Hosting documentation at {docsPath} on http:/localhost:{myServer.Port}");
            }
        }

        [HideInDocumentation]
        public void OpenURL(string page) {
            Application.OpenURL("http:/localhost:" + Instance.myServer.Port + "/" + page);
        }

        [HideInDocumentation]
        private void OnApplicationQuit() {
            myServer.Stop();
        }

        /// <summary>
        /// A simple http server implementation from <see href="https://answers.unity.com/questions/1245582/create-a-simple-https-server-on-the-streaming-asset.html">this Unity answer</see>
        /// </summary>
        class SimpleHTTPServer {

            /// <summary>
            /// List of files that represent an "index" page
            /// </summary>
            private readonly string[] _indexFiles = {
                "index.html",
                "index.htm",
                "default.html",
                "default.htm"
            };

            /// <summary>
            /// Dictionary of mime types for handling various file types
            /// </summary>
            private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
#region extension to MIME type list
                    { ".asf", "video/x-ms-asf" },
                    { ".asx", "video/x-ms-asf" },
                    { ".avi", "video/x-msvideo" },
                    { ".bin", "application/octet-stream" },
                    { ".cco", "application/x-cocoa" },
                    { ".crt", "application/x-x509-ca-cert" },
                    { ".css", "text/css" },
                    { ".deb", "application/octet-stream" },
                    { ".der", "application/x-x509-ca-cert" },
                    { ".dll", "application/octet-stream" },
                    { ".dmg", "application/octet-stream" },
                    { ".ear", "application/java-archive" },
                    { ".eot", "application/octet-stream" },
                    { ".exe", "application/octet-stream" },
                    { ".flv", "video/x-flv" },
                    { ".gif", "image/gif" },
                    { ".hqx", "application/mac-binhex40" },
                    { ".htc", "text/x-component" },
                    { ".htm", "text/html" },
                    { ".html", "text/html" },
                    { ".ico", "image/x-icon" },
                    { ".img", "application/octet-stream" },
                    { ".svg", "image/svg+xml" },
                    { ".iso", "application/octet-stream" },
                    { ".jar", "application/java-archive" },
                    { ".jardiff", "application/x-java-archive-diff" },
                    { ".jng", "image/x-jng" },
                    { ".jnlp", "application/x-java-jnlp-file" },
                    { ".jpeg", "image/jpeg" },
                    { ".jpg", "image/jpeg" },
                    { ".js", "application/x-javascript" },
                    { ".mml", "text/mathml" },
                    { ".mng", "video/x-mng" },
                    { ".mov", "video/quicktime" },
                    { ".mp3", "audio/mpeg" },
                    { ".mpeg", "video/mpeg" },
                    { ".mp4", "video/mp4" },
                    { ".mpg", "video/mpeg" },
                    { ".msi", "application/octet-stream" },
                    { ".msm", "application/octet-stream" },
                    { ".msp", "application/octet-stream" },
                    { ".pdb", "application/x-pilot" },
                    { ".pdf", "application/pdf" },
                    { ".pem", "application/x-x509-ca-cert" },
                    { ".pl", "application/x-perl" },
                    { ".pm", "application/x-perl" },
                    { ".png", "image/png" },
                    { ".prc", "application/x-pilot" },
                    { ".ra", "audio/x-realaudio" },
                    { ".rar", "application/x-rar-compressed" },
                    { ".rpm", "application/x-redhat-package-manager" },
                    { ".rss", "text/xml" },
                    { ".run", "application/x-makeself" },
                    { ".sea", "application/x-sea" },
                    { ".shtml", "text/html" },
                    { ".sit", "application/x-stuffit" },
                    { ".swf", "application/x-shockwave-flash" },
                    { ".tcl", "application/x-tcl" },
                    { ".tk", "application/x-tcl" },
                    { ".txt", "text/plain" },
                    { ".war", "application/java-archive" },
                    { ".wbmp", "image/vnd.wap.wbmp" },
                    { ".wmv", "video/x-ms-wmv" },
                    { ".xml", "text/xml" },
                    { ".xpi", "application/x-xpinstall" },
                    { ".zip", "application/zip" },
#endregion
            };
            /// <summary>
            /// The thread the http server is running on
            /// </summary>
            private Thread _serverThread;
            /// <summary>
            /// The root directory that is being served
            /// </summary>
            private string _rootDirectory;
            /// <summary>
            /// The listener for all http messages to the server
            /// </summary>
            private HttpListener _listener;
            /// <summary>
            /// The port the server is running on
            /// </summary>
            private int _port;

            /// <summary>
            /// Property that allows public read-only access to the port the server is running on
            /// </summary>
            public int Port {
                get { return _port; }
                private set { }
            }

            /// <summary>
            /// Construct server with given port.
            /// </summary>
            /// <param name="path">Directory path to serve.</param>
            /// <param name="port">Port of the server.</param>
            public SimpleHTTPServer(string path, int port) {
                this.Initialize(path, port);
            }

            /// <summary>
            /// Construct server with suitable port.
            /// </summary>
            /// <param name="path">Directory path to serve.</param>
            public SimpleHTTPServer(string path) {
                //get an empty port
                TcpListener l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                int port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
                this.Initialize(path, port);
            }

            /// <summary>
            /// Stop server and dispose all functions.
            /// </summary>
            public void Stop() {
                _serverThread.Abort();
                _listener.Stop();
            }

            /// <summary>
            /// Start http listener and continuously process requests
            /// </summary>
            private void Listen() {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
                _listener.Start();
                while (true) {
                    try {
                        HttpListenerContext context = _listener.GetContext();
                        Process(context);
                    } catch (Exception ex) {
                        print(ex);
                    }
                }
            }

            /// <summary>
            /// Processes a given http request
            /// </summary>
            /// <param name="context">The http request to process</param>
            private void Process(HttpListenerContext context) {
                string filename = context.Request.Url.AbsolutePath;
                filename = filename.Substring(1);

                if (string.IsNullOrEmpty(filename)) {
                    foreach (string indexFile in _indexFiles) {
                        if (File.Exists(Path.Combine(_rootDirectory, indexFile))) {
                            filename = indexFile;
                            break;
                        }
                    }
                }

                filename = Path.Combine(_rootDirectory, filename);

                if (File.Exists(filename)) {
                    try {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        Stream input = new FileStream(filename, FileMode.Open);

                        //Adding permanent http response headers
                        string mime;
                        context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                        context.Response.ContentLength64 = input.Length;
                        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                        context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

                        byte[] buffer = new byte[1024 * 16];
                        int nbytes;
                        while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                            context.Response.OutputStream.Write(buffer, 0, nbytes);
                        input.Close();


                        context.Response.OutputStream.Flush();
                    } catch (Exception ex) {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        print(ex);
                    }

                } else {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }

                context.Response.OutputStream.Close();
            }

            /// <summary>
            /// Creates thread to listen for http requests on the provided port, and serves pages found in the given path
            /// </summary>
            /// <param name="path">The directory of files to serve</param>
            /// <param name="port">The port to listen for http requests on</param>
            private void Initialize(string path, int port) {
                this._rootDirectory = path;
                this._port = port;
                _serverThread = new Thread(this.Listen);
                _serverThread.Start();
            }
        }
    }
}
