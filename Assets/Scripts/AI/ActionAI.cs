using System;
using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace WLL_NGO.AI
{
    public enum ActionUpdateFunction { Update, LateUpdate, FixedUpdate }

    /// <summary>
    /// Working for both team and player ai.
    /// It's a chain of running actions.
    /// </summary>
    public abstract class ActionAI: MonoBehaviour
    {
        //public UnityAction<bool> OnActionCompleted;
        //public UnityAction OnActionInterrupted;

        [SerializeField] ActionUpdateFunction updateFunction;
        public ActionUpdateFunction UpdateFunction
        {
            get { return updateFunction; }
            set { updateFunction = value; }
        }

        public ActionAI PreviousAction { get; private set; } = null;
        public ActionAI NextAction { get; private set; } = null;

        public MonoBehaviour Owner { get; private set; }

        bool interrupted = false;
        bool initialized = false;
        //bool completed = false;
        bool active = false;

        //float updateTime = 0;
        //float timeElapsed = 0;
        //UnityAction<ActionAI> interruptedCallback;

                
        protected abstract bool CheckConditions();
        protected abstract void Activate();
        //protected abstract bool IsCompleted(out bool succeeded);
        
        protected virtual void Update()
        {
            if (updateFunction != ActionUpdateFunction.Update || interrupted || !initialized)
                return;

            if(!CheckConditions())
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                Destroy(gameObject);
            }

            if (!active)
            {
                active = true;
                Activate();
            }
            
            

        }

        protected virtual void LateUpdate()
        {
            if (updateFunction != ActionUpdateFunction.LateUpdate || interrupted || !initialized)
                return;

            if (!CheckConditions())
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                Destroy(gameObject);
            }
            if (!active)
            {
                active = true;
                Activate();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (updateFunction != ActionUpdateFunction.FixedUpdate || interrupted || !initialized)
                return;

            if (!CheckConditions())
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                Destroy(gameObject);
            }

            if (!active)
            {
                active = true;
                Activate();
            }

        }

        protected virtual void OnDestroy()
        {
            if (PreviousAction)
            {
                PreviousAction.NextAction = null;
                PreviousAction.Restart();
            }
                
        }

        protected virtual void Restart()
        {
            //completed = false;
            //interrupted = false;
            active = false;
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="owner"></param>
        /// <param name="updateTime"></param>
        /// <param name="previousAction"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ActionAI CreateAction<T>(MonoBehaviour owner, ActionAI previousAction, ActionUpdateFunction updateFunction = ActionUpdateFunction.FixedUpdate, object[] parameters = null) where T : ActionAI
        {
            GameObject actionObject = new GameObject($"{owner.gameObject.name}_{typeof(T).Name}");
            //ActionAI action = actionObject.AddComponent<T>();
            ActionAI action = actionObject.AddComponent<T>();
            action.Owner = owner;
            action.PreviousAction = previousAction;
            action.UpdateFunction = updateFunction;
            //action.interruptedCallback = interruptedCallback;
            if (previousAction != null)
            {
                action.PreviousAction = previousAction;
                previousAction.NextAction = action;
                action.transform.parent = previousAction.transform;
            }
            action.Initialize(parameters);
            return action;
        }

        
        public virtual void Initialize(object[] parameters = null) 
        {
            Debug.Log("Action - Initialize");
            initialized = true;
            
        }

        



    }

}
