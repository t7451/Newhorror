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

        private NavMeshAgent agent;
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
            patrolAnchor = transform.position;
            ChoosePatrolTarget();
        }

        private void Update()
        {
            if (Time.time < nextPathUpdateTime)
            {
                return;
            }

            nextPathUpdateTime = Time.time + destinationUpdateInterval;

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

            float distance = Vector3.Distance(transform.position, chaseTarget.position);
            if (distance > loseSightRadius)
            {
                chaseTarget = null;
                state = State.Patrol;
                return;
            }

            if (distance <= attackRange)
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

            float distance = Vector3.Distance(transform.position, chaseTarget.position);
            if (distance > attackRange + 0.5f)
            {
                state = State.Chase;
                return;
            }

            agent.SetDestination(transform.position);

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

            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                NetworkObject playerObject = kvp.Value.PlayerObject;
                if (playerObject == null)
                {
                    continue;
                }

                float sqr = (playerObject.transform.position - transform.position).sqrMagnitude;
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
