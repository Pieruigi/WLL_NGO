using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.AI
{
    public class ZoneTrigger : MonoBehaviour
    {
        public static UnityAction<ZoneTrigger, PlayerAI> OnOpponentPlayerEnter;
        public static UnityAction<ZoneTrigger, PlayerAI> OnOpponentPlayerExit;

        public static List<ZoneTrigger> zoneTriggerList = new List<ZoneTrigger>();

        [SerializeField]
        PlayerAI caretaker;
        public PlayerAI Caretaker { get { return caretaker; } }

        [SerializeField]
        Transform idlePoint;
        public Vector3 DefaultPosition
        {
            get { return idlePoint.position; }
        }

        [SerializeField]
        List<PlayerAI> inTriggerList = new List<PlayerAI>();
        public List<PlayerAI> InTriggerList { get { return inTriggerList; } }

        /// <summary>
        /// Default length is when defensive line falls in the middle field, where default offset might by different than zero ( think for example
        /// to a middle field defending zone )
        /// </summary>
        float defaultOffset = 0;
        
        
        [SerializeField]
        bool moveOnResize = false;

        bool activated = false;

        private void Awake()
        {
            defaultOffset = transform.parent.position.x;
            zoneTriggerList.Add(this);
            Activate(false);
        }

        private void Update()
        {
            if (!activated)
                return;

            float ratio = 2f * Caretaker.TeamAI.WaitingLine / GameFieldInfo.GetFieldLength();
          
            if (transform.parent.localScale.x != ratio)
            {
                
                Vector3 scale = transform.parent.localScale;
                scale.x = ratio;
          
                transform.parent.localScale = scale;
            }
        }

        private void OnDestroy()
        {
            zoneTriggerList.Remove(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(Tags.Player))
                return;
            PlayerAI player = other.GetComponent<PlayerAI>();

            if (inTriggerList.Contains(player)) // To be sure
                return;

            inTriggerList.Add(player); // We add any player

            if (player.TeamAI != caretaker.TeamAI)
            {
                // We only report opponents
                OnOpponentPlayerEnter?.Invoke(this, player);
            }
            
        }



        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(Tags.Player))
                return;
            PlayerAI player = other.GetComponent<PlayerAI>();

            inTriggerList.Remove(player);

            if (player.TeamAI != caretaker.TeamAI)
            {
                OnOpponentPlayerExit?.Invoke(this, player);
            }
            
        }

        public static IList<ZoneTrigger> GetZoneTriggerAll()
        {
            return zoneTriggerList.AsReadOnly();
        }

        public static IList<ZoneTrigger> GetZoneTriggers(PlayerAI player)
        {
            return zoneTriggerList.Where(z=>z.caretaker == player).ToList();
        }

        public bool IsPlayerZone(PlayerAI player)
        {
            return caretaker == player;
        }

        public void Activate(bool value)
        {
            activated = value;
            GetComponent<Collider>().enabled = value;
        }


    }

}
