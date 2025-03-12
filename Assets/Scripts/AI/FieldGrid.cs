using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class FieldGrid : Singleton<FieldGrid>
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

        [SerializeField]
        int defenceColumnCount;

        [SerializeField]
        int middleColumnStart, middleColumnCount;

        [SerializeField]
        int sideRowCount, centerRowCount;


        int attackColumnCount;


        Dictionary<PlayerAI, PlayerData> playerDictionary = new Dictionary<PlayerAI, PlayerData>();

        protected override void Awake()
        {
            base.Awake();
            // Check rows * cols
            if (rowCount * colCount != blocks.Count)
                Debug.LogError("TeamFieldGrid - rows * cols != blocks.Count");

            attackColumnCount = defenceColumnCount;

            InitDefenceBlocks();
            InitMiddleBlocks();
            InitAttackBlocks();
            InitSideBlocks();
            InitCenterBlocks();
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
            if (!playerDictionary.ContainsKey(player))
                playerDictionary.Add(player, new PlayerData());

            playerDictionary[player].previousBlock = playerDictionary[player].currentBlock;
            playerDictionary[player].currentBlock = block;
            Debug.Log($"PlayerData - player:{player}, current:{playerDictionary[player].currentBlock}, prev:{playerDictionary[player].previousBlock}");
        }

        void InitDefenceBlocks()
        {
            var defenceColumnStart = 0;
            for (int i = 0; i < rowCount; i++) // Rows
            {
                int start = defenceColumnStart + colCount * i;
                int stop = start + defenceColumnCount;
                for (int j = start; j < stop; j++)
                    blocks[j].SetDefenceBlock();

            }
        }

        void InitMiddleBlocks()
        {

            for (int i = 0; i < rowCount; i++) // Rows
            {
                int start = middleColumnStart + colCount * i;
                int stop = start + middleColumnCount;
                for (int j = start; j < stop; j++)
                    blocks[j].SetMiddleBlock();

            }
        }

        void InitAttackBlocks()
        {
            var attackColumnStart = colCount - attackColumnCount;
            for (int i = 0; i < rowCount; i++) // Rows
            {
                int start = attackColumnStart + colCount * i;
                int stop = start + attackColumnCount;
                for (int j = start; j < stop; j++)
                    blocks[j].SetAttackBlock();
            }
        }

        void InitSideBlocks()
        {

            for (int i = 0; i < sideRowCount; i++) // Rows
            {
                int start = colCount * i;
                int stop = start + colCount;
                for (int j = start; j < stop; j++)
                {
                    blocks[j].SetLeftSideBlock();
                    blocks[(rowCount - 1 - i) * colCount + j % colCount].SetRightSideBlock(); // Other side
                }
            }

        }

        void InitCenterBlocks()
        {
            int offset = (rowCount - centerRowCount) / 2 * colCount;

            for (int i = 0; i < centerRowCount; i++) // Rows
            {
                int start = offset + colCount * i;
                int stop = start + colCount;
                for (int j = start; j < stop; j++)
                    blocks[j].SetCenterBlock();
            }


        }

        public Collider Move(PlayerAI player, int forward, int right)
        {
            bool home = player.TeamAI == TeamAI.HomeTeamAI;

            return null;
        }



        public Vector3 GetRandomPositionInsideBlock(int blockId)
        {
            return blocks[blockId].GetRandomPosition();
        }

        

        public List<FieldBlock> GetLeftDefenceBlockAll(TeamAI team)
        {
            if (team.Home)
                return blocks.FindAll(b => b.HomeDefenceBlock && b.HomeLeftSideBlock);
            else
                return blocks.FindAll(b => b.AwayDefenceBlock && b.AwayLeftSideBlock);
        }

        public List<FieldBlock> GetLeftMiddleFieldBlockAll(TeamAI team)
        {
            if (team.Home)
                return blocks.FindAll(b => b.MiddleFieldBlock && b.HomeLeftSideBlock);
            else
                return blocks.FindAll(b => b.MiddleFieldBlock && b.AwayLeftSideBlock);
        }

        public List<FieldBlock> GetLeftAttackBlockAll(TeamAI team)
        {
            if (team.Home)
                return blocks.FindAll(b => b.HomeAttackBlock && b.HomeLeftSideBlock);
            else
                return blocks.FindAll(b => b.AwayAttackBlock && b.AwayLeftSideBlock);
        }


        public List<FieldBlock> GetCenterDefenceBlockAll(TeamAI team)
        {
            if (team.Home)
                return blocks.FindAll(b => b.HomeDefenceBlock && b.CenterBlock);
            else
                return blocks.FindAll(b => b.AwayDefenceBlock && b.CenterBlock);
        }

        public List<FieldBlock> GetCenterMiddleFieldBlockAll()
        {
            return blocks.FindAll(b => b.MiddleFieldBlock && b.CenterBlock);
        }

        public List<FieldBlock> GetCenterAttackBlockAll(TeamAI team)
        {
            if (team.Home)
                return blocks.FindAll(b => b.HomeAttackBlock && b.CenterBlock);
            else
                return blocks.FindAll(b => b.AwayAttackBlock && b.CenterBlock);
        }

        public List<FieldBlock> GetRightDefenceBlockAll(TeamAI team)
        {
            if (team.Home)
                return blocks.FindAll(b => b.HomeDefenceBlock && b.HomeRightSideBlock);
            else
                return blocks.FindAll(b => b.AwayDefenceBlock && b.AwayRightSideBlock);
        }

        public List<FieldBlock> GetRightMiddleFieldBlockAll(TeamAI team)
        {
            if (team.Home)
                return blocks.FindAll(b => b.MiddleFieldBlock && b.HomeRightSideBlock);
            else
                return blocks.FindAll(b => b.MiddleFieldBlock && b.AwayRightSideBlock);
        }

        public List<FieldBlock> GetRightAttackBlockAll(TeamAI team)
        {
            if (team.Home)
                return blocks.FindAll(b => b.HomeAttackBlock && b.HomeRightSideBlock);
            else
                return blocks.FindAll(b => b.AwayAttackBlock && b.AwayRightSideBlock);
        }

        public FieldBlock GetTheClosestBlock(Vector3 position)
        {
            float minDist = 0;
            FieldBlock closest = null;
            for (int i = 0; i < blocks.Count; i++)
            {
                float dist = Vector3.Distance(blocks[i].transform.position, position);
                if (!closest || minDist > dist)
                {
                    closest = blocks[i];
                    minDist = dist;
                }
            }

            return closest;
        }

        // public int ForwardBlocksLeft(PlayerAI player)
        // {
        //     int currId = blocks.IndexOf(playerDictionary[player].currentBlock);
        //     int row = currId / colCount;
        //     int col = currId % colCount;

        //     bool home = player.TeamAI == TeamAI.HomeTeamAI;
        //     if (home)
        //         return colCount - 1 - col;
        //     else
        //         return col;

        // }

        // public int BackwardBlocksLeft(PlayerAI player)
        // {
        //     int currId = blocks.IndexOf(playerDictionary[player].currentBlock);
        //     int row = currId / colCount;
        //     int col = currId % colCount;

        //     bool home = player.TeamAI == TeamAI.HomeTeamAI;
        //     if (!home)
        //         return colCount - 1 - col;
        //     else
        //         return col;

        // }


        // public int RightBlocksLeft(PlayerAI player)
        // {
        //     int currId = blocks.IndexOf(playerDictionary[player].currentBlock);
        //     int row = currId / colCount;
        //     int col = currId % colCount;

        //     bool home = player.TeamAI == TeamAI.HomeTeamAI;
        //     if (home)
        //         return rowCount - 1 - row;
        //     else
        //         return row;

        // }

        // public int LeftBlocksLeft(PlayerAI player)
        // {
        //     int currId = blocks.IndexOf(playerDictionary[player].currentBlock);
        //     int row = currId / colCount;
        //     int col = currId % colCount;

        //     bool home = player.TeamAI == TeamAI.HomeTeamAI;
        //     if (!home)
        //         return rowCount - 1 - row;
        //     else
        //         return row;

        // }
    }

}
