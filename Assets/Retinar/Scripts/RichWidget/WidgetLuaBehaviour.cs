/*
*Copyright(C) by SWM
*Author: Samuel
*Version: 4.4.4
*UnityVersion：2018.4.23f1
*Date: 2020-10-12 11:25:01
*/
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using XLua;

namespace Samuel.RichWidget
{
    public static class CustomLuaCallCS
    {
        [LuaCallCSharp]
        public static IEnumerable<Type> mymodule_lua_call_cs_list = new List<Type> ()
        {
            typeof(DG.Tweening.DOVirtual),
            typeof(DG.Tweening.EaseFactory),
            typeof(DG.Tweening.Tweener),
            typeof(DG.Tweening.Tween),
            typeof(DG.Tweening.Core.ABSSequentiable),
            typeof(DG.Tweening.DOTweenModuleUI),
            typeof(DG.Tweening.TweenCallback),
            typeof(DG.Tweening.TweenExtensions),
            typeof(DG.Tweening.Sequence),
            typeof(DG.Tweening.DOTweenPath),
            typeof(DG.Tweening.DOTween),
            typeof(DG.Tweening.DOTweenAnimation),
            typeof(DG.Tweening.DOTweenAnimationExtensions),
            typeof(DG.Tweening.DOTweenCYInstruction),
            typeof(DG.Tweening.DOTweenInspectorMode),
            typeof(DG.Tweening.DOTweenModuleAudio),
            typeof(DG.Tweening.DOTweenModulePhysics),
            typeof(DG.Tweening.DOTweenModulePhysics2D),
            typeof(DG.Tweening.DOTweenModuleSprite),
            typeof(DG.Tweening.DOTweenModuleUnityVersion),
            typeof(DG.Tweening.DOTweenModuleUtils),
            typeof(DG.Tweening.DOTweenProShortcuts),
            typeof(DG.Tweening.DOTweenVisualManager),
            typeof(DG.Tweening.ShortcutExtensions),
            typeof(DG.Tweening.TweenParams),
            typeof(DG.Tweening.TweenSettingsExtensions),
            //typeof(Mapbox.Directions.DirectionResource),
            //typeof(Mapbox.Utils.Vector2d),
            //typeof(Mapbox.Utils.RectD),
            //typeof(Mapbox.Unity.MeshGeneration.Data.UnityTile),
            //typeof(Samuel.Utility.SamuelUtility),
            //typeof(Samuel.MessageBox.UIMessageBoxWidgetsUniWebView),
            //typeof(Samuel.MessageBox.UIMessageBoxFullScreenMediaPlayer),
            //typeof(Samuel.MessageBox.UIMessageBoxFullScreenAudioPlayer),
            //typeof(Samuel.MessageBox.UIMessageBoxFullScreenImagePlayer),
            //typeof(Samuel.MessageBox.UIMessageBoxFullScreenAssetbundlePlayer),
            typeof(RenderSettings),
            typeof(VideoPlayer),
            typeof(Animator),
            typeof(Animation),
            typeof(Material),
            //typeof(SWMMedia),
            //typeof(List<SWMMedia>)
        };

        [CSharpCallLua]
        public static IEnumerable<Type> mymodule_cs_call_lua_list = new List<Type> ()
        {
            typeof(Action<float>),
            typeof(Action<int>),
            //typeof(Action<bool,List<SWMMedia>>)
        };
    }

    [Serializable]
    public class Injection
    {
        public string name;
        public UnityEngine.Object value;
    }

    public class WidgetLuaBehaviour : MonoBehaviour
    {
        [SerializeField] private bool _loadFromResources;
        [SerializeField] private string _loadFromResourcesPath;
        [SerializeField] private TextAsset _luaScript;
        [SerializeField] private Injection[] _injections;

        internal static LuaEnv luaEnv = new LuaEnv (); //all lua behaviour shared one luaenv only!
        internal static float lastGCTime = 0;
        internal const float GCInterval = 1;//1 second

        private Action _luaStart;
        private Action _luaUpdate;
        private Action _luaOnDestroy;
        private Action _luaOnEnable;
        private Action _luaOnDisable;

        private LuaTable _scriptEnv;

        void Awake ()
        {
            luaEnv.AddLoader (CustomLoaderFromXLuaResourcesPath);

            if (_loadFromResources)
                _luaScript = Resources.Load<TextAsset> (_loadFromResourcesPath);

            if (_luaScript == null)
                return;

            _scriptEnv = luaEnv.NewTable ();

            // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
            LuaTable meta = luaEnv.NewTable ();
            meta.Set ("__index", luaEnv.Global);
            _scriptEnv.SetMetaTable (meta);
            meta.Dispose ();

            _scriptEnv.Set ("self", this);
            foreach (var injection in _injections)
            {
                _scriptEnv.Set (injection.name, injection.value);
            }

            luaEnv.DoString (_luaScript.text, _luaScript.name, _scriptEnv);

            Action luaAwake = _scriptEnv.Get<Action> ("Awake");
            luaAwake?.Invoke ();

            _scriptEnv.Get ("Start", out _luaStart);
            _scriptEnv.Get ("Update", out _luaUpdate);
            _scriptEnv.Get ("OnDestroy", out _luaOnDestroy);
            _scriptEnv.Get ("OnEnable", out _luaOnEnable);
            _scriptEnv.Get ("OnDisable", out _luaOnDisable);
        }

        // Use this for initialization
        void Start ()
        {
            if (_luaScript == null)
                return;
            _luaStart?.Invoke ();
        }

        // Update is called once per frame
        void Update ()
        {
            if (_luaScript == null)
                return;
            _luaUpdate?.Invoke ();
            if (Time.time - lastGCTime > GCInterval)
            {
                luaEnv.Tick ();
                lastGCTime = Time.time;
            }
        }

        void OnDestroy ()
        {
            if (_luaScript == null)
                return;
            _luaOnDestroy?.Invoke ();
            _luaOnDestroy = null;
            _luaUpdate = null;
            _luaStart = null;
            _scriptEnv.Dispose ();
            _injections = null;
        }

        private void OnEnable ()
        {
            if (_luaScript == null)
                return;
            _luaOnEnable?.Invoke ();
        }

        private void OnDisable ()
        {
            if (_luaScript == null)
                return;
            _luaOnDisable?.Invoke ();
        }

        public void ExcuteLuaFunction (string functionName)
        {
            if (_luaScript == null)
                return;
            if (_scriptEnv == null)
                return;

            var a = _scriptEnv.Get<Action> (functionName);
            a?.Invoke ();
        }

        public void ExcuteLuaFunction (string functionName, params object[] vs)
        {
            if (_luaScript == null)
                return;
            if (_scriptEnv == null)
                return;

            var a = _scriptEnv.Get<Action<object[]>> (functionName);
            a?.Invoke (vs);
        }

        public void SetLuaValue<T> (string fieldName, T value)
        {
            if (_luaScript == null)
                return;
            if (_scriptEnv == null)
                return;

            Debug.Log ($"Set : {fieldName} => {value}");
            _scriptEnv.Set (fieldName, value);
        }

        public byte[] CustomLoaderFromXLuaResourcesPath (ref string fileName)
        {
            TextAsset textAsset = Resources.Load<TextAsset> ("Util.lua");

            if (textAsset != null)
            {
                return System.Text.Encoding.UTF8.GetBytes (textAsset.text);
            }

            return null;
        }
    }
}