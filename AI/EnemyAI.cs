using System;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.AI
{
    public enum AIState { Patrol, Chase, Attack, Recover, Dead }

    public sealed class EnemyAI
    {
        private AIState _state = AIState.Patrol;
        public  AIState State  => _state;

        private readonly Enemy _owner;
        private float _patrolLeft, _patrolRight;
        private bool  _goingRight = true;
        private float _attackCooldown;
        private float _recoverTimer;
        private float _chaseTimeout;

        public float DetectionRange  { get; set; } = 210f;
        public float AttackRange     { get; set; } = 55f;
        public float AttackCooldownMax { get; set; } = 1.6f;
        public float RecoverDuration   { get; set; } = 0.7f;

        public EnemyAI(Enemy owner, float left, float right)
        {
            _owner       = owner;
            _patrolLeft  = left;
            _patrolRight = right;
        }

        /// <summary>
        /// Updates the patrol boundary positions (used after level scaling).
        /// </summary>
        public void SetPatrolBounds(float left, float right)
        {
            _patrolLeft  = left;
            _patrolRight = right;
        }

        /// <summary>Current left patrol boundary.</summary>
        public float PatrolLeft  => _patrolLeft;
        /// <summary>Current right patrol boundary.</summary>
        public float PatrolRight => _patrolRight;

        public void Update(float dt, Character target)
        {
            if (!_owner.IsAlive) { _state = AIState.Dead; return; }
            _attackCooldown -= dt;

            switch (_state)
            {
                case AIState.Patrol:  DoPatrol(dt, target);  break;
                case AIState.Chase:   DoChase(dt, target);   break;
                case AIState.Attack:  DoAttack(target);      break;
                case AIState.Recover: DoRecover(dt);         break;
            }
        }

        private void DoPatrol(float dt, Character target)
        {
            float spd = _owner.MoveSpeed * 0.4f;
            _owner.VelocityX = _goingRight ? spd : -spd;
            _owner.FacingRight = _goingRight;
            if (_owner.X >= _patrolRight) _goingRight = false;
            if (_owner.X <= _patrolLeft)  _goingRight = true;
            if (target != null && _owner.DistanceTo(target) < DetectionRange)
                GoTo(AIState.Chase);
        }

        private void DoChase(float dt, Character target)
        {
            if (target == null) { GoTo(AIState.Patrol); return; }
            _chaseTimeout -= dt;
            float dist = _owner.DistanceTo(target);
            if (_chaseTimeout <= 0 || dist > DetectionRange * 1.5f) { GoTo(AIState.Patrol); return; }
            if (dist <= AttackRange && _attackCooldown <= 0) { GoTo(AIState.Attack); return; }
            float dir = target.X > _owner.X ? 1f : -1f;
            _owner.VelocityX   = dir * _owner.MoveSpeed;
            _owner.FacingRight = dir > 0;
        }

        private void DoAttack(Character target)
        {
            _owner.VelocityX = 0;
            if (_owner.TryAttack())
            {
                _attackCooldown = AttackCooldownMax;
                _recoverTimer   = RecoverDuration;
                GoTo(AIState.Recover);
            }
        }

        private void DoRecover(float dt)
        {
            _owner.VelocityX = 0;
            _recoverTimer -= dt;
            if (_recoverTimer <= 0) GoTo(AIState.Chase);
        }

        private void GoTo(AIState next)
        {
            _state = next;
            if (next == AIState.Chase) _chaseTimeout = 5f;
        }
    }
}
