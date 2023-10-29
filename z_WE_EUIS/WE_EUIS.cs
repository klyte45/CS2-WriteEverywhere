#define LOCALURL

using BelzontWE;
using K45EUIS_Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace z_WE_EUIS
{
    public class WE_EUIS : IEUISModRegister
    {
        public string ModderIdentifier => "k45";
        public string ModAcronym => "we";
        public Action<Action<string, object[]>> OnGetEventEmitter => (eventCaller) => WriteEverywhereCS2Mod.Instance.SetupCaller(eventCaller);
        public Action<Action<string, Delegate>> OnGetEventsBinder => (eventCaller) => WriteEverywhereCS2Mod.Instance.SetupEventBinder(eventCaller);
        public Action<Action<string, Delegate>> OnGetCallsBinder => (eventCaller) => WriteEverywhereCS2Mod.Instance.SetupCallBinder(eventCaller);
    }
    public class WE_EUIS_Main : IEUISAppRegister
    {
        public string ModAppIdentifier => "main";

        public string DisplayName => "Write Everywhere - Main";

#if LOCALURL
        public string UrlJs => "http://localhost:8500/k45-we-main.js";//
        public string UrlCss => "http://localhost:8500/k45-we-main.css";//
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
