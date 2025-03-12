using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.AI
{
    public class FieldBlock : MonoBehaviour
    {
        //[SerializeField]
        bool defenceBlock, middleBlock, attackBlock, leftSideBlock, centerBlock, rightSideBlock;

        public bool CenterBlock => centerBlock;

        public bool HomeLeftSideBlock => leftSideBlock;

        public bool HomeRightSideBlock => rightSideBlock;

        public bool AwayLeftSideBlock => rightSideBlock;

        public bool AwayRightSideBlock => leftSideBlock;

        public bool HomeDefenceBlock => defenceBlock;

        public bool MiddleFieldBlock => middleBlock;

        public bool HomeAttackBlock => attackBlock;

        public bool AwayDefenceBlock => attackBlock;

        public bool AwayAttackBlock => defenceBlock;

        public static UnityAction<FieldBlock, PlayerAI> OnPlayerEnter;
        public static UnityAction<FieldBlock, PlayerAI> OnPlayerExit;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(Tags.Player))
                OnPlayerEnter?.Invoke(this, other.GetComponent<PlayerAI>());
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(Tags.Player))
                OnPlayerExit?.Invoke(this, other.GetComponent<PlayerAI>());
        }

        public void SetDefenceBlock()
        {
            defenceBlock = true;
        }

        public void SetMiddleBlock()
        {
            middleBlock = true;
        }

        public void SetAttackBlock()
        {
            attackBlock = true;
        }

        public void SetLeftSideBlock()
        {
            leftSideBlock = true;
        }

        public void SetCenterBlock()
        {
            centerBlock = true;
        }

        public void SetRightSideBlock()
        {
            rightSideBlock = true;
        }

        public bool IsDefence(bool home)
        {
            return home ? HomeDefenceBlock : AwayDefenceBlock;
        }

        public bool IsMiddleField()
        {
            return MiddleFieldBlock;
        }

        public bool IsAttack(bool home)
        {
            return home ? HomeAttackBlock : AwayAttackBlock;
        }

        public bool IsBehind(FieldBlock other, bool home)
        {
            return (IsDefence(home) && !other.IsDefence(home)) || (IsMiddleField() && other.IsAttack(home));
        }

        public bool IsAhead(FieldBlock other, bool home)
        {
            return (IsAttack(home) && !other.IsAttack(home)) || (IsMiddleField() && other.IsDefence(home));
        }

        public bool IsInLine(FieldBlock other, bool home)
        {
            return (IsDefence(home) && other.IsDefence(home)) || (IsMiddleField() && other.IsMiddleField()) || (IsAttack(home) && other.IsAttack(home));
        }

        public bool IsLeftSide(bool home)
        {
            return home ? HomeLeftSideBlock : AwayLeftSideBlock;
        }

        public bool IsCenter()
        {
            return CenterBlock;
        }

        public bool IsRightSide(bool home)
        {
            return home ? HomeRightSideBlock : AwayRightSideBlock;
        }

        public Vector3 GetRandomPosition()
        {
            Vector3 pos = transform.position;
            pos.y = 0;
            return pos;
        }


    }

}
