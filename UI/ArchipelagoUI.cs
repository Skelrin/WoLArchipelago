using UnityEngine;

namespace WoLArchipelago
{
    public class ArchipelagoUI : MonoBehaviour
    {
        public static ArchipelagoUI Instance { get; private set; }

        private bool showUI = false;
        private Rect windowRect = new Rect(40, 40, 350, 300);
        private string uiHost = "127.0.0.1";
        private string uiPort = "38281";
        private string uiSlot = "Skelrin";
        private string uiPassword = "";
        private bool hasConnectedOnce = false;

        private void Awake()
        {
            Instance = this;
        }

        public void ToggleHasConnectedOnce()
        {
            hasConnectedOnce = !hasConnectedOnce;
        }

        public void Toggle()
        {
            showUI = !showUI;
        }

        public void Hide() => showUI = false;

        private void OnGUI()
        {
            if (showUI)
            {
                GUI.skin = null;
                windowRect = GUI.Window(999, windowRect, DrawArchipelagoWindow, "Archipelago Connection");
            }

            if (!Plugin.AP.IsConnected && hasConnectedOnce)
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
            GUI.Box(new Rect(0, 0, windowRect.width, windowRect.height), "", GUI.skin.box);
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            GUILayout.Label("Host:");
            uiHost = GUILayout.TextField(uiHost);
            GUILayout.Label("Port:");
            uiPort = GUILayout.TextField(uiPort);
            GUILayout.Label("Slot Name:");
            uiSlot = GUILayout.TextField(uiSlot);
            GUILayout.Label("Password:");
            uiPassword = GUILayout.PasswordField(uiPassword, '*');
            GUILayout.Space(10);

            if (!Plugin.AP.IsConnected)
            {
                string buttonText = hasConnectedOnce ? "Reconnect" : "Connect";

                if (GUILayout.Button(buttonText, GUILayout.Height(30)))
                {
                    if (int.TryParse(uiPort, out int port)) 
                    {
                        Plugin.AP.Connect(uiHost, port, uiSlot, uiPassword, isReconnecting: hasConnectedOnce);
                    }
                    else 
                    {
                        Plugin.Log.LogError("Invalid Port!");
                    }
                }
            }
            else
            {
                hasConnectedOnce = true;

                if (GUILayout.Button("Disconnect", GUILayout.Height(30))) 
                {
                    Plugin.AP.Disconnect();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label($"Status: {Plugin.AP.StatusMessage}");
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}