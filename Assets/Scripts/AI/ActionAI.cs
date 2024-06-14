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
      
        Func<bool> ConditionFunction;
        
        protected virtual void Update()
        {
            if (updateFunction != ActionUpdateFunction.Update || interrupted || !initialized || completed)
                return;

            if(ConditionFunction != null && !ConditionFunction.Invoke(/*conditionParameters*/))
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                OnActionInterrupted?.Invoke(this);
                CheckForDestroy();

            }

            if (interrupted)
                return;

            
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

            if (ConditionFunction != null && !ConditionFunction.Invoke(/*conditionParameters*/))
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                OnActionInterrupted?.Invoke(this);
                CheckForDestroy();
            }

            if (interrupted)
                return;

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

            if (ConditionFunction != null && !ConditionFunction.Invoke(/*conditionParameters*/))
            {
                interrupted = true;
                //interruptedCallback?.Invoke(this);
                OnActionInterrupted?.Invoke(this);
                CheckForDestroy();
                
            }

            if (interrupted)
                return;

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
                    DestroyAction();
                   
                }
                
            }
                
        }

        

        protected virtual void Loop(){}
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The type of action ( must be of type ActionAI )</typeparam>
        /// <param name="owner">The logic entity that is creating the current action ( for example a team or a specific player in the field )</param>
        /// <param name="previousAction">The parent action</param>
        /// <param name="restartOnNoChildren">True if you want to player the Activate() method again once all the children have completed</param>
        /// <param name="updateFunction">Does the action run in Update, LateUpdate or FixedUpdate?</param>
        /// <param name="parameters">Initialization parameters</param>
        /// <param name="conditionFunction">A delegate function returning a bool to check action condition ( on false the action is interrupted )</param>
        /// <returns>The action just created</returns>
        public static ActionAI CreateAction<T>(MonoBehaviour owner, ActionAI previousAction, bool restartOnNoChildren = false, ActionUpdateFunction updateFunction = ActionUpdateFunction.Update, object[] parameters = null, Func<bool> conditionFunction = null) where T : ActionAI
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
            action.ConditionFunction = conditionFunction;
            return action;
        }

        protected virtual void Activate() { }

        

        protected virtual void HandleOnChildActionInterrupted(ActionAI childAction){}

        protected virtual void HandleOnChildActionCompleted(ActionAI childAction, bool succeeded){}

        public virtual bool IsCompleted(out bool succeeded)
        {
            succeeded = false;
            return false;
        }
        
        public virtual void Initialize(object[] parameters = null) 
        {
            initialized = true;
        }

        protected virtual void Release() { }

        public virtual void Restart()
        {
            active = false;
            completed = false;
            interrupted = false;
        }

        public virtual void DestroyAction()
        {
            PreviousAction.NextActionList.Remove(this);
            if (PreviousAction.NextActionList.Count == 0 && PreviousAction.restartOnNoChildren)
                PreviousAction.Restart();

            Release();
            Destroy(gameObject);
        }

        /// <summary>
        /// You can override the condition function passed in to the CreateAction method here. 
        /// </summary>
        /// <param name="function"></param>
        public void SetConditionFunction(Func<bool> function)
        {
            ConditionFunction = function;
        }
    }

}
