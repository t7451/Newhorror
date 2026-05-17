using HorrorCoopGame.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace HorrorCoopGame.AI
{
    /// <summary>
    /// Server-only enemy AI driven by a simple state machine. Pathfinding
    /// destination updates are throttled to keep WebGL/mobile CPU low.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyAI : NetworkBehaviour
    {
        private enum State
        {
            Patrol,
            Chase,
            Attack
        }

        [Header("Behaviour")]
        [SerializeField] private float detectionRadius = 12f;
        [SerializeField] private float loseSightRadius = 18f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackCooldown = 1.25f;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float patrolRadius = 8f;

        [Header("Optimization")]
        [SerializeField] private float destinationUpdateInterval = 0.2f;
        [Tooltip("Multiplier applied to update interval when no player is inside loseSightRadius. Saves CPU on idle/far enemies (mobile/WebGL).")]
        [SerializeField] private float idleUpdateMultiplier = 4f;

        private NavMeshAgent agent;
        private Transform cachedTransform;
        private State state = State.Patrol;
        private float nextPathUpdateTime;
        private float nextAttackTime;
        private Vector3 patrolAnchor;
        private Vector3 patrolTarget;
        private Transform chaseTarget;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                // Pathfinding is server-authoritative only.
                if (TryGetComponent(out NavMeshAgent localAgent))
                {
                    localAgent.enabled = false;
                }

                enabled = false;
                return;
            }

            agent = GetComponent<NavMeshAgent>();
            cachedTransform = transform;
            patrolAnchor = cachedTransform.position;
            ChoosePatrolTarget();
        }

        private void Update()
        {
            float now = Time.time;
            if (now < nextPathUpdateTime)
            {
                return;
            }

            // Adaptive throttling: when patrolling (no target) update less often.
            float interval = state == State.Patrol
                ? destinationUpdateInterval * Mathf.Max(1f, idleUpdateMultiplier)
                : destinationUpdateInterval;
            nextPathUpdateTime = now + interval;

            switch (state)
            {
                case State.Patrol:
                    TickPatrol();
                    break;
                case State.Chase:
                    TickChase();
                    break;
                case State.Attack:
                    TickAttack();
                    break;
            }
        }

        private void TickPatrol()
        {
            if (TryFindClosestPlayer(detectionRadius, out Transform player))
            {
                chaseTarget = player;
                state = State.Chase;
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                ChoosePatrolTarget();
            }

            agent.SetDestination(patrolTarget);
        }

        private void TickChase()
        {
            if (chaseTarget == null)
            {
                state = State.Patrol;
                return;
            }

            // sqrMagnitude avoids a per-tick sqrt; matters when many enemies tick on mobile.
            float sqrDistance = (chaseTarget.position - cachedTransform.position).sqrMagnitude;
            if (sqrDistance > loseSightRadius * loseSightRadius)
            {
                chaseTarget = null;
                state = State.Patrol;
                return;
            }

            if (sqrDistance <= attackRange * attackRange)
            {
                state = State.Attack;
                return;
            }

            agent.SetDestination(chaseTarget.position);
        }

        private void TickAttack()
        {
            if (chaseTarget == null)
            {
                state = State.Patrol;
                return;
            }

            float breakRange = attackRange + 0.5f;
            float sqrDistance = (chaseTarget.position - cachedTransform.position).sqrMagnitude;
            if (sqrDistance > breakRange * breakRange)
            {
                state = State.Chase;
                return;
            }

            agent.SetDestination(cachedTransform.position);

            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            if (chaseTarget.TryGetComponent(out PlayerStats stats))
            {
                stats.TakeDamageServerRpc(attackDamage);
            }
        }

        private bool TryFindClosestPlayer(float radius, out Transform closest)
        {
            closest = null;
            float bestSqr = radius * radius;
            Vector3 selfPosition = cachedTransform.position;

            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                NetworkObject playerObject = kvp.Value.PlayerObject;
                if (playerObject == null)
                {
                    continue;
                }

                float sqr = (playerObject.transform.position - selfPosition).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    closest = playerObject.transform;
                }
            }

            return closest != null;
        }

        private void ChoosePatrolTarget()
        {
            Vector2 random = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = patrolAnchor + new Vector3(random.x, 0f, random.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolTarget = hit.position;
            }
            else
            {
                patrolTarget = patrolAnchor;
            }
        }
    }
}
