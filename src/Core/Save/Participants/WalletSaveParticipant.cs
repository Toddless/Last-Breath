namespace Core.Save.Participants
{
    using Data.SaveData;
    using Newtonsoft.Json.Linq;
    using Trade;

    public class WalletSaveParticipant(IWalletService wallet) : ISaveParticipant
    {
        public string SectionId => "wallet";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Wallet;

        public JToken Capture() => JToken.FromObject(new WalletSaveData { Gold = wallet.Gold });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<WalletSaveData>();
            if (saved == null) return;
            wallet.RestoreState(saved.Gold);
        }
    }
}
