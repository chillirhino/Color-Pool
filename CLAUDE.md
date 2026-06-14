# Color Pool Painting — 3D (Unity)

A casual mobile game: **paint-by-numbers meets pool**. The player slingshots a
white cue ball across a flat table; colored balls knocked rolling paint the
numbered regions of a hidden picture as they pass over matching-color regions.
Finish the picture to win.

> A working 2D reference implementation lives in `../prototype-color-pool/index.html`
> (vanilla JS + Canvas). It is the **source of truth for game feel and tuning** —
> physics constants, paint logic, and the settings model below were validated there.
> This document specifies the 3D Unity port. **Magnet mode from the prototype is
> intentionally out of scope for now.**

---

## 1. Core concept

- The table is a flat rectangular surface (like a pool/billiard table) laid out
  in 3D, viewed from a **near-top-down angled camera** (≈55–70° downward tilt) so
  balls have real 3D volume and lighting but the picture on the table is clearly
  readable.
- The table surface displays a **paint-by-numbers picture**: an illustration
  divided into outlined regions, each labelled with a color number (1–10). Regions
  start unpainted (white with a faint number).
- 10 numbered colors. Each colored ball carries a color number; the white cue ball
  carries none.
- The player **drag-aims a slingshot** on the cue ball and releases to launch it.
- Real ball physics: balls collide with each other and bounce off the table
  cushions (rails), losing energy to friction until they stop.
- As a **colored ball rolls over a region whose number matches its color**, that
  region gets painted. Rolling over non-matching regions does nothing.
- The level is **complete when every region is painted**, then the finished
  picture is celebrated (animate/sparkle).

---

## 2. The 3D translation (what changes from the 2D prototype)

| Aspect | 2D prototype | 3D Unity target |
|---|---|---|
| Table | 960×600 canvas | Flat mesh/quad, world units (e.g. 9.6 × 6.0 m), with raised rail meshes |
| Balls | drawn circles | `Sphere` mesh + `Rigidbody` + `SphereCollider`, rolling on the table |
| Physics | hand-rolled 2D integrator | Unity physics constrained to the table plane (see §5) |
| Picture | Voronoi mosaic recolored per-pixel | Texture on the table material, painted into a `RenderTexture` (see §6) |
| Aiming | pointer drag on canvas | screen drag → world ray onto cue ball; trajectory preview line |
| Camera | implicit top-down | `Camera` angled above the table, slight perspective |

Keep the game **on a single horizontal plane** (XZ), gravity only holding balls to
the table. This is "top-down pool in 3D," not a 3D-volume game.

---

## 3. Core gameplay loop

1. **Aim** — player presses on/near the cue ball and drags backward. A dotted
   trajectory line shows launch direction and power (longer drag = more power,
   capped). Input is locked while any ball is moving.
2. **Launch** — on release, apply impulse to the cue ball opposite the drag.
3. **Simulate** — balls roll, collide, bounce off rails, paint matching regions,
   and decay to rest via friction.
4. **Settle** — once all balls are below a stop-speed threshold, re-enable aiming.
5. **Repeat** until the picture is fully painted → **win screen** (shots taken),
   then "Play again" / "New picture".

Track and display: **% painted** (progress bar) and **shot count**.

---

## 4. Settings (validated in the prototype — replicate all)

Exposed via an in-game settings panel; applied on "Apply & New Game":

- **Picture** — which illustration to paint. Prototype ships two:
  - `heart` — abstract; uses whichever colors the player enabled.
  - `cactus` — green cactus, pink flower, blue sky, yellow desert (4 colors).
  - Adding more is cheap: a picture is just a map from a surface point to a
    color index (see §6).
- **White ball size** — cue ball radius, independent slider.
- **Colored balls size** — radius for all colored balls, independent slider.
- **Paint mode**:
  - `Regular` — rolling over any part of a matching region fills the **whole region** instantly.
  - `Professional` — the ball paints a **trail of its own width**; a region only
    counts as done once ~80% of its area is covered, then it snap-fills the rest.
- **Balls per color** — count 0–5 for each of the 10 colors. The table is generated
  with exactly the requested balls; colors set to 0 are excluded. A picture only
  spawns balls for the colors it actually contains (each present color gets ≥1 ball
  so every level is winnable). Example: 3 red + 3 blue + 3 green → a 3-color picture
  with 9 colored balls + 1 cue.

---

## 5. Physics (Unity)

Use Unity's built-in physics but constrain to the table plane:

- Each ball: `Rigidbody` (`useGravity = true` or a custom downforce), `SphereCollider`,
  `PhysicMaterial` with **high bounciness** (rails ~0.85, ball–ball ~0.95 from the
  prototype) and **low friction**, plus **angular + Y-position freeze** is *not*
  ideal — instead let them roll but keep them on the plane via a flat floor collider.
- Rails: static box colliders forming the table border.
- **Friction / roll decay**: the prototype uses a per-frame velocity damping of
  `0.988`. In Unity, approximate with `Rigidbody.linearDamping` (start ~0.4–0.6 and
  tune) or a manual `velocity *= pow(0.988, dt*60)` in `FixedUpdate`.
- **Stop threshold**: when all balls' speed < ~0.05 (prototype units; scale to world),
  zero them out and hand control back to the player.
- **Ball sizes differ** (cue vs colored), so collision response must use each ball's
  own radius/mass — Unity handles this natively via collider radius + `Rigidbody.mass`.
  Scale mass with volume so a big cue convincingly scatters small balls.
- **Launch impulse**: `power = clamp(dragLength * k, 0, MAX)`, applied as
  `rb.AddForce(dir * power, ForceMode.Impulse)`. Tune `k` and `MAX` against the
  prototype's feel (`MAX_SHOT = 24`, `power = len * 0.12`).

Recommended: run physics in `FixedUpdate`, sample paint coverage there too (see §6).

---

## 6. Painting system (the core technical piece)

This is the highest-risk system; design it carefully.

**Region model.** Every picture provides two things:
1. A **region-ID map** — which region each surface point belongs to.
2. A **region → color number** mapping — what color each region must be painted.

Two viable implementations:

- **(A) Texture-mask approach (recommended).** Author/generate two textures sized to
  the table:
  - `RegionIdTex`: each region a unique flat color (R/G channels encode an integer
    region id). Sampled on the CPU to know which region a world point hits.
  - `RegionColorLUT`: region id → required color index (a small array, not a texture).
  - A runtime **`PaintRT` RenderTexture** (the visible paint layer), initially clear.
  The table material composites: base outline/numbers texture + `PaintRT` on top.

- **(B) Mesh-per-region approach.** Each region is its own mesh with a material whose
  color is revealed when painted. Simpler painting (just toggle a material/animate a
  fill) but heavier authoring and no partial "trail" coverage. Use only if (A) proves
  too costly on low-end devices.

**Painting at runtime (approach A):**
- Each `FixedUpdate`, for every moving colored ball, take its table-space position
  (and the segment from its previous position, to avoid gaps at speed).
- Look up the region id under the ball via `RegionIdTex`. If
  `RegionColorLUT[regionId] == ball.colorIndex` and the region isn't already complete:
  - **Regular mode**: mark the whole region complete; blit its full area into `PaintRT`
    in the region's color.
  - **Professional mode**: stamp a **disc of the ball's radius** (clipped to that
    region only) into `PaintRT` along the travel segment. Track covered-area per region;
    when coverage ≥ 80%, snap-fill the remainder.
- Maintain a per-region covered-pixel counter and a global painted-area total to drive
  the **% progress** and the **win check** (all regions complete).
- Clipping "paint only inside the matching region" is done by testing each stamped
  texel's region id against the ball's color (CPU stamp into a `Color32[]` then upload,
  or a GPU shader that samples `RegionIdTex` and discards mismatches).

**Numbers & outlines.** Render region outlines and the faint color-number labels in
the base texture (or as a separate overlay). Hide a region's number once it's complete.

**Pictures as functions.** Mirror the prototype: a picture is defined by a function
`f(u, v) -> colorIndex` over the table's UV space (e.g. `cactusFn`, `heartFn`). At level
build time, rasterize `f` into `RegionIdTex` + `RegionColorLUT`. This makes adding new
pictures cheap and is the path to later swapping in hand-authored line-art (where each
region in the art has a known fill color).

---

## 7. Suggested project structure

```
color-pool-3d/
  Assets/
    Scenes/Game.unity
    Scripts/
      GameManager.cs        // state machine: Aiming → Simulating → Settled → Win
      TableSetup.cs         // builds table, rails, spawns balls from settings
      Ball.cs               // colorIndex, radius, prevPos; cue vs colored flag
      CueAimController.cs    // drag input, trajectory preview, launch impulse
      PaintSurface.cs        // RegionIdTex/PaintRT, stamp + region coverage + win check
      PictureLibrary.cs      // picture functions (heart, cactus) → region build
      Settings.cs            // ball sizes, paint mode, balls-per-color, picture
      UIController.cs        // progress bar, shot count, settings panel, win panel
    Materials/  Textures/  Prefabs/  (Ball, Rail, Table)
  CLAUDE.md
```

State machine in `GameManager`: lock input unless state == `Aiming`; transition to
`Settled` when `AllBallsStopped()`; to `Win` when `PaintSurface.IsComplete`.

---

## 8. Art / assets (hyper-casual, cheap tier)

- **Balls**: a single sphere prefab, instanced with per-ball color material + a number
  decal/texture. Glossy, bright, soft shadow. Cue = white.
- **Table**: flat quad with the paint material; wooden/colored rails. Soft ambient
  + one directional light; subtle ball shadows for depth.
- **Camera**: angled above table, slight perspective, framed so the whole picture is
  readable. Portrait orientation (mobile).
- **UI**: progress bar, shot counter, color legend (number ↔ swatch), settings panel,
  win panel. Match the prototype's layout.
- No characters/animation needed → asset cost is near-zero; Synty/Kenney props
  optional for theming the table surround.

---

## 9. Tuning constants (start from prototype values)

| Meaning | Prototype value |
|---|---|
| Velocity damping / friction | `0.988` per frame @60fps |
| Rail restitution | `0.85` |
| Ball–ball restitution | `0.95` |
| Stop speed threshold | `0.05` |
| Max shot power | `24` |
| Drag→power factor | `0.12` |
| Pro-mode region completion | `80%` coverage |
| Default ball radius | `26 px` (≈ scale to ~0.26 m on a 9.6 m table) |
| Colors | 10 (palette + names in prototype `PALETTE`/`NAMES`) |

Re-tune in world units; preserve the **ratios and feel**, not the raw numbers.

---

## 10. Build order (suggested)

1. Table + rails + one cue ball + drag-launch + camera. Get the **shot feel** right
   first (this is 90% of whether the game is fun).
2. Spawn colored balls from a hardcoded count; ball–ball + rail collisions; settle/aim loop.
3. `PaintSurface` with a single test picture in **Regular** mode (whole-region fill);
   wire up % progress + win.
4. **Professional** trail painting + 80% snap-fill.
5. Settings panel: ball sizes, paint mode, balls-per-color, picture selector.
6. Picture library (heart, cactus) via picture functions; win celebration + polish.

(Magnet mode and a power-up/upgrade economy are deferred — do not implement yet.)
```
