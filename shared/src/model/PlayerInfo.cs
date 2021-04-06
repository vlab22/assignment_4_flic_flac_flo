namespace shared
{
    /**
     * Empty placeholder class for the PlayerInfo object which is being tracked for each client by the server.
     * Add any data you want to store for the player here and make it extend ASerializable.
     */
    public class PlayerInfo : ASerializable
    {
        public int id;
        public string userName;

        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(id);
            pPacket.Write(userName);
        }

        public override void Deserialize(Packet pPacket)
        {
            id = pPacket.ReadInt();
            userName = pPacket.ReadString();
        }
    }
}