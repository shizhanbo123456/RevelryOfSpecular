using UnityEngine;

public class EntityPlayer : MonoBehaviour
{
    [SerializeField]private Animator animator;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void RefreshAnimation(EntityType type, float speed, bool hasNearbyPlayer)
    {
        if (animator == null) return;

        if (type.IsCharacter())
        {
            animator.SetBool("Moving", speed > 0.01f);
            return;
        }

        if (!type.IsNpc()) return;

        if (hasNearbyPlayer)
        {
            animator.SetInteger("State", 3);
        }
        else if (speed <= 0.01f)
        {
            animator.SetInteger("State", 0);
        }
        else if (speed <= 2f)
        {
            animator.SetInteger("State", 1);
        }
        else
        {
            animator.SetInteger("State", 2);
        }
    }
}
