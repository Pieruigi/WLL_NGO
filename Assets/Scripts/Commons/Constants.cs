using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO
{
    public class Constants
    {
        public const string DedicatedServerArg = "-dedicatedServer";
        public const string ServerPortArg = "-port";
        public const string ServerAddressArg = "-ip";
        public const string RegionArg = "-region";

        public const string GameModeArg = "-gameMode";


        public const int MatchmakerTimeout = 20000; // In millis
        public const int BoltSessionJoinAttempts = 10;
        public const ushort NoMatchmakingTestingPort = 9797;
        public const int ServerTickRate = 60;

        public const string PoweredMatchQueueName = "1VS1-Powered";
        public const string FastMatchQueueName = "1VS1-GoldenGoal";
        public const string ClassicMatchQueueName = "1VS1-Classic";
        public const string PlayWithFriendsQueueName = "PlayWithFriends";
        public const int ClientMatchmakingTimeout = 20000; // In millis

        public const string DefaultGameScene = "Game";
        public const string LobbyScene = "Lobby";
        public const string ClientMainScene = "Client";
        public const string ServerMainScene = "Server";
        
        


    }

}

