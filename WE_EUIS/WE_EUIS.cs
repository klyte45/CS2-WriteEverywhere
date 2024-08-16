//#define LOCALURL

using BelzontWE;
using K45EUIS_Ext;
using System;
using Unity.Entities;

namespace z_WE_EUIS
{
    public class WE_EUIS : IEUISModRegister
    {
        public string ModderIdentifier => "k45";
        public string ModAcronym => "we";
        public Action<Action<string, object[]>> OnGetEventEmitter => (eventCaller) => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETestController>().SetupCaller(eventCaller);
        public Action<Action<string, Delegate>> OnGetEventsBinder => (eventCaller) => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETestController>().SetupEventBinder(eventCaller);
        public Action<Action<string, Delegate>> OnGetCallsBinder => (eventCaller) => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WETestController>().SetupCallBinder(eventCaller);
    }
    public class WE_EUIS_Main : IEUISAppRegister
    {
        public string ModAppIdentifier => "main";

        public string DisplayName => "Write Everywhere - Main";

#if DEBUG
        public string UrlJs => "http://localhost:8775/k45-we-main.js";//
        public string UrlCss => "http://localhost:8775/k45-we-main.css";//
        public string UrlIcon => $"coui://{WriteEverywhereCS2Mod.Instance.CouiHost}/UI/images/WE.svg";
#else
        public string UrlJs => $"coui://{WriteEverywhereCS2Mod.Instance.CouiHost}/UI/k45-we-main.js";
        public string UrlCss => $"coui://{WriteEverywhereCS2Mod.Instance.CouiHost}/UI/k45-we-main.css";
        public string UrlIcon => $"coui://{WriteEverywhereCS2Mod.Instance.CouiHost}/UI/images/WE.svg";
#endif

        public string ModderIdentifier => "k45";

        public string ModAcronym => "we";
    }
}
