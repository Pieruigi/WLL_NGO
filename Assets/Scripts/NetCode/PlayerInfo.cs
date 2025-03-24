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
       
        }



        public bool IsLocal
        {
            get { return clientId.Value == NetworkManager.Singleton.LocalClientId; }
        }

   
        /// <summary>
        /// Executed on the server to init data ( for example the teamroster )
        /// </summary>
        /// <param name="data"></param>

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            initialized.OnValueChanged = (o,n) => { Debug.Log($"Player initialized:{this}"); OnInitializedChanged?.Invoke(this); };
            ready.OnValueChanged = (o, n) => { Debug.Log($"Player ready:{this}"); OnReadyChanged?.Invoke(this); };
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

        /// <summary>
        /// Executed on the server to initialize the local client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        [ServerRpc(RequireOwnership = false)]
        public void InizialiteServerRpc(string data)
        {
            Initialize(data);
            
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

        public void SetReady(bool value)
        {
            ready.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetReadyServerRpc(bool value)
        {
            ready.Value = value;
        }

        public override string ToString()
        {
            return $"[Player id:{id.Value}, clientId:{clientId.Value}, bot:{bot.Value}, home:{home.Value}, initialized:{initialized.Value}, data:{data.Value}, ready:{ready.Value}]";
        }

       


    }

}
