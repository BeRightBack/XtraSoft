using Microsoft.JSInterop;

namespace XtraSoft.WebGL.Babylon
{
    public class Engine(IJSRuntime jsRuntime, JsRuntimeObjectRef objRef) : BabylonObject(jsRuntime, objRef)
    {
    }
}
