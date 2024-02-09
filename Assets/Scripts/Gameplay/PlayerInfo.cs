using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Gameplay
{
    [System.Serializable]
    public struct PlayerInfo: INetworkSerializable, System.IEquatable<PlayerInfo>
    {
        [SerializeField] FixedString32Bytes id;
        public string Id
        {
            get { return id.ToString(); }
        }
        [SerializeField] ulong clientId;
        public ulong ClientId
        {
            get { return clientId; }
        }
        [SerializeField] bool connected;
        public bool Connected
        {
            get { return connected; }
            set { connected = value; }
        }

        public PlayerInfo(ulong clientId, bool connected)
        {
            this.id = "";
            this.clientId = clientId;
            this.connected = connected;
        }

        public bool Equals(PlayerInfo other)
        {
            return id == other.id && clientId == other.clientId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out id);
                reader.ReadValueSafe(out clientId);
                reader.ReadValueSafe(out connected);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(id);
                writer.WriteValueSafe(clientId);
                writer.WriteValueSafe(connected);
            }
        }
    }

}
