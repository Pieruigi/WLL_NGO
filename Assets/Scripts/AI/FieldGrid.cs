using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class FieldGrid: Singleton<FieldGrid>
    {

        class PlayerData
        {
            //public PlayerAI player;
            public FieldBlock currentBlock;
            public FieldBlock previousBlock;
        }

        [SerializeField]
        List<FieldBlock> blocks;

        [SerializeField]
        int rowCount = 5;

        [SerializeField]
        int colCount = 9;

        [SerializeField]
        Collider centerBlock;

        Dictionary<PlayerAI, PlayerData> playerDictionary = new Dictionary<PlayerAI, PlayerData>();

        protected override void Awake()
        {
            base.Awake();
            // Check rows * cols
            if (rowCount * colCount != blocks.Count)
                Debug.LogError("TeamFieldGrid - rows * cols != blocks.Count");
            
            

        }

        

        private void OnEnable()
        {
            FieldBlock.OnPlayerEnter += HandleOnPlayerEnter;
            FieldBlock.OnPlayerExit += HandleOnPlayerExit;
        }

        

        private void OnDisable()
        {
            FieldBlock.OnPlayerEnter -= HandleOnPlayerEnter;
            FieldBlock.OnPlayerExit -= HandleOnPlayerExit;
        }

        private void HandleOnPlayerExit(FieldBlock block, PlayerAI player)
        {
            if (!playerDictionary.ContainsKey(player))
                playerDictionary.Add(player, new PlayerData());

            if (block == playerDictionary[player].currentBlock)
            {
                playerDictionary[player].currentBlock = playerDictionary[player].previousBlock;
            }

            if (block == playerDictionary[player].previousBlock)
            {
                playerDictionary[player].previousBlock = null;
            }

            Debug.Log($"PlayerData - player:{player}, current:{playerDictionary[player].currentBlock}, prev:{playerDictionary[player].previousBlock}");
        }

        private void HandleOnPlayerEnter(FieldBlock block, PlayerAI player)
        {
            if(!playerDictionary.ContainsKey(player))
                playerDictionary.Add(player, new PlayerData());

            playerDictionary[player].previousBlock = playerDictionary[player].currentBlock;
            playerDictionary[player].currentBlock = block;
            Debug.Log($"PlayerData - player:{player}, current:{playerDictionary[player].currentBlock}, prev:{playerDictionary[player].previousBlock}");
        }

        public Collider Move(PlayerAI player, int forward, int right)
        {
            bool home = player.TeamAI == TeamAI.HomeTeamAI;

            return null;
        }

        //public Collider GetCollider(Vector3 position)
        //{

        //}

        public Vector3 GetRandomPositionInsideBlock(int blockId)
        {
            Vector3 pos = blocks[blockId].transform.position;
            pos.y = 0;
            Debug.Log($"Block {blocks[blockId].name}, pos:{pos}");
            return pos;
        }

        public int ForwardBlocksLeft(PlayerAI player)
        {
            int currId = blocks.IndexOf(playerDictionary[player].currentBlock);
            int row = currId / colCount;
            int col = currId % colCount;

            bool home = player.TeamAI == TeamAI.HomeTeamAI;
            if (home)
                return colCount - 1 - col;
            else
                return col;
            
        }

        public int BackwardBlocksLeft(PlayerAI player)
        {
            int currId = blocks.IndexOf(playerDictionary[player].currentBlock);
            int row = currId / colCount;
            int col = currId % colCount;

            bool home = player.TeamAI == TeamAI.HomeTeamAI;
            if (!home)
                return colCount - 1 - col;
            else
                return col;

        }


        public int RightBlocksLeft(PlayerAI player)
        {
            int currId = blocks.IndexOf(playerDictionary[player].currentBlock);
            int row = currId / colCount;
            int col = currId % colCount;

            bool home = player.TeamAI == TeamAI.HomeTeamAI;
            if (home)
                return rowCount - 1 - row;
            else
                return row;

        }

        public int LeftBlocksLeft(PlayerAI player)
        {
            int currId = blocks.IndexOf(playerDictionary[player].currentBlock);
            int row = currId / colCount;
            int col = currId % colCount;

            bool home = player.TeamAI == TeamAI.HomeTeamAI;
            if (!home)
                return rowCount - 1 - row;
            else
                return row;

        }
    }

}
