# Snowglow mobile art budget

Reference: Samsung Galaxy A07, Android URP, 30 FPS (33.3 ms/frame). These are
conservative starting budgets for this mid low-poly game, not device limits or
evidence that most phones run it smoothly. Profile representative older/lower-end
phones and the A07 with snow simulation, UI, shadows and saving enabled.

| Content | Suggested working target | Review trigger |
| --- | --- | --- |
| Complete nearby vehicle, including wheels and equipment | 5,000–10,000 triangles | Above 15,000 triangles or 30,000 imported vertices |
| Small scenery prop | 50–1,000 triangles | Repeated instances or tiny screen footprint justify less detail |
| House/building assembly | 500–3,000 triangles | Above 5,000; consider repeated windows/trim and LOD |
| Vehicle materials | 1–4 shared materials, preferably one opaque palette plus glass | Above 12 material slots; count slots across every renderer |
| Vehicle renderers | About 6–10 for separate moving modules | Many tiny renderers; combine rigid parts in the Editor |
| Visible scene geometry | Initially aim around 100,000–200,000 triangles | Includes snow/terrain; count extra shadow passes separately |

Numerical targets above are Snowglow recommendations. They are advisory: screen
coverage, transparency/overdraw, shadow passes, shader cost, CPU draw submission,
mesh updates and physics can dominate at a low triangle count. Use Unity's device
Profiler and Frame Debugger for actual timings and submissions. The SRP Batcher
reduces submission/setup work; it does not make every material slot a single draw.

The new loader is within the vehicle geometry target: 7,656 vehicle triangles,
8,424 including its preauthored carried-snow visual. The complete prefab has
15,095 imported vertices, eight renderers and nine material slots / three distinct
materials. The tractor is also within the geometry target (7,274 triangles), but
its 30 material slots should be consolidated into a shared palette in a future
optimization pass. No triangle decimation is justified for these vehicles yet.

At the saved depot camera, an Editor frustum-bounds inventory found 69,770 mesh
triangles, including 54,432 snow triangles, and 69 material slots. This is only a
candidate inventory: it excludes Terrain, UI, particles and shadow passes, and
does not model batching or occlusion. Other camera positions and tall snow piles
can cost more. It is not a rendered-frame or FPS measurement.

Future catalog vehicle checks now warn above 15,000 triangles / 30,000 imported
vertices. Existing material-slot warnings remain active. Recheck whole equipped
vehicles: reviewing body, wheels and equipment separately can hide their total.
Use LOD/culling for distant/repeated assets, keep transparent glass modest, and
avoid adding tiny unseen bevels/bolts. Preserve each original source before any
optimization; author prepared meshes and proxies in the Editor.

Primary guidance: [Android geometry optimization](https://developer.android.com/games/optimize/geometry),
[Unity mobile graphics/art optimization](https://unity.com/how-to/mobile-game-optimization-tips-part-2).
