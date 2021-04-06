using System.Collections.Generic;

namespace shared
{
    public class PlayersInfoResponse : ASerializable
    {
        public PlayerInfo[] playersInfo = new PlayerInfo[2];

        public override void Serialize(Packet pPacket)
        {
            foreach (var playerInfo in playersInfo)
            {
                pPacket.Write(playerInfo);
            }
        }

        public override void Deserialize(Packet pPacket)
        {
            for (int i = 0; i < playersInfo.Length; i++)
            {
                playersInfo[i] = pPacket.Read<PlayerInfo>();
            }
        }
    }
}