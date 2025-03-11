using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class MoveToDirectionActionAI : PlayerActionAI
    {

        Vector3 direction;

        public override void Initialize(ActionParams parameters = default)
        {
            base.Initialize(parameters);
            
            this.direction = (parameters as ReachDestinationActionParams).Destination;
            
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
