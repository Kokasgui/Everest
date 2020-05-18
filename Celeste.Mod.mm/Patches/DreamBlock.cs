﻿#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using Celeste.Mod;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Xna.Framework;
using System.Collections;

namespace Celeste {
    class patch_DreamBlock : DreamBlock {
        private bool playerHasDreamDash;
        private LightOcclude occlude;
        private float whiteHeight;
        private float whiteFill;
        private Shaker shaker;
        private Vector2 shake;

        public patch_DreamBlock(EntityData data, Vector2 offset)
            : base(data, offset) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

        [MonoModLinkTo("Celeste.DreamBlock", "System.Void .ctor(Microsoft.Xna.Framework.Vector2,System.Single,System.Single,System.Nullable`1<Microsoft.Xna.Framework.Vector2>,System.Boolean,System.Boolean,System.Boolean)")]
        [MonoModForceCall]
        [MonoModRemove]
        public extern void ctor(Vector2 position, float width, float height, Vector2? node, bool fastMoving, bool oneUse, bool below);
        [MonoModConstructor]
        public void ctor(Vector2 position, float width, float height, Vector2? node, bool fastMoving, bool oneUse) {
            ctor(position, width, height, node, fastMoving, oneUse, false);
        }

        public void DeactivateNoRoutine() {
            if (playerHasDreamDash) {
                playerHasDreamDash = false;
                Setup();
                if (occlude == null) {
                    occlude = new LightOcclude(1f);
                }
                Add(occlude);
                whiteHeight = 1f;
                whiteFill = 0f;
                if (shaker != null) {
                    shaker.On = false;
                }
                SurfaceSoundIndex = 11;
            }
        }

        public IEnumerator Deactivate() {
            Level level = SceneAs<Level>();
            yield return 1f;
            Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
            if (shaker == null) {
                shaker = new Shaker(true, delegate (Vector2 t) {
                    shake = t;
                });
            }
            Add(shaker);
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float alpha = 0f; alpha < 1f; alpha += Engine.DeltaTime) {
                whiteFill = Ease.CubeIn(alpha);
                yield return null;
            }
            shaker.On = false;
            yield return 0.5f;
            DeactivateNoRoutine();
            whiteHeight = 1f;
            whiteFill = 1f;
            for (float yOffset = 1f; yOffset > 0f; yOffset -= Engine.DeltaTime * 0.5f) {
                whiteHeight = yOffset;
                if (level.OnInterval(0.1f)) {
                    for (int xOffset = 0; xOffset < Width; xOffset += 4) {
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, new Vector2(X + xOffset, Y + Height * whiteHeight + 1f));
                    }
                }
                if (level.OnInterval(0.1f)) {
                    level.Shake(0.3f);
                }
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                yield return null;
            }
            while (whiteFill > 0f) {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
        }
    }
}
