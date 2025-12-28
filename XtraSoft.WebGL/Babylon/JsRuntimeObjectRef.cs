using Microsoft.JSInterop;
using System.Text.Json.Serialization;

namespace XtraSoft.WebGL.Babylon
{
    public sealed class JsRuntimeObjectRef : IDisposable, IAsyncDisposable
    {
        internal IJSRuntime? JSRuntime { get; set; }

        [JsonPropertyName("__jsObjRefId")]
        public int JsObjectRefId { get; set; }

        public void Dispose()
        {
            // Route sync dispose through async path to properly observe the ValueTask
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (JSRuntime is not null)
            {
                await JSRuntime.InvokeVoidAsync("babylonInterop.removeObjectRef", JsObjectRefId);
            }

            GC.SuppressFinalize(this);
        }
    }
}
