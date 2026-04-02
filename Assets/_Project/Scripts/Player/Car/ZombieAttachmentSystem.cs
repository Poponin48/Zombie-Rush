using UnityEngine;

namespace Project.Player.Car
{
    /// <summary>
    /// Reserved for future attach mechanic (zombies on hull). No logic yet.
    /// </summary>
    public class ZombieAttachmentSystem : MonoBehaviour
    {
        [SerializeField, Tooltip("Max attached zombies when the feature ships (design: 5).")]
        private int futureMaxAttached = 5;

        public int FutureMaxAttached => futureMaxAttached;
    }
}
