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
        public UnityAction<ActionAI, bool> OnActionCompleted;
        public UnityAction<ActionAI> OnActionInterrupted;

        [SerializeField] ActionUpdateFunction updateFunction;
        public ActionUpdateFunction UpdateFunction
        {
            get { return updateFunction; }
            private set { updateFunction = value; }
        }

        public ActionAI PreviousAction { get; private set; } = null;
        public List<ActionAI> NextActionList { get; private set; } = new List<ActionAI>();

        public MonoBehaviour Owner { get; private set; }

        bool interrupted = false;
        bool initialized = false;
        bool completed = false;
        bool active = false;
        bool restartOnNoChildren = false;
        //bool loop = false;
        //protected bool Loop
        //{
        //    get { return loop; }
        //    private set { loop = value; }
        //}    
        //float updateTime = 0;
        //float timeElapsed = 0;
        //UnityAction<ActionAI> interruptedCallback;

                
        protected abstract bool CheckConditions();
        protected abstract void Activate();
        //protected abstract bool IsCompleted(out bool succeeded);
        
        protected virtual void Update()
        {
            if (updateFunction != ActionUpdateFunction.Update || interrupted || !initialized || completed)
                return;

            if(!CheckConditions())
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                OnActionInterrupted?.Invoke(this);
                CheckForDestroy();
            }

            if (!active)
            {
                active = true;
                Activate();
            }

            Loop();

            bool succeeded;
            if(IsCompleted(out succeeded))
            {
                completed = true;
                OnActionCompleted?.Invoke(this, succeeded);
                CheckForDestroy();
            }

        }

        protected virtual void LateUpdate()
        {
            if (updateFunction != ActionUpdateFunction.LateUpdate || interrupted || !initialized || completed)
                return;

            if (!CheckConditions())
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                OnActionInterrupted?.Invoke(this);
                CheckForDestroy();
            }
            if (!active)
            {
                active = true;
                Activate();
            }

            Loop();

            bool succeeded;
            if (IsCompleted(out succeeded))
            {
                completed = true;
                OnActionCompleted?.Invoke(this, succeeded);
                CheckForDestroy();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (updateFunction != ActionUpdateFunction.FixedUpdate || interrupted || !initialized || completed)
                return;

            if (!CheckConditions())
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                OnActionInterrupted?.Invoke(this);
                CheckForDestroy();
            }

            if (!active)
            {
                active = true;
                Activate();
            }

            //if (loop)
            Loop();

            bool succeeded;
            if (IsCompleted(out succeeded))
            {
                completed = true;
                OnActionCompleted?.Invoke(this, succeeded);
                CheckForDestroy();
            }

        }

        /// <summary>
        /// When an action is completed or interrupted it is destroyed and the parent action is restarted.
        /// If you want the parent to keep going add a new action or restart this one when the action is notified.
        /// Override this function if you want a different behaviour.
        /// </summary>
        protected virtual void CheckForDestroy()
        {
            if (PreviousAction) // We don't want to destroy the root action
            {
                if (active)
                {
                    PreviousAction.NextActionList.Remove(this);
                    if (PreviousAction.NextActionList.Count == 0 && PreviousAction.restartOnNoChildren)
                        PreviousAction.Restart();
                        

                    Destroy(gameObject);
                }
                
            }
                
        }

        

        protected virtual void Loop(){}
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="owner"></param>
        /// <param name="updateTime"></param>
        /// <param name="previousAction"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ActionAI CreateAction<T>(MonoBehaviour owner, ActionAI previousAction, bool restartOnNoChildren = false, ActionUpdateFunction updateFunction = ActionUpdateFunction.FixedUpdate, object[] parameters = null) where T : ActionAI
        {
            GameObject actionObject = new GameObject($"{owner.gameObject.name}_{typeof(T).Name}");
            //ActionAI action = actionObject.AddComponent<T>();
            ActionAI action = actionObject.AddComponent<T>();
            action.Owner = owner;
            action.PreviousAction = previousAction;
            action.UpdateFunction = updateFunction;
            action.restartOnNoChildren = restartOnNoChildren;
            //action.interruptedCallback = interruptedCallback;
            if (previousAction != null)
            {
                action.PreviousAction = previousAction;
                previousAction.NextActionList.Add(action);
                action.transform.parent = previousAction.transform;
                action.OnActionCompleted += previousAction.HandleOnChildActionCompleted;
                action.OnActionInterrupted += previousAction.HandleOnChildActionInterrupted;
            }
            action.Initialize(parameters);
            return action;
        }

        protected virtual void HandleOnChildActionInterrupted(ActionAI childAction){}

        protected virtual void HandleOnChildActionCompleted(ActionAI childAction, bool succeeded){}

        public virtual bool IsCompleted(out bool succeeded)
        {
            succeeded = false;
            return false;
        }
        
        public virtual void Initialize(object[] parameters = null) 
        {
            Debug.Log("Action - Initialize");
            initialized = true;
            
        }

        public virtual void Restart()
        {
            Debug.Log("BBBBBBBBBBBBBBBBBBBBBBBBB");
            active = false;
            completed = false;
            interrupted = false;
        }




    }

}
