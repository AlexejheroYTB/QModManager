﻿// CODE FROM https://github.com/SubnauticaNitrox/Nitrox/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QModManager.SceneDebugger
{
    internal class SceneDebugger : BaseDebugger
    {
        internal readonly List<DebuggerAction> actionList = new List<DebuggerAction>();
        internal readonly BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        internal readonly KeyCode RayCastKey = KeyCode.F9;
        internal bool editMode;
        internal Vector2 gameObjectScrollPos;

        /// <summary>
        ///     Search used in hierarchy view to find gameobjects with the given name.
        /// </summary>
        internal string gameObjectSearch = "";

        internal string gameObjectSearchCache = "";
        internal bool gameObjectSearchIsSearching;
        internal string gameObjectSearchPatternInvalidMessage = "";
        internal List<GameObject> gameObjectSearchResult = new List<GameObject>();

        internal Vector2 hierarchyScrollPos;
        internal Vector2 monoBehaviourScrollPos;
        internal MonoBehaviour selectedMonoBehaviour;
        internal GameObject selectedObject;

        internal bool selectedObjectActiveSelf;
        internal Vector3 selectedObjectPos;
        internal Quaternion selectedObjectRot;
        internal Vector3 selectedObjectScale;
        internal Scene selectedScene;

        internal SceneDebugger() : base(500, null, KeyCode.S, true, false, false, GUISkinCreationOptions.DERIVEDCOPY)
        {
            ActiveTab = AddTab("Scenes", RenderTabScenes);
            AddTab("Hierarchy", RenderTabHierarchy);
            AddTab("GameObject", RenderTabGameObject);
            AddTab("MonoBehaviour", RenderTabMonoBehaviour);
        }

        internal override void Update()
        {
            if (Input.GetKeyDown(RayCastKey))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray, float.MaxValue, int.MaxValue);

                foreach (RaycastHit hit in hits)
                {
                    // Not using the player layer mask as we should be able to hit remote players.  Simply filter local player.
                    if (hit.transform.gameObject.name != "Player")
                    {
                        selectedObject = hit.transform.gameObject;
                        ActiveTab = GetTab("Hierarchy").Get();
                        break;
                    }
                }
            }
        }

        internal override void OnSetSkin(GUISkin skin)
        {
            base.OnSetSkin(skin);

            skin.SetCustomStyle("sceneLoaded", skin.label, s =>
            {
                s.normal = new GUIStyleState
                {
                    textColor = Color.green
                };
                s.fontStyle = FontStyle.Bold;
            });

            skin.SetCustomStyle("loadScene", skin.button, s => { s.fixedWidth = 60; });

            skin.SetCustomStyle("mainScene", skin.label, s =>
            {
                s.margin = new RectOffset(2, 10, 10, 10);
                s.alignment = TextAnchor.MiddleLeft;
                s.fontSize = 20;
            });

            skin.SetCustomStyle("fillMessage", skin.label, s =>
            {
                s.stretchWidth = true;
                s.stretchHeight = true;
                s.fontSize = 24;
                s.alignment = TextAnchor.MiddleCenter;
                s.fontStyle = FontStyle.Italic;
            });

            skin.SetCustomStyle("breadcrumb", skin.label, s =>
            {
                s.fontSize = 20;
                s.fontStyle = FontStyle.Bold;
            });

            skin.SetCustomStyle("breadcrumbNav", skin.box, s =>
            {
                s.stretchWidth = false;
                s.fixedWidth = 100;
            });

            skin.SetCustomStyle("options", skin.textField, s =>
            {
                s.fixedWidth = 200;
                s.margin = new RectOffset(8, 8, 4, 4);
            });
            skin.SetCustomStyle("options_label", skin.label, s => { s.alignment = TextAnchor.MiddleLeft; });

            skin.SetCustomStyle("bold", skin.label, s => { s.fontStyle = FontStyle.Bold; });
            skin.SetCustomStyle("error", skin.label, s =>
            {
                s.fontStyle = FontStyle.Bold;
                s.normal.textColor = Color.red;
            });
        }

        internal void RenderTabScenes()
        {
            using (new GUILayout.VerticalScope("Box"))
            {
                GUILayout.Label("All scenes", "header");
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    Scene currentScene = SceneManager.GetSceneByBuildIndex(i);
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    bool isSelected = selectedScene.IsValid() && currentScene == selectedScene;
                    bool isLoaded = currentScene.isLoaded;

                    using (new GUILayout.HorizontalScope("Box"))
                    {
                        if (GUILayout.Button($"{(isSelected ? ">> " : "")}{i}: {path.TruncateLeft(35)}", isLoaded ? "sceneLoaded" : "label"))
                        {
                            selectedScene = currentScene;
                            ActiveTab = GetTab("Hierarchy").Get();
                        }

                        if (isLoaded)
                        {
                            if (GUILayout.Button("Load", "loadScene"))
                            {
                                SceneManager.UnloadSceneAsync(i);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Unload", "loadScene"))
                            {
                                SceneManager.LoadSceneAsync(i);
                            }
                        }
                    }
                }
            }
        }

        internal void RenderTabHierarchy()
        {
            using (new GUILayout.HorizontalScope("Box"))
            {
                StringBuilder breadcrumbBuilder = new StringBuilder();
                if (selectedObject != null)
                {
                    Transform parent = selectedObject.transform;
                    while (parent != null)
                    {
                        breadcrumbBuilder.Insert(0, '/');
                        breadcrumbBuilder.Insert(0, string.IsNullOrEmpty(parent.name) ? "<no-name>" : parent.name);
                        parent = parent.parent;
                    }
                }

                breadcrumbBuilder.Insert(0, "//");
                GUILayout.Label(breadcrumbBuilder.ToString(), "breadcrumb");

                using (new GUILayout.HorizontalScope("breadcrumbNav"))
                {
                    if (GUILayout.Button("<<"))
                    {
                        selectedObject = null;
                    }

                    if (GUILayout.Button("<"))
                    {
                        selectedObject = selectedObject?.transform.parent?.gameObject;
                        selectedObjectActiveSelf = selectedObject.activeSelf;
                        selectedObjectPos = selectedObject.transform.position;
                        selectedObjectRot = selectedObject.transform.rotation;
                        selectedObjectScale = selectedObject.transform.localScale;
                    }
                }
            }

            // GameObject search textbox.
            using (new GUILayout.HorizontalScope("Box"))
            {
                gameObjectSearch = GUILayout.TextField(gameObjectSearch);

                // Disable searching if text is cleared after a search has happened.
                if (gameObjectSearchIsSearching && string.IsNullOrEmpty(gameObjectSearch))
                {
                    gameObjectSearchIsSearching = false;
                }

                if (gameObjectSearch.Length > 0)
                {
                    if (GUILayout.Button("Search", "button", GUILayout.Width(80)))
                    {
                        gameObjectSearchIsSearching = true;
                    }
                    else if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                    {
                        gameObjectSearchIsSearching = true;
                    }
                }
            }

            if (!gameObjectSearchIsSearching)
            {
                // Not searching, just select game objects from selected scene (if any).
                using (new GUILayout.VerticalScope("Box"))
                {
                    if (selectedScene.IsValid())
                    {
                        using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(hierarchyScrollPos))
                        {
                            hierarchyScrollPos = scroll.scrollPosition;
                            List<GameObject> showObjects = new List<GameObject>();
                            if (selectedObject == null)
                            {
                                showObjects = selectedScene.GetRootGameObjects().ToList();
                            }
                            else
                            {
                                foreach (Transform t in selectedObject.transform)
                                {
                                    showObjects.Add(t.gameObject);
                                }
                            }

                            foreach (GameObject child in showObjects)
                            {
                                string guiStyle = child.transform.childCount > 0 ? "bold" : "label";
                                if (GUILayout.Button($"{child.name}", guiStyle))
                                {
                                    selectedObject = child.gameObject;
                                    selectedObjectActiveSelf = selectedObject.activeSelf;
                                    selectedObjectPos = selectedObject.transform.position;
                                    selectedObjectRot = selectedObject.transform.rotation;
                                    selectedObjectScale = selectedObject.transform.localScale;
                                }
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label($"No selected scene\nClick on a Scene in '{GetTab("Hierarchy").Get().Name}'", "fillMessage");
                    }
                }
            }
            else
            {
                // Searching. Return all gameobjects with matching type name.
                if (gameObjectSearch != gameObjectSearchCache)
                {
                    try
                    {
                        Regex.IsMatch("", gameObjectSearch);
                        gameObjectSearchPatternInvalidMessage = "";
                    }
                    catch (Exception ex)
                    {
                        gameObjectSearchPatternInvalidMessage = ex.Message;
                    }

                    if (string.IsNullOrEmpty(gameObjectSearchPatternInvalidMessage))
                    {
                        gameObjectSearchResult = Resources.FindObjectsOfTypeAll<GameObject>().Where(go => Regex.IsMatch(go.name, gameObjectSearch)).OrderBy(go => go.name).ToList();
                        gameObjectSearchCache = gameObjectSearch;
                    }
                    else
                    {
                        GUILayout.Label(gameObjectSearchPatternInvalidMessage, "error");
                    }
                }

                using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(hierarchyScrollPos))
                {
                    hierarchyScrollPos = scroll.scrollPosition;

                    foreach (GameObject item in gameObjectSearchResult)
                    {
                        string guiStyle = item.transform.childCount > 0 ? "bold" : "label";
                        if (GUILayout.Button($"{item.name}", guiStyle))
                        {
                            selectedObject = item.gameObject;
                            selectedObjectActiveSelf = selectedObject.activeSelf;
                            selectedObjectPos = selectedObject.transform.position;
                            selectedObjectRot = selectedObject.transform.rotation;
                            selectedObjectScale = selectedObject.transform.localScale;
                        }
                    }
                }
            }
        }

        internal void RenderTabGameObject()
        {
            using (new GUILayout.VerticalScope("Box"))
            {
                if (selectedObject)
                {
                    using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(gameObjectScrollPos))
                    {
                        gameObjectScrollPos = scroll.scrollPosition;
                        RenderToggleButtons($"GameObject: {selectedObject.name}");

                        using (new GUILayout.VerticalScope("Box"))
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                selectedObjectActiveSelf = GUILayout.Toggle(selectedObjectActiveSelf, "Active");
                            }

                            GUILayout.Label("Position");
                            using (new GUILayout.HorizontalScope())
                            {
                                float.TryParse(GUILayout.TextField(selectedObjectPos.x.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectPos.x);
                                float.TryParse(GUILayout.TextField(selectedObjectPos.y.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectPos.y);
                                float.TryParse(GUILayout.TextField(selectedObjectPos.z.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectPos.z);
                            }

                            GUILayout.Label("Rotation");
                            using (new GUILayout.HorizontalScope())
                            {
                                float.TryParse(GUILayout.TextField(selectedObjectRot.x.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectRot.x);
                                float.TryParse(GUILayout.TextField(selectedObjectRot.y.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectRot.y);
                                float.TryParse(GUILayout.TextField(selectedObjectRot.z.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectRot.z);
                                float.TryParse(GUILayout.TextField(selectedObjectRot.w.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectRot.w);
                            }

                            GUILayout.Label("Scale");
                            using (new GUILayout.HorizontalScope())
                            {
                                float.TryParse(GUILayout.TextField(selectedObjectScale.x.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectScale.x);
                                float.TryParse(GUILayout.TextField(selectedObjectScale.y.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectScale.y);
                                float.TryParse(GUILayout.TextField(selectedObjectScale.z.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out selectedObjectScale.z);
                            }
                        }

                        foreach (MonoBehaviour behaviour in selectedObject.GetComponents<MonoBehaviour>())
                        {
                            using (new GUILayout.HorizontalScope("Box"))
                            {
                                if (GUILayout.Button(behaviour.GetType().Name))
                                {
                                    selectedMonoBehaviour = behaviour;
                                    ActiveTab = GetTab("MonoBehaviour").Get();
                                }
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label($"No selected GameObject\nClick on an object in '{GetTab("Hierarchy").Get().Name}'", "fillMessage");
                }
            }
        }

        internal void RenderTabMonoBehaviour()
        {
            using (new GUILayout.VerticalScope("Box"))
            {
                if (selectedMonoBehaviour)
                {
                    using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(monoBehaviourScrollPos))
                    {
                        monoBehaviourScrollPos = scroll.scrollPosition;
                        RenderToggleButtons($"MonoBehaviour: {selectedMonoBehaviour.GetType().Name}");

                        using (new GUILayout.VerticalScope("Box"))
                        {
                            GUILayout.Label("Fields", "header");
                            RenderAllMonoBehaviourFields(selectedMonoBehaviour);
                            GUILayout.Label("Methods", "header");
                            RenderAllMonoBehaviourMethods(selectedMonoBehaviour);
                        }
                    }
                }
                else
                {
                    GUILayout.Label($"No selected MonoBehaviour\nClick on an object in '{GetTab("MonoBehaviour").Get().Name}'", "fillMessage");
                }
            }
        }

        internal void RenderAllMonoBehaviourFields(MonoBehaviour mono)
        {
            FieldInfo[] fields = mono.GetType().GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                using (new GUILayout.HorizontalScope("box"))
                {
                    string[] fieldTypeNames = field.FieldType.ToString().Split('.');
                    GUILayout.Label("[" + fieldTypeNames[fieldTypeNames.Length - 1] + "]: " + field.Name, "options_label");
                    if (field.FieldType == typeof(bool))
                    {
                        bool boolVal = bool.Parse(GetValue(field, selectedMonoBehaviour));
                        if (GUILayout.Button(boolVal.ToString()))
                        {
                            RegisterFieldChanges(field, selectedMonoBehaviour, !boolVal);
                        }
                    }
                    else if (field.FieldType.BaseType == typeof(MonoBehaviour))
                    {
                        if (GUILayout.Button(field.Name))
                        {
                            selectedMonoBehaviour = (MonoBehaviour)field.GetValue(selectedMonoBehaviour);
                        }
                    }
                    else if (field.FieldType == typeof(GameObject))
                    {
                        if (GUILayout.Button(field.Name))
                        {
                            selectedObject = (GameObject)field.GetValue(selectedMonoBehaviour);
                            ActiveTab = GetTab("GameObject").Get();
                        }
                    }
                    else if (field.FieldType == typeof(Text))
                    {
                        if (GUILayout.Button(field.Name))
                        {
                            selectedMonoBehaviour = (Text)field.GetValue(selectedMonoBehaviour);
                        }
                    }
                    else if (field.FieldType == typeof(Texture) || field.FieldType == typeof(RawImage) || field.FieldType == typeof(Image))
                    {
                        Texture img;
                        if (field.FieldType == typeof(RawImage))
                        {
                            img = ((RawImage)field.GetValue(selectedMonoBehaviour)).mainTexture;
                        }
                        else if (field.FieldType == typeof(Image))
                        {
                            img = ((Image)field.GetValue(selectedMonoBehaviour)).mainTexture;
                        }
                        else
                        {
                            img = (Texture)field.GetValue(selectedMonoBehaviour);
                        }

                        GUIStyle style = new GUIStyle("box")
                        {
                            fixedHeight = 250 * (img.width / img.height),
                            fixedWidth = 250
                        };

                        GUILayout.Box(img, style);
                    }
                    else
                    {
                        try
                        {
                            //Check if convert work to prevent two TextFields
                            Convert.ChangeType(field.GetValue(selectedMonoBehaviour).ToString(), field.FieldType);
                            RegisterFieldChanges(field, selectedMonoBehaviour, Convert.ChangeType(GUILayout.TextField(GetValue(field, selectedMonoBehaviour), "options"), field.FieldType));
                        }
                        catch
                        {
                            GUILayout.TextField("Not implemented yet", "options");
                        }
                    }
                }
            }
        }

        internal void RenderAllMonoBehaviourMethods(MonoBehaviour mono)
        {
            MethodInfo[] methods = mono.GetType().GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).OrderBy(m => m.Name).ToArray();
            foreach (MethodInfo method in methods)
            {
                using (new GUILayout.VerticalScope("Box"))
                {
                    GUILayout.Label(method.ToString());
                    using (new GUILayout.HorizontalScope())
                    {
                        // TODO: Allow methods with parameters to be called.
                        if (!method.GetParameters().Any())
                        {
                            if (GUILayout.Button("Invoke"))
                            {
                                object result = method.Invoke(method.IsStatic ? null : mono, new object[0]);
                                if (result != null)
                                {
                                    ErrorMessage.AddMessage($"Invoked method {method.Name} which returned result: '{result}'.");
                                }
                                else
                                {
                                    ErrorMessage.AddMessage($"Invoked method {method.Name}. Return value was NULL.");
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void RenderToggleButtons(string label)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, "bold");
                editMode = GUILayout.Toggle(editMode, "EditMode");
                if (GUILayout.Button("Save"))
                {
                    SaveChanges();
                }
            }
        }

        internal void RegisterFieldChanges(FieldInfo field, Component component, object value)
        {
            if (editMode)
            {
                if (DebuggerAction.GetActionFromField(actionList, field) == null)
                {
                    DebuggerAction action = new DebuggerAction(component, field, component, value);
                    actionList.Add(action);
                }
                else
                {
                    DebuggerAction.GetActionFromField(actionList, field).Value = value;
                }
            }
        }

        internal string GetValue(FieldInfo field, object instance)
        {
            if (editMode)
            {
                foreach (DebuggerAction item in actionList)
                {
                    if (item.Field == field)
                    {
                        return item.Value.ToString();
                    }
                }
            }

            return field.GetValue(instance).ToString();
        }

        internal void SaveChanges()
        {
            selectedObject.SetActive(selectedObjectActiveSelf);
            selectedObject.transform.position = selectedObjectPos;
            selectedObject.transform.rotation = selectedObjectRot;
            selectedObject.transform.localScale = selectedObjectScale;

            foreach (DebuggerAction action in actionList)
            {
                action.SaveFieldValue();
            }

            actionList.Clear();
        }
    }

    public class DebuggerAction
    {
        public Component Component;
        public FieldInfo Field;
        public object Obj;
        public object Value;

        public DebuggerAction(Component component, FieldInfo field, object obj, object value)
        {
            Validate.NotNull(component);
            Validate.NotNull(field);
            Validate.NotNull(obj);

            Component = component;
            Field = field;
            Obj = obj;
            Value = value;
        }

        public static DebuggerAction GetActionFromField(List<DebuggerAction> list, FieldInfo field)
        {
            foreach (DebuggerAction item in list)
            {
                if (item.Field == field)
                {
                    return item;
                }
            }

            return null;
        }

        public void SaveFieldValue()
        {
            Field.SetValue(Obj, Value);
        }

        private static string GetGameObjectPath(GameObject gameObject)
        {
            StringBuilder path = new StringBuilder("/" + gameObject.name);
            Transform parent = gameObject.transform;
            while (parent.parent != null)
            {
                parent = parent.parent;
                path.Insert(1, parent.name + "/");
            }

            return path.ToString();
        }

        private static int GetComponentChildNumber(Component component)
        {
            int childNumber = -1;
            Component[] components = component.gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == component)
                {
                    childNumber = i;
                }
            }

            return childNumber;
        }
    }
}