using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class FormationHelper : MonoBehaviour
    {
        public static FormationHelper HomeFormationHelper { get; private set; }
        public static FormationHelper AwayFormationHelper { get; private set; }

        List<FormationHelperTrigger> currentTriggers = new List<FormationHelperTrigger>();

        public IList<FormationHelperTrigger> CurrentTriggers
        {
            get{ return currentTriggers.AsReadOnly(); }
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


        public void SetActualFormation(int actualFormation)
        {
            if (formations == null || formations.Count == 0)
                return;

            foreach (var formation in formations)
                    formation.SetActive(false);

            currentTriggers.Clear();

            this.actualFormation = actualFormation;
            formations[actualFormation].SetActive(true);
        }

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
    }
    
}
