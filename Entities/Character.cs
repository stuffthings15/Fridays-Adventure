using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Abilities;

namespace Fridays_Adventure.Entities
{
    [Flags]
    public enum StatusEffect
    {
        None       = 0,
        Frozen     = 1,
        Burning    = 2,
        Suppressed = 4,
        Sinking    = 8,
        Stunned    = 16,
        Dodging    = 32
    }

    public class Character : Entity
    {
        public int  MaxHealth { get; set; }
        private int _health;
        public int Health
        {
            get => _health;
            set => _health = Math.Max(0, Math.Min(MaxHealth, value));
        }
        public bool IsAlive => Health > 0;

        public bool  CannotSwim  { get; protected set; }
        public bool  IsGrounded  { get; set; }
        public float MoveSpeed   { get; set; } = 180f;
        public float JumpForce   { get; set; } = -440f;
        public const float Gravity = 860f;

        public int   AttackDamage { get; set; } = 10;
        public bool  IsAttacking  { get; set; }
        private float _attackTimer;
        private const float AttackDuration = 0.22f;
        public float AttackCooldown    { get; set; }
        private const float AttackCooldownMax = 0.45f;

        public float DodgeCooldown { get; private set; }
        private const float DodgeCooldownMax = 0.9f;

        public float InvincibilityTimer { get; protected set; }
        public bool  IsInvincible => InvincibilityTimer > 0;

        public StatusEffect ActiveEffects { get; private set; } = StatusEffect.None;
        private readonly Dictionary<StatusEffect, float> _effectTimers = new Dictionary<StatusEffect, float>();

        public List<Ability> Abilities { get; } = new List<Ability>();

        public Character(float x, float y, int w, int h, int maxHp) : base(x, y, w, h)
        {
            MaxHealth = maxHp;
            _health   = maxHp;
        }

        public bool HasEffect(StatusEffect e) => (ActiveEffects & e) != 0;

        public void ApplyEffect(StatusEffect e, float duration)
        {
            ActiveEffects |= e;
            _effectTimers[e] = duration;
        }

        public void RemoveEffect(StatusEffect e)
        {
            ActiveEffects &= ~e;
            _effectTimers.Remove(e);
        }

        public virtual void TakeDamage(int amount)
        {
            if (IsInvincible || HasEffect(StatusEffect.Dodging)) return;
            Health -= amount;
            InvincibilityTimer = 0.5f;
        }

        /// <summary>
        /// Grants invincibility for the specified duration (used by power-ups and checkpoints).
        /// </summary>
        public void GrantInvincibility(float duration)
        {
            InvincibilityTimer = Math.Max(InvincibilityTimer, duration);
        }

        public bool TryAttack()
        {
            if (AttackCooldown > 0 || IsAttacking) return false;
            IsAttacking  = true;
            _attackTimer = AttackDuration;
            AttackCooldown = AttackCooldownMax;

            // Diagnostic logging for attack cooldown issues
            System.Diagnostics.Debug.WriteLine($"[ATTACK] Fired at AttackCooldown={AttackCooldown:F2}s, Duration={AttackDuration:F2}s, Cooldown={AttackCooldownMax:F2}s");

            return true;
        }

        public bool TryDodge()
        {
            if (DodgeCooldown > 0 || HasEffect(StatusEffect.Dodging)) return false;
            ApplyEffect(StatusEffect.Dodging, 0.28f);
            DodgeCooldown = DodgeCooldownMax;
            return true;
        }

        public override void Update(float dt)
        {
            TickEffects(dt);
            TickCooldowns(dt);
            if (!IsGrounded)
                VelocityY += Gravity * dt;
            else
                VelocityY = 80f;   // constant push keeps player overlapping platform each frame
        }

        private void TickEffects(float dt)
        {
            if (InvincibilityTimer > 0) InvincibilityTimer -= dt;
            var keys = new List<StatusEffect>(_effectTimers.Keys);
            foreach (var e in keys)
            {
                _effectTimers[e] -= dt;
                if (_effectTimers[e] <= 0)
                    RemoveEffect(e);
            }
        }

        private void TickCooldowns(float dt)
        {
            if (AttackCooldown > 0) AttackCooldown -= dt;
            if (DodgeCooldown  > 0) DodgeCooldown  -= dt;
            if (IsAttacking)
            {
                _attackTimer -= dt;
                if (_attackTimer <= 0) IsAttacking = false;
            }
            foreach (var a in Abilities)
                a.TickCooldown(dt);
        }

        protected void DrawHealthBar(Graphics g)
        {
            int bx = (int)X, by = (int)Y - 9, bw = Width;
            float pct = (float)Health / MaxHealth;
            g.FillRectangle(Brushes.DarkRed, bx, by, bw, 5);
            using (var br = new SolidBrush(Color.LimeGreen))
                g.FillRectangle(br, bx, by, (int)(bw * pct), 5);
        }
    }
}
