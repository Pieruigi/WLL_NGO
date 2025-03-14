using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class FormationHelper : MonoBehaviour
    {
        public static FormationHelper HomeFormationHelper { get; private set; }
        public static FormationHelper AwayFormationHelper { get; private set; }

        List<FormationHelperTrigger> currentTriggers = new List<FormationHelperTrigger>();

        public IList<FormationHelperTrigger> CurrentTriggers
        {
            get { return currentTriggers.AsReadOnly(); }
        }

        [SerializeField]
        bool home;
        public bool Home
        {
            get { return home; }
        }

        [SerializeField]
        List<GameObject> formations;

        int actualFormation = 1;

        List<Transform> kickOffTransforms = new List<Transform>(), kickOffKickerTransforms = new List<Transform>();

        List<ZoneTrigger> waitingZones = new List<ZoneTrigger>();
        List<ZoneTrigger> pressingZones = new List<ZoneTrigger>();

        void Awake()
        {
            // if (name.ToLower().StartsWith("home"))
            //     home = true;
            // else if (name.ToLower().StartsWith("away"))
            //     home = false;

            if (home)
                HomeFormationHelper = this;
            else
                AwayFormationHelper = this;

            //SetActualFormation(actualFormation);
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Initialize(GameObject prefab)
        {
            // Remove any other formation if exists
            if (transform.childCount > 0)
            {
                Destroy(transform.GetChild(0));
            }

            // Clear trigger 
            currentTriggers.Clear();
            kickOffTransforms.Clear();
            kickOffKickerTransforms.Clear();
            waitingZones.Clear();
            pressingZones.Clear();

            // Create formation
            GameObject fo = Instantiate(prefab, transform);
            fo.transform.localPosition = Vector3.zero;
            fo.transform.localEulerAngles = Vector3.up * (home ? 0f : 180f);

            // Get kick off transforms
            Transform kok = fo.transform.Find("KickOff").Find("Kicker");
            Transform ko = fo.transform.Find("KickOff").Find("NotKicker");
            for (int i = 0; i < MatchController.Instance.PlayerPerTeam; i++)
            {
                kickOffTransforms.Add(ko.GetChild(i));
                kickOffKickerTransforms.Add(kok.GetChild(i));
            }

            // Get defensive zone
            ZoneTrigger[] wz = fo.transform.Find("WaitingZone").GetComponentsInChildren<ZoneTrigger>();
            ZoneTrigger[] pz = fo.transform.Find("PressingZone").GetComponentsInChildren<ZoneTrigger>();
            for (int i = 0; i < MatchController.Instance.PlayerPerTeam-1; i++)
            {
                wz[i].Init(i);
                pz[i].Init(i);
                waitingZones.Add(wz[i]);
                pressingZones.Add(pz[i]);
            }
                
            
            // Get pressing zone

        }


        // public void SetActualFormation(int actualFormation)
        // {
        //     if (formations == null || formations.Count == 0)
        //         return;

        //     foreach (var formation in formations)
        //         formation.SetActive(false);

        //     currentTriggers.Clear();

        //     this.actualFormation = actualFormation;
        //     formations[actualFormation].SetActive(true);
        // }

        public void AddTrigger(FormationHelperTrigger trigger)
        {
            if (currentTriggers.Contains(trigger))
                return;

            currentTriggers.Add(trigger);

        }

        public void RemoveTrigger(FormationHelperTrigger trigger)
        {
            currentTriggers.Remove(trigger);
        }

        public IList<Transform> GetKickOffSpawnPoints()
        {
            return kickOffTransforms;
        }

        public IList<Transform> GetKickOffKickerSpawnPoints()
        {
            return kickOffKickerTransforms;
        }
    }
    
}
