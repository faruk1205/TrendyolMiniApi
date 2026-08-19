namespace TrendyolMiniApi.Enums
{
    public enum MessageStatus
    {
        Pending = 0,   // Redis kuyruğuna atıldı, henüz dağıtılmadı
        Sent = 1,      // SignalR ile ilgili gruba başarıyla iletildi
        Failed = 2     // Worker N defa denedi, dead-letter'a düştü
    }
}