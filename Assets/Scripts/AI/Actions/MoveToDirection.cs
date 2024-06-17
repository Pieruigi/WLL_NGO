using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class MoveToDirection : PlayerActionAI
    {

        Vector3 direction;

        public override void Initialize(object[] parameters = null)
        {
            base.Initialize(parameters);
            if(parameters != null)
            {
                this.direction = (Vector3)parameters[0];
            }
            
        }

        protected override void Loop()
        {
            base.Loop();

#if TEST_AI
            PlayerAI.transform.position += direction * PlayerAI.Speed * DeltaTime;
            
#endif
        }

        private void OnDestroy()
        {
            // We should reset the command here
        }
    }

}
