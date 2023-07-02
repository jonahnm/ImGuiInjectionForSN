using System;
using DearImguiSharp;
using BepInEx;
using System.Runtime.InteropServices;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.EventSystems;
using MonoMod.RuntimeDetour;
using System.Linq;
using System.Reflection;
using Plsworkthistime;

namespace SNIMGUILib
{
    [BepInPlugin("SNIMGUI", "SNIMGUI", "1.0.0")]
    public class Class1 : BaseUnityPlugin
    {
        [DllImport("untitled1.dll")]
        internal static extern void begin(IntPtr a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void callback(IntPtr a);
        internal static GCHandle callbackgch;
        internal static callback del;
        internal static bool ready = false;
        internal static SharpDX.Direct3D11.Device device;
        internal static SharpDX.Direct3D11.DeviceContext deviceContext;
        internal static IntPtr window;
        internal static RenderTargetView render_target;
        internal static ManualLogSource logger;
        public static bool cursorvisible = false;
        private static Dictionary<KeyCode, ImGuiKey> keytoim = new Dictionary<KeyCode, ImGuiKey>();
        private static string previnput = "";
        public delegate void imguievent();
        public static event imguievent ImGuiEvent = () => { };
        internal static Harmony harmony = new Harmony("ImGUIINJECTIONFORSN");

        void Awake()
        {
            harmony.PatchAll();
            foreach(var key in Enum.GetNames(typeof(KeyCode)))
            {
                try
                {
                    keytoim.Add((KeyCode)Enum.Parse(typeof(KeyCode), key), (ImGuiKey)Enum.Parse(typeof(ImGuiKey), key));
                } catch { }
            }
            del = new callback(RenderEvent);
            callbackgch = GCHandle.Alloc(del);
            var ptr = Marshal.GetFunctionPointerForDelegate(del);
            logger = Logger;
            begin(ptr);
            var allFlags = (BindingFlags)(-1);
            var unityEngineUIDll = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.GetName().Name == "UnityEngine.UI");
            var _eventSystemType = unityEngineUIDll.GetType("UnityEngine.EventSystems.EventSystem");
            var _eventSystemUpdate = _eventSystemType.GetMethod("Update", allFlags);
            var _eventSystemUpdateHook = new Hook(_eventSystemUpdate, this.GetType().GetMethod(nameof(IgnoreUI),allFlags));
            gameObject.AddComponent<MainThread>();
        }
        private static void IgnoreUI(Action<object> orig,object self)
        {
            if(cursorvisible)
            {
                return;
            }
            orig(self);
        } 
        void Update()
        {
            if(ready)
            {
                if(Input.GetKeyDown(KeyCode.Insert))
                {
                    if(!cursorvisible)
                    {
                        ImGui.GetIO().MouseDrawCursor = true;
                        ImGui.GetIO().ConfigFlags &= ~(int)ImGuiConfigFlags.NoMouse;
                        cursorvisible = true;
                    } else
                    {
                        ImGui.GetIO().MouseDrawCursor = false;
                        ImGui.GetIO().ConfigFlags |= (int)ImGuiConfigFlags.NoMouse;
                        cursorvisible = false;
                    }
                }
                try
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        ImGui.ImGuiIO_AddMouseButtonEvent(ImGui.GetIO(), 0, true);
                    }
                    if(Input.GetMouseButtonUp(0))
                    {
                        ImGui.ImGuiIO_AddMouseButtonEvent(ImGui.GetIO(), 0, false);
                    }
                    ImGui.ImGuiIO_AddKeyEvent(ImGui.GetIO(), (int)ImGuiKey.Backspace, Input.inputString == "\b");
                    ImGui.ImGuiIO_AddKeyEvent(ImGui.GetIO(), (int)ImGuiKey.Enter, Input.inputString == "\n");
                    ImGui.ImGuiIO_AddInputCharactersUTF8(ImGui.GetIO(), Input.inputString);
                    if (Input.inputString != "\b" && Input.inputString != "\n")
                    {
                        ImGui.ImGuiIO_AddKeyEvent(ImGui.GetIO(), (int)keytoim[(KeyCode)Enum.Parse(typeof(KeyCode), Input.inputString)], true);
                    }
                } catch { }
            }
        }
        static void RenderEvent(IntPtr swap_chain)
        {
            if (swap_chain == IntPtr.Zero)
            {
                logger.LogInfo("This is from begin!");
                return;
            }
            var managedswapchain = SwapChain.FromPointer<SwapChain>(swap_chain);
            if (!ready)
            {
                logger.LogInfo("Initing ImGui!");
                var description = managedswapchain.Description;
                device = managedswapchain.GetDevice<SharpDX.Direct3D11.Device>();
                deviceContext = device.ImmediateContext;
                window = description.OutputHandle;
                ImGui.CreateContext(null);
                ImGui.ImGuiImplWin32Init(window);
                logger.LogInfo("Created context!");
                unsafe
                {
                    var imguicontext = new ID3D11DeviceContext(deviceContext.NativePointer.ToPointer());
                    var imguidevice = new ID3D11Device(device.NativePointer.ToPointer());
                    ImGui.ImGuiImplDX11Init(imguidevice, imguicontext);
                }
                SharpDX.Direct3D11.Texture2D back_buffer = default;
                back_buffer = managedswapchain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0);
                render_target = new RenderTargetView(device, back_buffer);
                back_buffer.Dispose();
                ImGui.GetIO().SetPlatformImeDataFn = null;
                ready = true;
                logger.LogInfo("Done Initing!");
            }
            ImGui.ImGuiImplDX11NewFrame();
            ImGui.ImGuiImplWin32NewFrame();
            ImGui.NewFrame();
            ImGuiEvent();
            ImGui.EndFrame();
            ImGui.Render();
            deviceContext.OutputMerger.SetRenderTargets(render_target);
            ImGui.ImGuiImplDX11RenderDrawData(ImGui.GetDrawData());
        }
    }
}
