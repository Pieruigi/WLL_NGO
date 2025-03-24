using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.Netcode
{
    [System.Serializable]
    public class PlayerInfo: NetworkBehaviour
    {
        public static UnityAction<PlayerInfo> OnReadyChanged;
        public static UnityAction<PlayerInfo> OnInitializedChanged;

        [SerializeField] NetworkVariable<FixedString32Bytes> id = new NetworkVariable<FixedString32Bytes>(); // The player identifier ( ex. the playfab id )
        public string Id
        {
            get { return id.Value.ToString(); }
        }
        [SerializeField] NetworkVariable<ulong> clientId = new NetworkVariable<ulong>(0);
        public ulong ClientId
        {
            get { return clientId.Value; }
        }
       
        [SerializeField] NetworkVariable<bool> bot = new NetworkVariable<bool>(false);
        public bool Bot
        {
            get { return bot.Value; }
        }

        [SerializeField] NetworkVariable<bool> home = new NetworkVariable<bool>(false);
        public bool Home
        {
            get { return home.Value; }
        }



        /// <summary>
        /// True if this player loaded data from external source ( for example the teamroster from playfab )
        /// </summary>
        [SerializeField] NetworkVariable<bool> initialized = new NetworkVariable<bool>(false);
        public bool Initialized
        {
            get { return initialized.Value; }
            // set
            // {
            //     var old = initialized;
            //     initialized = value;
            //     if (old != initialized)
            //     {
            //         Debug.Log($"TEST - AAAAAAAAAAAAAAA init:{initialized}, old:{old}");
            //         OnInitializedChanged?.Invoke(this);
            //     }
                    
            // }
        }

        [SerializeField]NetworkVariable<FixedString32Bytes> data = new NetworkVariable<FixedString32Bytes>();
        public string Data
        {
            get { return data.Value.ToString(); }
        }




        /// <summary>
        /// True when client scene has been completely loaded ( addressables, etc. ) and the player is ready to play.
        /// </summary>
        NetworkVariable<bool> ready = new NetworkVariable<bool>(false);
        public bool Ready
        {
            get { return ready.Value; }
            set
            {
                var old = ready.Value;
                ready.Value = value;
                if(old != ready.Value)
                    OnReadyChanged?.Invoke(this);
            }
        }



        public bool IsLocal
        {
            get { return clientId.Value == NetworkManager.Singleton.LocalClientId; }
        }

        /// <summary>
        /// Called by the server to create human player info.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        // public static PlayerInfo CreateHumanPlayer(ulong clientId, bool home)
        // {
        //     return new PlayerInfo()
        //     {
        //         clientId = clientId,
        //         bot = false,
        //         id = $"h_{clientId}",
        //         initialized = false,
        //         home = home,
        //         data = ""
        //     };
        // }

        /// <summary>
        /// Called by the server to create bot player info.
        /// </summary>
        /// <returns></returns>
        // public static PlayerInfo CreateBotPlayer()
        // {
        //     return new PlayerInfo()
        //     {
        //         clientId = 0,
        //         bot = true,
        //         id = "",
        //         initialized = false,
        //         home = false,
        //         data = ""
        //     };
        // }

        /// <summary>
        /// Executed on the server to init data ( for example the teamroster )
        /// </summary>
        /// <param name="data"></param>

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            initialized.OnValueChanged = (o,n) => { OnInitializedChanged?.Invoke(this); };
            PlayerInfoManager.Instance.AddPlayer(this);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PlayerInfoManager.Instance.RemovePlayer(this);
        }

        public void Initialize(string json)
        {
            data.Value = json;
            initialized.Value = true;
        }

        public void CreateHumanPlayer(ulong clientId, bool home)
        {
            this.clientId.Value = clientId;
            this.home.Value = home;
            bot.Value = false;
            id.Value = $"h_{clientId}";
            initialized.Value = false;
            data.Value = "";
        }

        public void CreateBotPlayer()
        {
            clientId.Value = 0;
            this.home.Value = false;
            bot.Value = true;
            id.Value = $"h_bot";
            initialized.Value = false;
            data.Value = "";
        }


        public override string ToString()
        {
            return $"[Player id:{id}, clientId:{clientId}, bot:{bot}, home:{home}, initialized:{initialized}, data:{data}, ready:{ready}]";
        }

        #region data serialization
        // public bool Equals(PlayerInfo other)
        // {
        //     return id == other.id && clientId == other.clientId && bot == other.bot;
        // }

        // public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        // {
        //     Debug.Log($"TEST - AAAAAAAAAAAAAAAAA:{data}");

        //     bool oldInitialized = initialized;

        //     if (serializer.IsReader)
        //     {
        //         var reader = serializer.GetFastBufferReader();
        //         reader.ReadValueSafe(out id);
        //         reader.ReadValueSafe(out clientId);
        //         reader.ReadValueSafe(out bot);
        //         reader.ReadValueSafe(out home);
        //         reader.ReadValueSafe(out initialized);
          
        //         //Initialized = i;
        //         reader.ReadValueSafe(out data);
        //         reader.ReadValueSafe(out bool r); // We do this to trigger the OnReadyChanged event
        //         Ready = r;

        //         if (oldInitialized != initialized)
        //             OnInitializedChanged?.Invoke(this);
        //     }
        //     else
        //     {
        //         var writer = serializer.GetFastBufferWriter();
        //         writer.WriteValueSafe(id);
        //         writer.WriteValueSafe(clientId);
        //         writer.WriteValueSafe(bot);
        //         writer.WriteValueSafe(home);
        //         writer.WriteValueSafe(initialized);
        //         writer.WriteValueSafe(data);
        //         writer.WriteValueSafe(ready);

        //     }

        //     Debug.Log($"TEST - BBBBBBBBBBBBBBBBBBB:{data}");
        // }
        #endregion


    }

}
