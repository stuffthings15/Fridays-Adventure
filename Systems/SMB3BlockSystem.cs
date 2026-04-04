// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 – Multi-Team Implementation
// Systems/SMB3BlockSystem.cs
// Purpose: Interactive SMB3-style Question Blocks, Brick Blocks, and coin arcs.
// ────────────────────────────────────────────────────────────────────────────
// Team 4  (Lead Game Designer)  – Idea 1:  QuestionBlock (hit from below = coin/item)
// Team 4  (Lead Game Designer)  – Idea 2:  BrickBlock (powered player breaks it)
// Team 4  (Lead Game Designer)  – Idea 3:  HiddenBlock (invisible until hit)
// Team 4  (Lead Game Designer)  – Idea 4:  MultiCoinBlock (taps out after 5 coins)
// Team 4  (Lead Game Designer)  – Idea 5:  BlockManager coordinator for scenes
// Team 7  (Gameplay Programmer) – Idea 1:  Ceiling bump: player head stops on block
// Team 7  (Gameplay Programmer) – Idea 2:  Block-hit state change on ceiling contact
// Team 7  (Gameplay Programmer) – Idea 3:  Brick break only when powered (FireFlower/Leaf)
// Team 7  (Gameplay Programmer) – Idea 4:  Coin arc gravity physics (parabola)
// Team 14 (Environment Artist)  – Idea 1:  Question block yellow/orange visual with '?'
// Team 14 (Environment Artist)  – Idea 2:  Brick block orange grid pattern
// Team 14 (Environment Artist)  – Idea 3:  Used block (gray) after contents spent
// Team 14 (Environment Artist)  – Idea 4:  Block bump Y-offset animation
// Team 17 (VFX Artist)          – Idea 1:  Coin arc sparkle trail particles
// Team 17 (VFX Artist)          – Idea 2:  Brick break fragment particles (4-dir)
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Systems
{
    // ── Block type enum ────────────────────────────────────────────────────────
    /// <summary>
    /// What the block contains when hit from below.
    /// Team 4 (Lead Game Designer) — content type for block design.
    /// </summary>
    public enum BlockContent
    {
        Coin,       // single coin per hit (MultiCoin variant taps several)
        MultiCoin,  // releases 5 coins on rapid hits before turning gray
        Mushroom,   // spawns a Super Mushroom item
        FireFlower, // spawns a Fire Flower item
        Leaf,       // spawns a Super Leaf item
        Star,       // spawns a Star invincibility item
    }

    // ══════════════════════════════════════════════════════════════════════════
    // QuestionBlock
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Classic SMB3 '?' block.  Hit from below by the player to reveal contents.
    /// Contents are delivered via <see cref="BlockManager.OnBlockHit"/>.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 1: QuestionBlock entity.
    /// Team 7  (Gameplay Programmer) — Idea 1: ceiling bump stops player upward velocity.
    /// Team 14 (Environment Artist)  — Idea 1 &amp; 4: yellow visual + bump animation.
    /// Team 17 (VFX Artist)          — Idea 1: coin sparkle on hit.
    /// </summary>
    public sealed class QuestionBlock
    {
        // ── Geometry ──────────────────────────────────────────────────────────
        public float X        { get; }
        public float Y        { get; }
        public int   Width    { get; } = 32;
        public int   Height   { get; } = 32;

        public Rectangle Hitbox => new Rectangle((int)X, (int)Y, Width, Height);

        // ── State ─────────────────────────────────────────────────────────────
        /// <summary>Block content type.</summary>
        public BlockContent Content { get; }

        /// <summary>
        /// True once the contents have been released; block renders as gray.
        /// Team 4 (Lead Game Designer) — Idea 1.
        /// </summary>
        public bool IsUsed { get; private set; }

        /// <summary>True while the block is hidden (invisible until struck).</summary>
        public bool IsHidden { get; private set; }

        // ── Multi-coin counter (Team 4 — Idea 4) ─────────────────────────────
        private int _coinsRemaining;
        private const int MultiCoinMax = 5;

        // ── Bump animation (Team 14 — Idea 4) ────────────────────────────────
        private float _bumpTimer;
        private const float BumpDuration = 0.18f;

        /// <summary>Visual Y offset during bump animation (negative = upward).</summary>
        public float BumpOffset { get; private set; }

        // ── Blink (hidden block discovery) ───────────────────────────────────
        private float _blinkTimer;

        /// <summary>
        /// Constructs a QuestionBlock at world coordinates.
        /// </summary>
        /// <param name="x">World-space left edge.</param>
        /// <param name="y">World-space top edge.</param>
        /// <param name="content">What the block contains.</param>
        /// <param name="hidden">True = invisible until hit (Team 4 — Idea 3).</param>
        public QuestionBlock(float x, float y, BlockContent content = BlockContent.Coin, bool hidden = false)
        {
            X          = x;
            Y          = y;
            Content    = content;
            IsHidden   = hidden;
            _coinsRemaining = content == BlockContent.MultiCoin ? MultiCoinMax : 1;
        }

        // ── Update ─────────────────────────────────────────────────────────────
        /// <summary>Advances animation timers.</summary>
        public void Update(float dt)
        {
            // Bump animation (Team 14 — Idea 4)
            if (_bumpTimer > 0f)
            {
                _bumpTimer -= dt;
                float t = 1f - (_bumpTimer / BumpDuration);
                // Sine arc: up then return
                BumpOffset = -(float)Math.Sin(t * Math.PI) * 8f;
                if (_bumpTimer <= 0f) BumpOffset = 0f;
            }

            // Blink timer for hidden-block discovery
            if (_blinkTimer > 0f)
            {
                _blinkTimer -= dt;
                if (_blinkTimer <= 0f) IsHidden = false;
            }
        }

        // ── Hit detection (Team 7 — Idea 1 &amp; 2) ────────────────────────────
        /// <summary>
        /// Checks whether <paramref name="player"/> is hitting this block from below.
        /// If so, applies ceiling bump physics and triggers the block if not used.
        /// Returns true when the block was struck this frame.
        /// Team 7 (Gameplay Programmer) — Idea 1: ceiling bump.
        /// </summary>
        public bool CheckPlayerHitFromBelow(Player player)
        {
            if (IsUsed && Content != BlockContent.MultiCoin) return false;
            if (IsUsed && _coinsRemaining <= 0)               return false;

            // Player must be moving upward and have their top edge just below block bottom.
            if (player.VelocityY >= 0f) return false;

            Rectangle pb = new Rectangle((int)player.X, (int)player.Y, player.Width, player.Height);
            Rectangle bb = new Rectangle((int)X, (int)(Y + BumpOffset), Width, Height + 4);

            // Horizontal overlap
            if (pb.Right < bb.Left + 4 || pb.Left > bb.Right - 4) return false;

            // Vertical: player head (top) must be within 8px of block bottom
            float headY = player.Y;
            float blockBot = Y + BumpOffset + Height;
            if (headY > blockBot || headY < blockBot - 14f) return false;

            // Stop upward velocity (ceiling bounce-back)
            player.VelocityY = 80f;   // small downward nudge after head-bump

            if (!IsHidden) Trigger(player);
            else { _blinkTimer = 0.3f; IsHidden = false; Trigger(player); }

            return true;
        }

        // ── Trigger ────────────────────────────────────────────────────────────
        /// <summary>
        /// Activates the block: releases contents and starts bump animation.
        /// Team 4 (Lead Game Designer) — Idea 1 &amp; 4.
        /// Team 17 (VFX Artist) — Idea 1: coin sparkle.
        /// </summary>
        private void Trigger(Player player)
        {
            _bumpTimer = BumpDuration;

            if (Content == BlockContent.MultiCoin)
            {
                // Team 4 — Idea 4: multi-coin block taps out
                if (_coinsRemaining > 0)
                {
                    _coinsRemaining--;
                    Game.Instance.AddCoins(1);
                    Game.Instance.FloatingText.Spawn("+1",
                        (int)(X + Width / 2f), (int)Y - 10, Color.Gold);
                    ParticleSystem.SpawnCoinSparkle(X + Width / 2f, Y);

                    if (_coinsRemaining <= 0) IsUsed = true;
                }
                return;
            }

            if (IsUsed) return;
            IsUsed = true;

            // Coin sparkle VFX (Team 17 — Idea 1)
            ParticleSystem.SpawnCoinSparkle(X + Width / 2f, Y);

            // Notify manager (or caller) via callback stored in BlockManager
            BlockManager.NotifyBlockHit(this, player);
        }

        // ── Draw (Team 14 — Ideas 1–4) ─────────────────────────────────────────
        /// <summary>
        /// Draws the block at its world position offset by <paramref name="cameraX"/>.
        /// Team 14 (Environment Artist) — visual rendering.
        /// </summary>
        public void Draw(Graphics g, float cameraX)
        {
            if (IsHidden) return;  // hidden blocks are invisible

            float sx = X - cameraX;
            float sy = Y + BumpOffset;
            var r = new RectangleF(sx, sy, Width, Height);

            if (IsUsed)
            {
                // Used / gray block (Team 14 — Idea 3)
                using (var br = new SolidBrush(Color.FromArgb(140, 130, 120)))
                    g.FillRectangle(br, r);
                using (var pen = new Pen(Color.FromArgb(100, 100, 90), 1))
                    g.DrawRectangle(pen, sx, sy, Width - 1, Height - 1);
            }
            else
            {
                // Yellow '?' block (Team 14 — Idea 1)
                using (var br = new LinearGradientBrush(r,
                    Color.FromArgb(255, 220, 60), Color.FromArgb(220, 160, 20), 90f))
                    g.FillRectangle(br, r);

                // Orange border
                using (var pen = new Pen(Color.FromArgb(200, 120, 0), 2))
                    g.DrawRectangle(pen, sx + 1, sy + 1, Width - 3, Height - 3);

                // Inner bevel highlight
                using (var br = new SolidBrush(Color.FromArgb(100, 255, 255, 200)))
                    g.FillRectangle(br, sx + 2, sy + 2, Width - 4, 5);

                // '?' character
                using (var f = new Font("Courier New", 14, FontStyle.Bold))
                {
                    var sz = g.MeasureString("?", f);
                    g.DrawString("?", f, Brushes.White,
                        sx + (Width  - sz.Width)  / 2f,
                        sy + (Height - sz.Height) / 2f);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BrickBlock
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// SMB3 breakable brick block.  A small/unpowered player bounces off it;
    /// a powered-up (Mushroom/FireFlower/Leaf) player smashes it for score.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 2: BrickBlock entity.
    /// Team 7  (Gameplay Programmer) — Idea 3: break only when powered.
    /// Team 14 (Environment Artist)  — Idea 2 &amp; 4: orange grid visual + bump.
    /// Team 17 (VFX Artist)          — Idea 2: brick break fragment particles.
    /// </summary>
    public sealed class BrickBlock
    {
        public float X      { get; }
        public float Y      { get; }
        public int   Width  { get; } = 32;
        public int   Height { get; } = 32;

        public Rectangle Hitbox => new Rectangle((int)X, (int)Y, Width, Height);

        /// <summary>True once the block has been broken.</summary>
        public bool IsBroken { get; private set; }

        // ── Bump animation ────────────────────────────────────────────────────
        private float _bumpTimer;
        private const float BumpDuration = 0.14f;
        public float BumpOffset { get; private set; }

        public BrickBlock(float x, float y)
        {
            X = x;
            Y = y;
        }

        public void Update(float dt)
        {
            if (_bumpTimer > 0f)
            {
                _bumpTimer -= dt;
                float t = 1f - (_bumpTimer / BumpDuration);
                BumpOffset = -(float)Math.Sin(t * Math.PI) * 6f;
                if (_bumpTimer <= 0f) BumpOffset = 0f;
            }
        }

        /// <summary>
        /// Checks if <paramref name="player"/> hits this block from below.
        /// Powered player breaks it; unpowered player bounces off.
        /// Team 7 (Gameplay Programmer) — Idea 3.
        /// </summary>
        public bool CheckPlayerHitFromBelow(Player player)
        {
            if (IsBroken || player.VelocityY >= 0f) return false;

            Rectangle pb = new Rectangle((int)player.X, (int)player.Y, player.Width, player.Height);
            Rectangle bb = new Rectangle((int)X, (int)(Y + BumpOffset), Width, Height + 4);

            if (pb.Right < bb.Left + 4 || pb.Left > bb.Right - 4) return false;

            float headY   = player.Y;
            float blockBot = Y + BumpOffset + Height;
            if (headY > blockBot || headY < blockBot - 14f) return false;

            player.VelocityY = 80f;

            bool isPowered = PowerUpInventory.ActiveSuit == SuitType.Mushroom ||
                             PowerUpInventory.ActiveSuit == SuitType.FireFlower ||
                             PowerUpInventory.ActiveSuit == SuitType.Leaf;

            if (isPowered)
            {
                // Break the brick (Team 7 — Idea 3)
                IsBroken = true;
                Game.Instance.PlayerBounty += 50;
                Game.Instance.FloatingText.Spawn("+50",
                    (int)(X + Width / 2f), (int)Y - 10, Color.OrangeRed);

                // Fragment particles (Team 17 — Idea 2)
                SpawnBrickFragments();
            }
            else
            {
                // Unpowered: just bump
                _bumpTimer = BumpDuration;
            }

            return true;
        }

        /// <summary>
        /// Spawns 4-direction brick fragment particles.
        /// Team 17 (VFX Artist) — Idea 2.
        /// </summary>
        private void SpawnBrickFragments()
        {
            // Use ParticleSystem's burst in 4 diagonal directions
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            ParticleSystem.SpawnBurst(cx, cy, 8,
                Color.FromArgb(200, 120, 40), 80f, 200f, 0.8f, 1.4f);
        }

        /// <summary>
        /// Draws the brick block.
        /// Team 14 (Environment Artist) — Idea 2 &amp; 4.
        /// </summary>
        public void Draw(Graphics g, float cameraX)
        {
            if (IsBroken) return;

            float sx = X - cameraX;
            float sy = Y + BumpOffset;

            // Brick fill
            using (var br = new SolidBrush(Color.FromArgb(200, 110, 40)))
                g.FillRectangle(br, sx, sy, Width, Height);

            // Mortar lines (orange brick grid — Team 14 Idea 2)
            using (var pen = new Pen(Color.FromArgb(140, 70, 20), 1))
            {
                // Horizontal mortar at half height
                g.DrawLine(pen, sx, sy + Height / 2f, sx + Width, sy + Height / 2f);
                // Vertical mortar — staggered top / bottom halves
                g.DrawLine(pen, sx + Width / 4f, sy, sx + Width / 4f, sy + Height / 2f);
                g.DrawLine(pen, sx + 3 * Width / 4f, sy, sx + 3 * Width / 4f, sy + Height / 2f);
                g.DrawLine(pen, sx + Width / 2f, sy + Height / 2f, sx + Width / 2f, sy + Height);
            }

            // Top highlight
            using (var br = new SolidBrush(Color.FromArgb(80, 255, 200, 100)))
                g.FillRectangle(br, sx, sy, Width, 3);

            // Outer border
            using (var pen = new Pen(Color.FromArgb(140, 60, 10), 1))
                g.DrawRectangle(pen, sx, sy, Width - 1, Height - 1);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FlyingCoin  (coin arc animation)
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Animated coin that arcs upward from a block and falls back down.
    /// Pure visual — actual coin counting is handled by BlockManager.
    ///
    /// Team 7  (Gameplay Programmer) — Idea 4: coin arc gravity physics.
    /// Team 17 (VFX Artist)          — Idea 1: coin sparkle trail.
    /// </summary>
    public sealed class FlyingCoin
    {
        public float X     { get; private set; }
        public float Y     { get; private set; }
        private float _vy;
        private float _life = 1.2f;     // seconds until despawn
        private float _rot;

        /// <summary>True once the coin has finished its arc.</summary>
        public bool IsDead => _life <= 0f;

        public FlyingCoin(float x, float y)
        {
            X   = x;
            Y   = y;
            _vy = -280f;   // initial upward velocity
        }

        public void Update(float dt)
        {
            _life -= dt;
            _vy   += 380f * dt;   // gravity (Team 7 — Idea 4)
            Y     += _vy * dt;
            _rot  += 360f * dt;   // spin

            // Coin sparkle trail every ~0.05 s using a simple modulus approximation
            if ((int)(_life * 20) % 3 == 0)
                ParticleSystem.SpawnCoinSparkle(X, Y);
        }

        /// <summary>
        /// Draws the spinning coin disc.
        /// Team 14 (Environment Artist) — Idea 1: coin visual.
        /// </summary>
        public void Draw(Graphics g, float cameraX)
        {
            if (IsDead) return;
            float sx = X - cameraX;
            // Squash width for spin illusion
            float w = Math.Abs((float)Math.Cos(_rot * Math.PI / 180f)) * 14f + 2f;

            using (var br = new SolidBrush(Color.Gold))
                g.FillEllipse(br, sx - w / 2f, Y - 7, w, 14);
            using (var br = new SolidBrush(Color.FromArgb(180, 255, 255, 100)))
                g.FillEllipse(br, sx - w / 2f + 1, Y - 5, w * 0.4f, 4);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BlockManager
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Central coordinator for all interactive blocks in a level.
    /// Scenes add blocks via <see cref="QuestionBlocks"/> / <see cref="BrickBlocks"/>,
    /// call <see cref="Update"/> and <see cref="Draw"/> each frame, and receive
    /// item-spawn requests via <see cref="OnItemSpawned"/>.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 5: coordinator.
    /// Team 7  (Gameplay Programmer) — Idea 1–4: physics integration.
    /// </summary>
    public static class BlockManager
    {
        // ── Block collections ─────────────────────────────────────────────────
        /// <summary>All question blocks in the current level.</summary>
        public static readonly List<QuestionBlock> QuestionBlocks = new List<QuestionBlock>();

        /// <summary>All brick blocks in the current level.</summary>
        public static readonly List<BrickBlock>    BrickBlocks    = new List<BrickBlock>();

        /// <summary>Active coin arc animations.</summary>
        public static readonly List<FlyingCoin>    FlyingCoins    = new List<FlyingCoin>();

        // ── Event callback ────────────────────────────────────────────────────
        /// <summary>
        /// Called when a QuestionBlock is struck and releases an item.
        /// Scene can hook this to spawn a PowerUp entity at the given position.
        /// Team 4 (Lead Game Designer) — Idea 5.
        /// </summary>
        public static Action<QuestionBlock, Player> OnItemSpawned;

        // ── Notification from QuestionBlock ───────────────────────────────────
        internal static void NotifyBlockHit(QuestionBlock block, Player player)
        {
            // Spawn a flying coin for Coin content
            if (block.Content == BlockContent.Coin)
            {
                FlyingCoins.Add(new FlyingCoin(block.X + block.Width / 2f, block.Y));
                Game.Instance.AddCoins(1);
                Game.Instance.FloatingText.Spawn("+1 COIN",
                    (int)(block.X + block.Width / 2f), (int)block.Y - 16, Color.Gold);
            }
            else
            {
                // Item block — apply power-up directly
                SuitType suit = BlockContentToSuit(block.Content);
                if (suit != SuitType.None)
                {
                    // If player already has a suit, put it in reserve
                    if (PowerUpInventory.ActiveSuit == SuitType.None)
                        PowerUpInventory.ApplySuit(suit);
                    else
                        PowerUpInventory.SetReserve(suit);

                    string label = BlockContentName(block.Content);
                    Game.Instance.FloatingText.Spawn(label,
                        (int)(block.X + block.Width / 2f), (int)block.Y - 20,
                        Color.LimeGreen, large: true);
                }

                // Notify scene for custom item spawning
                OnItemSpawned?.Invoke(block, player);
            }
        }

        private static SuitType BlockContentToSuit(BlockContent c)
        {
            switch (c)
            {
                case BlockContent.Mushroom:   return SuitType.Mushroom;
                case BlockContent.FireFlower: return SuitType.FireFlower;
                case BlockContent.Leaf:       return SuitType.Leaf;
                case BlockContent.Star:       return SuitType.Star;
                default:                      return SuitType.None;
            }
        }

        private static string BlockContentName(BlockContent c)
        {
            switch (c)
            {
                case BlockContent.Mushroom:   return "SUPER!";
                case BlockContent.FireFlower: return "FIRE!";
                case BlockContent.Leaf:       return "TANOOKI!";
                case BlockContent.Star:       return "STARMAN!";
                default:                      return "+COIN";
            }
        }

        // ── Update ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Advances block animations, coin arcs, and checks player hit detection.
        /// Call once per frame from the gameplay scene.
        /// Team 7 (Gameplay Programmer) — physics integration.
        /// </summary>
        public static void Update(float dt, Player player)
        {
            foreach (var qb in QuestionBlocks)
            {
                qb.Update(dt);
                if (player != null) qb.CheckPlayerHitFromBelow(player);
            }
            foreach (var bb in BrickBlocks)
            {
                bb.Update(dt);
                if (player != null) bb.CheckPlayerHitFromBelow(player);
            }
            for (int i = FlyingCoins.Count - 1; i >= 0; i--)
            {
                FlyingCoins[i].Update(dt);
                if (FlyingCoins[i].IsDead) FlyingCoins.RemoveAt(i);
            }
        }

        // ── Hitbox list for collision ──────────────────────────────────────────
        /// <summary>
        /// Returns the combined solid hitboxes of all active (non-broken, non-used) blocks.
        /// Use these in <c>ResolveHorizontal</c> / <c>ResolveVertical</c>.
        /// Team 7 (Gameplay Programmer) — platform collision source.
        /// </summary>
        public static List<Rectangle> GetSolidHitboxes()
        {
            var list = new List<Rectangle>();
            foreach (var qb in QuestionBlocks)
                list.Add(qb.Hitbox);
            foreach (var bb in BrickBlocks)
                if (!bb.IsBroken) list.Add(bb.Hitbox);
            return list;
        }

        // ── Draw ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Draws all blocks and flying coins.
        /// Call from the scene's Draw() inside the camera transform.
        /// Team 14 (Environment Artist).
        /// </summary>
        public static void Draw(Graphics g, float cameraX)
        {
            foreach (var bb in BrickBlocks)  bb.Draw(g, cameraX);
            foreach (var qb in QuestionBlocks) qb.Draw(g, cameraX);
            foreach (var fc in FlyingCoins)    fc.Draw(g, cameraX);
        }

        // ── Reset ─────────────────────────────────────────────────────────────
        /// <summary>Clears all blocks and coin arcs (call on level load/exit).</summary>
        public static void Reset()
        {
            QuestionBlocks.Clear();
            BrickBlocks.Clear();
            FlyingCoins.Clear();
            OnItemSpawned = null;
        }
    }
}
