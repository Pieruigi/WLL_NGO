using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Netcode
{
    [System.Serializable]
    public struct PlayerInfo: INetworkSerializable, System.IEquatable<PlayerInfo>
    {
        [SerializeField] FixedString32Bytes id; // The player identifier ( ex. the playfab id )
        public string Id
        {
            get { return id.ToString(); }
        }
        [SerializeField] ulong clientId;
        public ulong ClientId
        {
            get { return clientId; }
        }
       
        bool bot;
        public bool Bot
        {
            get { return bot; }
        }

        bool home;
        public bool Home
        {
            get { return home; }
        }

        /// <summary>
        /// True if this player loaded data from external source ( for example the teamroster from playfab )
        /// </summary>
        bool initialized;
        public bool Initialized
        {
            get { return initialized; }
        }

        [SerializeField] FixedString32Bytes data;
        public string Data
        {
            get { return data.ToString(); }
        }

        /// <summary>
        /// True when client scene has been completely loaded ( addressables, etc. ) and the player is ready to play.
        /// </summary>
        bool ready;
        public bool Ready
        {
            get { return ready; }
            set { ready = value; }
        }


        public bool IsLocal
        {
            get { return clientId == NetworkManager.Singleton.LocalClientId; }
        }

        /// <summary>
        /// Called by the server to create human player info.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static PlayerInfo CreateHumanPlayer(ulong clientId, bool home)
        {
            Debug.Log($"Creating player info, isHome:{home}");
            return new PlayerInfo()
            {
                clientId = clientId,
                bot = false,
                id = $"h_{clientId}",
                initialized = false,
                home = home,
                data = ""
            };
        }

        /// <summary>
        /// Called by the server to create bot player info.
        /// </summary>
        /// <returns></returns>
        public static PlayerInfo CreateBotPlayer()
        {
            return new PlayerInfo()
            {
                clientId = 0,
                bot = true,
                id = "",
                initialized = false,
                home = false,
                data = ""
            };
        }

        /// <summary>
        /// Executed on the server to init data ( for example the teamroster )
        /// </summary>
        /// <param name="data"></param>
       
        public void Initialize(string json)
        {
            Debug.Log($"Initializing player (clientId:{clientId}): {json}");
            data = json;
            initialized = true;
        }

        public override string ToString()
        {
            return $"[Player id:{id}, clientId:{clientId}, bot:{bot}, home:{home}, initialized:{initialized}, data:{data}, ready:{ready}]";
        }

        #region data serialization
        public bool Equals(PlayerInfo other)
        {
            return id == other.id && clientId == other.clientId && bot == other.bot;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out id);
                reader.ReadValueSafe(out clientId);
                reader.ReadValueSafe(out bot);
                reader.ReadValueSafe(out home);
                reader.ReadValueSafe(out initialized);
                reader.ReadValueSafe(out data);
                reader.ReadValueSafe(out ready);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(id);
                writer.WriteValueSafe(clientId);
                writer.WriteValueSafe(bot);
                writer.WriteValueSafe(home);
                writer.WriteValueSafe(initialized);
                writer.WriteValueSafe(data);
                writer.WriteValueSafe(ready);
            }
        }
        #endregion


    }

}
