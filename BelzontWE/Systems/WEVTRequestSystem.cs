using Belzont.Interfaces;
using BelzontWE.Sprites;
using Game;

#if BURST
using UnityEngine.Scripting;
#else
#endif

namespace BelzontWE
{
    /// <summary>
    /// Batches per-frame VT <c>RequestRegion</c> calls for all active atlases.
    /// <para>
    /// <b>Why this system exists:</b> Previously, <see cref="WETextureAtlas.NotifyRendering"/>
    /// was called per-entity inside the <c>beginContextRendering</c> callback
    /// (<see cref="WERendererSystem"/>). With hundreds of visible text entities sharing
    /// a handful of atlases, this produced O(entities) redundant <c>RequestRegion</c> calls
    /// deep inside the render pipeline — hurting frame time.
    /// </para>
    /// <para>
    /// This system runs once per game frame (EndFrame phase) and iterates only the unique
    /// atlas instances, producing O(atlases) calls instead. This mirrors the game's own
    /// <c>VTTextureRequester.UpdateTexturesVTRequests</c> pattern, which batches requests
    /// outside the render pipeline.
    /// </para>
    /// </summary>
    public partial class WEVTRequestSystem : BelzontBasicSystem
    {
        protected override AllowedPhase UpdatePhase => AllowedPhase.EndFrame;

        private WEAtlasesLibrary m_atlasesLibrary;

#if BURST
        [Preserve]
#endif
        protected override void OnCreateWithBarrier()
        {
            m_atlasesLibrary = World.GetOrCreateSystemManaged<WEAtlasesLibrary>();
        }

#if BURST
        [Preserve]
#endif
        protected override void OnUpdate()
        {
            m_atlasesLibrary.NotifyAllVTAtlasesRendering();
        }
    }
}
