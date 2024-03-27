using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.AI
{
    public enum ActionUpdateFunction { Update, LateUpdate, FixedUpdate }

    /// <summary>
    /// Working for both team and player ai.
    /// It's a chain of running actions.
    /// </summary>
    public abstract class ActionAI: MonoBehaviour
    {
        public UnityAction<bool> OnActionCompleted;
        public UnityAction OnActionInterrupted;

        [SerializeField] ActionUpdateFunction updateFunction;
        public ActionUpdateFunction UpdateFunction
        {
            get { return updateFunction; }
            set { updateFunction = value; }
        }

        public ActionAI PreviousAction { get; private set; } = null;
        public ActionAI NextAction { get; private set; } = null;

        bool interrupted = false;
        bool initialized = false;

        float updateTime = 0;
        float timeElapsed = 0;

        protected abstract bool CheckConditions();
        protected abstract void DoUpdate();
        protected abstract bool IsCompleted(out bool succeeded);
        
        protected virtual void Update()
        {
            if (updateFunction != ActionUpdateFunction.Update || interrupted || !initialized)
                return;

            if(!CheckConditions())
            {
                interrupted = true;
                OnActionInterrupted?.Invoke();
            }
            else
            {
                timeElapsed -= Time.deltaTime;
                if(timeElapsed < 0)
                {
                    timeElapsed = updateTime;
                    DoUpdate();
                    bool succeeded = false;
                    if (IsCompleted(out succeeded))
                    {
                        OnActionCompleted?.Invoke(succeeded);
                    }
                }

                
            }
        }

        protected virtual void LateUpdate()
        {
            if (updateFunction != ActionUpdateFunction.LateUpdate || interrupted || !initialized)
                return;

            if (!CheckConditions())
            {
                interrupted = true;
                OnActionInterrupted?.Invoke();
            }
            else
            {
                timeElapsed -= Time.deltaTime;
                if(timeElapsed < 0)
                {
                    timeElapsed = updateTime;
                    DoUpdate();
                    bool succeeded = false;
                    if (IsCompleted(out succeeded))
                    {
                        OnActionCompleted?.Invoke(succeeded);
                    }
                }

                
            }
        }

        protected virtual void FixedUpdate()
        {
            if (updateFunction != ActionUpdateFunction.FixedUpdate || interrupted || !initialized)
                return;

            if (!CheckConditions())
            {
                interrupted = true;
                OnActionInterrupted?.Invoke();
            }
            else
            {
                timeElapsed -= Time.fixedDeltaTime;
                if (timeElapsed < 0)
                {
                    timeElapsed = updateTime;
                    DoUpdate();
                    bool succeeded = false;
                    if (IsCompleted(out succeeded))
                    {
                        OnActionCompleted?.Invoke(succeeded);
                    }
                }

                
            }
        }

        public static ActionAI CreateAction<T>(float updateTime = 0, ActionAI previousAction = null, object[] parameters = null) where T : ActionAI
        {
            GameObject actionObject = new GameObject($"{nameof(T)}");
            //ActionAI action = actionObject.AddComponent<T>();
            ActionAI action = actionObject.AddComponent<T>();
            action.Initialize(.5f, previousAction, parameters);
            return action;
        }

        public virtual void Initialize(float updateTime = 0, ActionAI previousAction = null, object[] parameters = null) 
        {
            initialized = true;
            timeElapsed = updateTime;
            if (previousAction != null)
            {
                PreviousAction = previousAction;
                previousAction.NextAction = this;
            }
        }

    }

}
