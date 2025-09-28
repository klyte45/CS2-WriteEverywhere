using Belzont.Interfaces;
using Belzont.Utils;
using cohtml.Net;
using Colossal.UI;
using Game.UI;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using UnityEngine;
using BelzontWE.Sprites;

namespace BelzontWE
{
    public class GameUIResourceHandlerOverrides : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            AddRedirect(typeof(GameUIResourceHandler).GetMethod("OnResourceRequest", RedirectorUtils.allFlags), GetType().GetMethod(nameof(BeforeOnResourceRequest), RedirectorUtils.allFlags));
            AddRedirect(typeof(DefaultResourceHandler).GetMethod("OnResourceRequest", RedirectorUtils.allFlags), GetType().GetMethod(nameof(BeforeOnResourceRequest), RedirectorUtils.allFlags));
            AddRedirect(typeof(cohtml.DefaultResourceHandler).GetMethod("OnResourceStreamRequest", RedirectorUtils.allFlags), GetType().GetMethod(nameof(BeforeOnResourceStreamRequest), RedirectorUtils.allFlags));
            AddRedirect(typeof(DefaultResourceHandler).GetMethod("OnResourceStreamRequest", RedirectorUtils.allFlags), GetType().GetMethod(nameof(BeforeOnResourceStreamRequest), RedirectorUtils.allFlags));
        }

        private static bool BeforeOnResourceRequest(ref IResourceRequest request, ref IResourceResponse response)
        {
            var url = request.GetURL();
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog("RES URL = " + url);
            if (url.StartsWith("coui://we.k45/_fonts/"))
            {
                var fontName = url["coui://we.k45/_fonts/".Length..];
                if (FontServer.Instance.TryGetFont(fontName, out var fontData))
                {
                    var arrData = fontData.Font._font.data.ArrayData;
                    var size = (ulong)arrData.Length;
                    var space = response.GetSpace(size);
                    Marshal.Copy(arrData, 0, space, arrData.Length);
                    response.Finish(ResourceResponse.Status.Success);
                    return false;
                }
            }
            else if (url.StartsWith("coui://we.k45/_css/"))
            {
                var fontName = url["coui://we.k45/_css/".Length..];
                if (FontServer.Instance.TryGetFont(fontName, out var font))
                {
                    response.SetStatus(200);
                    var data = Encoding.UTF8.GetBytes($"  @font-face {{\r\n        font-family: \"K45WE_{font.Guid}\";\r\n        src: url(coui://we.k45/_fonts/{fontName}) format('truetype');\r\n    }}");
                    var size = (ulong)data.Length;
                    var space = response.GetSpace(size);
                    Marshal.Copy(data, 0, space, data.Length);
                    response.Finish(ResourceResponse.Status.Success);
                    return false;
                }
            }
            else if (url.StartsWith("coui://we.k45/_textureAtlas/"))
            {
                var atlasName = HttpUtility.UrlDecode(url["coui://we.k45/_textureAtlas/".Length..]);

                if (WEAtlasesLibrary.Instance.TryGetAtlas(atlasName, out var textureAtlas))
                {
                    response.SetStatus(200);
                    var data = textureAtlas.Main.MakeReadable().EncodeToPNG();
                    var size = (ulong)data.Length;
                    var space = response.GetSpace(size);
                    Marshal.Copy(data, 0, space, data.Length);
                    response.Finish(ResourceResponse.Status.Success);
                    return false;
                }
            }
            return true;
        }
        private static bool BeforeOnResourceStreamRequest(ref IResourceRequest request, ref IResourceStreamResponse response)
        {
            var url = request.GetURL();
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog("STR URL = " + url);
            if (url.StartsWith("coui://we.k45/_fonts/"))
            {
                var fontName = url["coui://we.k45/_fonts/".Length..];
                if (FontServer.Instance.TryGetFont(fontName, out var fontData))
                {
                    var arrData = fontData.Font._font.data.ArrayData;
                    response.SetStreamReader(new StreamReader(arrData));
                    response.Finish(ResourceStreamResponse.Status.Success);
                    return false;
                }
            }
            else if (url.StartsWith("coui://we.k45/_css/"))
            {
                var fontName = url["coui://we.k45/_css/".Length..];
                if (FontServer.Instance.TryGetFont(fontName, out var entity))
                {
                    var data = Encoding.UTF8.GetBytes($"  @font-face {{\r\n        font-family: \"K45WE_{entity.Guid}\";\r\n        src: url(coui://we.k45/_fonts/{fontName}) format('truetype');\r\n    }}");
                    response.SetStreamReader(new StreamReader(data));
                    response.Finish(ResourceStreamResponse.Status.Success);
                    return false;
                }
            }
            else if (url.StartsWith("coui://we.k45/_textureAtlas/"))
            {
                var atlasName = HttpUtility.UrlDecode(url["coui://we.k45/_textureAtlas/".Length..]);

                if (WEAtlasesLibrary.Instance.TryGetAtlas(atlasName, out var textureAtlas))
                {
                    response.SetStreamReader(new StreamReader(textureAtlas.Main.EncodeToPNG()));
                    response.Finish(ResourceStreamResponse.Status.Success);
                    return false;
                }
            }
            return true;
        }
    }
}
