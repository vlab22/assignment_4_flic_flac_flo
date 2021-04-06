namespace shared
{
    public class WhoAmIResponse : ASerializable
    {
        public int idInRoom;
        public string userName;
        
        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(idInRoom);
            pPacket.Write(userName);
        }

        public override void Deserialize(Packet pPacket)
        {
            idInRoom = pPacket.ReadInt();
            userName = pPacket.ReadString();
        }
    }
}