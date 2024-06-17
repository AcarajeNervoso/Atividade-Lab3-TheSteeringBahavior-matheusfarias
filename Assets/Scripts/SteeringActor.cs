using UnityEngine.UI;
using UnityEngine;

enum Behavior { Idle, Seek, Evade, Flee }
enum State { Idle, Arrive, Seek, Evade, Flee }

[RequireComponent(typeof(Rigidbody2D))]
public class SteeringActor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] Behavior behavior = Behavior.Seek;
    [SerializeField] Transform target = null;
    [SerializeField] float maxSpeed = 4f;
    [SerializeField, Range(0.1f, 0.99f)] float decelerationFactor = 0.75f;
    [SerializeField] float arriveRadius = 1.2f;
    [SerializeField] float stopRadius = 0.5f;
    [SerializeField] float evadeRadius = 5f;
    [SerializeField] float fleeRadius = 5f; // Raio de distância para comportamento de fuga

    Text behaviorDisplay = null;
    Rigidbody2D physics;
    State state = State.Idle;

    void Awake()
    {
        physics = GetComponent<Rigidbody2D>();
        physics.isKinematic = true;
        behaviorDisplay = GetComponentInChildren<Text>();
    }

    void FixedUpdate()
    {
        // Verifica se há um alvo definido
        if (target == null)
        {
            // Se não houver alvo, define comportamento como Idle e aplica desaceleração
            behavior = Behavior.Idle;
            ApplySteering(Vector2.zero, 0f);
        }
        else
        {
            // Calcula a distância até o alvo
            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            // Aplica a lógica de steering baseada no comportamento atual
            switch (behavior)
            {
                case Behavior.Idle:
                    ApplySteering(Vector2.zero, 0f);
                    break;
                case Behavior.Seek:
                    Vector2 seekSteering = CalculateSteering(target.position, maxSpeed);
                    ApplySteering(seekSteering, distanceToTarget);
                    break;
                case Behavior.Evade:
                    if (distanceToTarget < evadeRadius)
                    {
                        Vector2 evadeSteering = CalculateSteering(target.position, maxSpeed);
                        ApplySteering(-evadeSteering, distanceToTarget);
                    }
                    else
                    {
                        ApplySteering(Vector2.zero, 0f);
                    }
                    break;
                case Behavior.Flee:
                    if (distanceToTarget < fleeRadius)
                    {
                        Vector2 fleeSteering = CalculateSteering(target.position, maxSpeed);
                        ApplySteering(-fleeSteering, distanceToTarget);
                    }
                    else
                    {
                        ApplySteering(Vector2.zero, 0f);
                    }
                    break;
            }
            // Atualiza o estado no canvas
            switch (behavior)
            {
                case Behavior.Idle:
                    state = State.Idle;
                    break;
                case Behavior.Seek:
                    state = State.Seek;
                    break;
                case Behavior.Evade:
                    if (distanceToTarget < evadeRadius)
                    {
                        state = State.Evade;
                    }
                    else
                    {
                        state = State.Idle;
                    }
                    break;
                case Behavior.Flee:
                    if (distanceToTarget < fleeRadius)
                    {
                        state = State.Flee;
                    }
                    else
                    {
                        state = State.Idle;
                    }
                    break;
            }
        }

        // Limita a velocidade máxima
        physics.velocity = Vector2.ClampMagnitude(physics.velocity, maxSpeed);

        // Atualiza o texto do display com o estado atual
        behaviorDisplay.text = state.ToString().ToUpper();
    }

    Vector2 CalculateSteering(Vector3 targetPosition, float speed)
    {
        // Calcula o vetor de direção desejada
        Vector2 desiredVelocity = ((Vector2)targetPosition - physics.position).normalized * speed;

        // Calcula a força de steering (alteração na velocidade)
        return desiredVelocity - physics.velocity;
    }

    void ApplySteering(Vector2 steering, float distance)
    {
        // Define o estado baseado na distância
        if (distance < stopRadius)
        {
            state = State.Idle;
        }
        else if (distance < arriveRadius)
        {
            state = State.Arrive;
        }
        else
        {
            state = behavior == Behavior.Idle ? State.Idle : (behavior == Behavior.Seek ? State.Seek : State.Evade);
        }

        // Aplica o steering dependendo do estado atual
        switch (state)
        {
            case State.Idle:
                physics.velocity *= decelerationFactor;
                break;
            case State.Arrive:
                float arriveFactor = 0.01f + (distance - stopRadius) / (arriveRadius - stopRadius);
                physics.velocity += arriveFactor * steering * Time.fixedDeltaTime;
                break;
            case State.Seek:
            case State.Evade:
            case State.Flee:
                physics.velocity += steering * Time.fixedDeltaTime;
                break;
        }
    }

    void OnDrawGizmos()
    {
        if (target == null) return;

        // Desenha os gizmos correspondentes ao comportamento atual
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, arriveRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopRadius);

        switch (behavior)
        {
            case Behavior.Evade:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, evadeRadius);
                break;
            case Behavior.Flee:
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, fleeRadius);
                break;
        }

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, target.position);
    }
}
