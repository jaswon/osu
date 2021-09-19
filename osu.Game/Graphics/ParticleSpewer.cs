﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Game.Graphics
{
    public class ParticleSpewer : Sprite
    {
        private readonly FallingParticle[] particles;
        private int currentIndex;
        private double lastParticleAdded;

        private readonly double cooldown;
        private readonly double maxDuration;

        /// <summary>
        /// Determines whether particles are being spawned.
        /// </summary>
        public readonly BindableBool Active = new BindableBool();

        public override bool IsPresent => base.IsPresent && hasActiveParticles;

        /// <summary>
        /// Called each time a new particle should be spawned.
        /// </summary>
        public Func<FallingParticle?> CreateParticle = () => new FallingParticle();

        public float ParticleGravity;

        private bool hasActiveParticles => Active.Value || (lastParticleAdded + maxDuration) > Time.Current;

        public ParticleSpewer(Texture texture, int perSecond, double maxDuration)
        {
            Texture = texture;
            Blending = BlendingParameters.Additive;

            particles = new FallingParticle[perSecond * (int)Math.Ceiling(maxDuration / 1000)];

            cooldown = 1000f / perSecond;
            this.maxDuration = maxDuration;
        }

        protected override void Update()
        {
            base.Update();

            // reset cooldown if the clock was rewound.
            // this can happen when seeking in replays.
            if (lastParticleAdded > Time.Current) lastParticleAdded = 0;

            if (Active.Value && Time.Current > lastParticleAdded + cooldown)
            {
                var newParticle = CreateParticle();

                if (newParticle.HasValue)
                {
                    var particle = newParticle.Value;
                    particle.StartTime = (float)Time.Current;

                    particles[currentIndex] = particle;

                    currentIndex = (currentIndex + 1) % particles.Length;
                    lastParticleAdded = Time.Current;
                }
            }

            Invalidate(Invalidation.DrawNode);
        }

        protected override DrawNode CreateDrawNode() => new ParticleSpewerDrawNode(this);

        # region DrawNode

        private class ParticleSpewerDrawNode : SpriteDrawNode
        {
            private readonly FallingParticle[] particles;

            protected new ParticleSpewer Source => (ParticleSpewer)base.Source;

            private readonly float maxDuration;

            private float currentTime;
            private float gravity;
            private Axes relativePositionAxes;
            private Vector2 sourceSize;

            public ParticleSpewerDrawNode(Sprite source)
                : base(source)
            {
                particles = new FallingParticle[Source.particles.Length];
                maxDuration = (float)Source.maxDuration;
            }

            public override void ApplyState()
            {
                base.ApplyState();

                Source.particles.CopyTo(particles, 0);

                currentTime = (float)Source.Time.Current;
                gravity = Source.ParticleGravity;
                relativePositionAxes = Source.RelativePositionAxes;
                sourceSize = Source.DrawSize;
            }

            protected override void Blit(Action<TexturedVertex2D> vertexAction)
            {
                foreach (var p in particles)
                {
                    // ignore particles that weren't initialized.
                    if (p.StartTime <= 0) continue;

                    var timeSinceStart = currentTime - p.StartTime;

                    // ignore particles from the future.
                    // these can appear when seeking in replays.
                    if (timeSinceStart < 0) continue;

                    var alpha = p.AlphaAtTime(timeSinceStart);
                    if (alpha <= 0) continue;

                    var pos = p.PositionAtTime(timeSinceStart, gravity, maxDuration);
                    var scale = p.ScaleAtTime(timeSinceStart);
                    var angle = p.AngleAtTime(timeSinceStart);

                    var rect = createDrawRect(pos, scale);

                    var quad = new Quad(
                        transformPosition(rect.TopLeft, rect.Centre, angle),
                        transformPosition(rect.TopRight, rect.Centre, angle),
                        transformPosition(rect.BottomLeft, rect.Centre, angle),
                        transformPosition(rect.BottomRight, rect.Centre, angle)
                    );

                    DrawQuad(Texture, quad, DrawColourInfo.Colour.MultiplyAlpha(alpha), null, vertexAction,
                        new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height),
                        null, TextureCoords);
                }
            }

            private RectangleF createDrawRect(Vector2 position, float scale)
            {
                var width = Texture.DisplayWidth * scale;
                var height = Texture.DisplayHeight * scale;

                if (relativePositionAxes.HasFlagFast(Axes.X))
                    position.X *= sourceSize.X;
                if (relativePositionAxes.HasFlagFast(Axes.Y))
                    position.Y *= sourceSize.Y;

                return new RectangleF(
                    position.X - width / 2,
                    position.Y - height / 2,
                    width,
                    height);
            }

            private Vector2 transformPosition(Vector2 pos, Vector2 centre, float angle)
            {
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);

                float x = centre.X + (pos.X - centre.X) * cos + (pos.Y - centre.Y) * sin;
                float y = centre.Y + (pos.Y - centre.Y) * cos - (pos.X - centre.X) * sin;

                return Vector2Extensions.Transform(new Vector2(x, y), DrawInfo.Matrix);
            }
        }

        #endregion

        public struct FallingParticle
        {
            public float StartTime;
            public Vector2 StartPosition;
            public Vector2 Velocity;
            public float Duration;
            public float StartAngle;
            public float EndAngle;
            public float EndScale;

            public float AlphaAtTime(float timeSinceStart) => 1 - progressAtTime(timeSinceStart);

            public float ScaleAtTime(float timeSinceStart) => 1 + (EndScale - 1) * progressAtTime(timeSinceStart);

            public float AngleAtTime(float timeSinceStart) => StartAngle + (EndAngle - StartAngle) * progressAtTime(timeSinceStart);

            public Vector2 PositionAtTime(float timeSinceStart, float gravity, float maxDuration)
            {
                var progress = progressAtTime(timeSinceStart);
                var currentGravity = new Vector2(0, gravity * Duration / maxDuration * progress);

                return StartPosition + (Velocity + currentGravity) * timeSinceStart / maxDuration;
            }

            private float progressAtTime(float timeSinceStart) => Math.Clamp(timeSinceStart / Duration, 0, 1);
        }
    }
}
