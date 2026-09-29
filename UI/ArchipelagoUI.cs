using UnityEngine;

namespace WoLArchipelago
{
    /// <summary>
    /// GUI component rendering the Archipelago connection panel and disconnection alert.
    /// </summary>
    public class ArchipelagoUI : MonoBehaviour
    {
        public static ArchipelagoUI Instance { get; private set; }

        private bool _showUI;
        private Rect _windowRect = new(40f, 40f, 350f, 300f);
        private string _uiHost = "archipelago.gg";
        private string _uiPort = "";
        private string _uiSlot = "";
        private string _uiPassword = "";
        private bool _hasConnectedOnce;

        private void Awake()
        {
            Instance = this;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Instance?.Toggle();
            }
        }

        public void ToggleHasConnectedOnce()
        {
            _hasConnectedOnce = !_hasConnectedOnce;
        }

        public void Toggle()
        {
            _showUI = !_showUI;
        }

        public void Hide()
        {
            _showUI = false;
        }

        private void OnGUI()
        {
            if (_showUI)
            {
                GUI.skin = null;
                _windowRect = GUI.Window(999, _windowRect, DrawArchipelagoWindow, "Archipelago Connection");
            }

            if (Plugin.AP != null && !Plugin.AP.IsConnected && _hasConnectedOnce)
            {
                GUIStyle warningStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Normal
                };
                warningStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

                float width = 220f;
                float height = 22f;
                float posX = (Screen.width - width) / 2f;
                float posY = 10f;

                GUI.Box(new Rect(posX, posY, width, height), "Archipelago : Connection Lost", warningStyle);
            }
        }

        private void DrawArchipelagoWindow(int windowID)
        {
            GUI.Box(new Rect(0, 0, _windowRect.width, _windowRect.height), "", GUI.skin.box);
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            GUILayout.Label("Host:");
            _uiHost = GUILayout.TextField(_uiHost);

            GUILayout.Label("Port:");
            _uiPort = GUILayout.TextField(_uiPort);

            GUILayout.Label("Slot Name:");
            _uiSlot = GUILayout.TextField(_uiSlot);

            GUILayout.Label("Password:");
            _uiPassword = GUILayout.PasswordField(_uiPassword, '*');
            GUILayout.Space(10);

            if (Plugin.AP != null && !Plugin.AP.IsConnected)
            {
                string buttonText = _hasConnectedOnce ? "Reconnect" : "Connect";

                if (GUILayout.Button(buttonText, GUILayout.Height(30)))
                {
                    if (int.TryParse(_uiPort, out int port))
                    {
                        Plugin.AP.Connect(_uiHost, port, _uiSlot, _uiPassword, isReconnecting: _hasConnectedOnce);
                    }
                    else
                    {
                        Plugin.Log.LogError("[AP] Invalid Port entered in UI.");
                    }
                }
            }
            else
            {
                _hasConnectedOnce = true;

                if (GUILayout.Button("Disconnect", GUILayout.Height(30)))
                {
                    if (Plugin.AP != null)
                    {
                        Plugin.AP.Disconnect();
                    }
                }
            }

            GUILayout.Space(10);

            string statusMessage = (Plugin.AP != null) ? Plugin.AP.StatusMessage : "Disconnected";
            GUILayout.Label(string.Format("Status: {0}", statusMessage));

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}