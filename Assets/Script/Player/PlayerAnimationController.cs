using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDown.Movement
{
    public class PlayerAnimationController : MonoBehaviour
    {
        public Animator animator; // Reference to the Animator component attached to the player character.
        private PlayerMovement playerMovement; // Reference to the PlayerMovement script for input detection.
        private Shoot shoot; // Reference to the Shoot script for shooting detection.

        private bool isPunching = false; // Track if the player is punching

        // Start is called before the first frame update
        void Start()
        {
            // Get the PlayerMovement component attached to the player.
            playerMovement = GetComponent<PlayerMovement>();
            // Get the Shoot component attached to the player.
            shoot = GetComponent<Shoot>();
        }

        // Update is called once per frame
        void Update()
        {
            // Check if the player is moving by checking the input vector from PlayerMovement and if the player can move.
            bool isMoving = playerMovement.CurrentInput.magnitude > 0.1f && playerMovement.CanMove;

            // Update the Animator's "isMove" parameter.
            animator.SetBool("isMove", isMoving);

            // Update the Animator's "isShoot" parameter.
            animator.SetBool("isShoot", shoot != null && shoot.isShooting);

            // Update the Animator's "isPunch" parameter.
            animator.SetBool("isPunch", isPunching);
        }

        public void TriggerExplosion()
        {
            if (animator != null)
            {
                animator.SetTrigger("Explode"); // Ensure "Explode" is the name of the trigger in the Animator
            }
            else
            {
                Debug.LogError("Animator component is missing on the player!");
            }
        }
    }
}

