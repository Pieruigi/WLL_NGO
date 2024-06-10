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

        [SerializeField]
        Transform idlePoint;
        public Vector3 DefaultPosition
        {
            get { return idlePoint.position; }
        }

        private void Awake()
        {
            zoneTriggerList.Add(this);
            Activate(false);
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

            if(player.TeamAI != caretaker.TeamAI)
            {
                OnOpponentPlayerEnter?.Invoke(this, player);
            }
            
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(Tags.Player))
                return;
            PlayerAI player = other.GetComponent<PlayerAI>();
            if(player.TeamAI != caretaker.TeamAI)
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
            
            GetComponent<Collider>().enabled = value;
        }
    }

}
