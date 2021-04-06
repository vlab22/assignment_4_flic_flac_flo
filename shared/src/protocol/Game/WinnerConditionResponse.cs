namespace shared
{
    public class WinnerConditionResponse : ASerializable
    {
        public int winnerId;
        public string winnerUser;
        public int gameRoomId;
        
        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(winnerId);
            pPacket.Write(winnerUser);
            pPacket.Write(gameRoomId);
        }

        public override void Deserialize(Packet pPacket)
        {
            winnerId = pPacket.ReadInt();
            winnerUser = pPacket.ReadString();
            gameRoomId = pPacket.ReadInt();
        }
    }
}